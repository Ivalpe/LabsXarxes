// =================================================================================================
//  Lab 8 - Reliability over UDP: DELIVERY NOTIFICATION MANAGER
//  XJO - Xarxes per a Jocs Online
// =================================================================================================
//
//  THE EXERCISE
//    The theory's Delivery Notification Manager (XJO-3.2 slides 36 to 47), in C#, same names.
//    Every packet you send starts with a header (slide 8):
//      [sequence, 4 bytes][hasAcks, 1 byte]                          5 bytes, nothing noted
//      [sequence, 4 bytes][hasAcks, 1 byte][start, 4][count, 1]      10 bytes, one ack range
//      then your packet, from the MsgType byte on, as before.
//    One manager per connection: on the server one per ClientProxy, on the client one.
//    Given: WritePendingAcks, AddPendingAck, AckRange (end of this file), Success and Failure.
//
//  HOW TO RUN IT
//    Open the scene S_ReliabilityTest and press Play. Each check says PASS, FAIL or SKIPPED.
//
//  WHAT YOU HAVE TO DO
//    TODO 1 to 4 below. Delete the Lab8Todo.NotDone(...) line and write yours.
//    Then TODO 5 and 6 in ReliableChannel.cs.
//    Stuck? Disable the GameObject "ReliabilityTest", enable "ReliabilityTestSolution", and compare.
//    Its code is in DeliveryNotificationManagerSolution.cs.
//
//  THEN, IN YOUR GAME (slides 19 to 22)
//    SendPacket writes the header before your bytes and returns the sequence (slide 20).
//    Receive reads it first: ProcessSequenceNumber, then ProcessAcks, then your OnPacketReceived
//    (slide 21). Call ProcessTimedOutPackets every frame. Both sides send at least one packet
//    every tick, an empty one if there is nothing to say (slide 22).
//
// =================================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Lab8
{
    public class DeliveryNotificationManager
    {
        // The time in seconds, Time.time on the slides. The test scene replaces it to replay exact
        // situations (a lost packet, a late ack). In your game, leave it as it is.
        public Func<float> clock = () => Time.time;

        public float timeout = 0.5f;                                // the deadline, slide 15
        public float rtt;                                           // smoothed, in seconds
        public int dispatched, delivered, dropped;                  // statistics, slide 32
        public Action<int> onDeliverySuccess, onDeliveryFailure;    // what the game listens to, slide 23

        int m_nextOutgoingSequence;
        int m_nextExpectedSequence;
        readonly List<(int sequence, float timeDispatched)> m_inFlight = new List<(int, float)>();   // oldest first
        readonly List<AckRange> m_pendingAcks = new List<AckRange>();

        // For your debug overlay (slide 32) and the test scene.
        public int InFlight { get { return m_inFlight.Count; } }
        public int PendingAcks { get { return m_pendingAcks.Count; } }
        public float Loss { get { return dropped + delivered == 0 ? 0f : dropped / (float)(dropped + delivered); } }

        // ---------------------------------------------------------------------------- sending ---

        // --------------------------------------------------------------------------------- TODO 1 ---
        //  Goal: the first 4 bytes of the header (slide 9). Take the next number and move the counter,
        //  write it, count it in dispatched, and put the packet in m_inFlight with the time it left,
        //  clock(). Return the number, the caller needs it later (slide 20).
        //  Test scene when it works:  (a) header 5 bytes without acks, 10 with a range, 1 in flight
        public int WriteSequenceNumber(BinaryWriter writer)
        {
            int sequence = m_nextOutgoingSequence++;
            writer.Write(sequence);
            dispatched++;
            m_inFlight.Add((sequence, Time.time));
            return sequence;
        }

        // Given, slide 10. One range per packet, the oldest. Once written, it is crossed off.
        public void WritePendingAcks(BinaryWriter writer)
        {
            bool hasAcks = m_pendingAcks.Count > 0;
            writer.Write(hasAcks);
            if (hasAcks)
            {
                m_pendingAcks[0].Write(writer);
                m_pendingAcks.RemoveAt(0);
            }
        }

        // -------------------------------------------------------------------------- receiving ---

        // --------------------------------------------------------------------------------- TODO 2 ---
        //  Goal: read the other side's sequence (slides 11 and 12). Lower than m_nextExpectedSequence?
        //  An old packet, return false and the caller drops it. Otherwise the next expected is its
        //  number + 1, note it for the ack with AddPendingAck, and return true.
        //  Test scene when it works:  (c) received 0, 1, 3, ranges [0, 2] then [3, 1]
        //                             (d) received 0, 2, 1, packet 1 dropped
        public bool ProcessSequenceNumber(BinaryReader reader)
        {
            int sequence = reader.ReadInt32();
            if (sequence < m_nextExpectedSequence) return false;
            m_nextExpectedSequence = sequence + 1;
            AddPendingAck(sequence);
            return true;
        }

        // Given, slide 12. Right after the last range? That range grows. If not, a new range.
        void AddPendingAck(int sequence)
        {
            if (m_pendingAcks.Count == 0 || !m_pendingAcks[m_pendingAcks.Count - 1].ExtendIfShould(sequence))
                m_pendingAcks.Add(new AckRange(sequence));
        }

        public void ProcessAcks(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return;                      // hasAcks false, no range in this packet
            int nextAcked = reader.ReadInt32();                     // the start of the range
            int onePastAcked = nextAcked + reader.ReadByte();       // the first number after the range

            // ----------------------------------------------------------------------------- TODO 3 ---
            //  Goal: walk m_inFlight from the oldest packet, while the range is not used up and
            //  something is left in flight (slides 14 and 16). Three cases for m_inFlight[0]:
            //    older than nextAcked     never acked, lost. Remove it, Failure(its sequence)
            //    equal to nextAcked       acked. Remove it, Success(its sequence, its time), nextAcked++
            //    newer than nextAcked     nextAcked is no longer in flight, its deadline passed.
            //                             Skip to it, nextAcked = its sequence
            //  Every pass of the loop must remove a packet or move nextAcked, or Unity freezes.
            //  Test scene when it works:  (f) clean link, 200 packets, 200 delivered once each, 0 dropped

            while (nextAcked < onePastAcked && m_inFlight.Count > 0)
            {
                var packet = m_inFlight[0];
                if (packet.sequence < nextAcked)
                {
                    m_inFlight.RemoveAt(0);
                    Failure(packet.sequence);
                }
                else if (packet.sequence == nextAcked)
                {
                    m_inFlight.RemoveAt(0);
                    Success(packet.sequence, packet.timeDispatched);
                    nextAcked++;
                }
                else
                {
                    nextAcked = packet.sequence;
                }
            }
        }

        // --------------------------------------------------------------------------------- TODO 4 ---
        //  Goal: the deadline (slides 15 and 17). While the oldest packet in flight left more than
        //  timeout seconds ago, clock() minus its time, remove it and call Failure(its sequence).
        //  Called every frame. Nothing else notices an ack that never comes.
        //  A while loop, so remove before anything else, or it never ends and Unity freezes.
        //  Test scene when it works:  (i) ack later than 0.5 s, failure once, success never
        public void ProcessTimedOutPackets()
        {
            while (m_inFlight.Count > 0 && Time.time - m_inFlight[0].timeDispatched > timeout)
            {
                int sequence = m_inFlight[0].sequence;
                m_inFlight.RemoveAt(0);
                Failure(sequence);
            }
        }

        // --------------------------------------------------------------- given, slide 17 -------

        void Success(int sequence, float timeDispatched)
        {
            delivered++;
            rtt += (clock() - timeDispatched - rtt) * 0.1f;         // 10 % toward the new round trip
            onDeliverySuccess?.Invoke(sequence);
        }

        void Failure(int sequence)
        {
            dropped++;
            onDeliveryFailure?.Invoke(sequence);
        }
    }

    // ===================================================================== shared by the handout ===

    // Given, slide 13. Packets start, start + 1, ... start + count - 1 arrived.
    public class AckRange
    {
        public int start;
        public byte count = 1;
        public AckRange(int sequence) { start = sequence; }

        public bool ExtendIfShould(int sequence)                    // the next one in a row?
        {
            if (sequence != start + count || count == 255) return false;
            count++;
            return true;
        }

        public void Write(BinaryWriter writer) { writer.Write(start); writer.Write(count); }
    }

    // Lab 7's MsgType with Events and Input added (slides 25 and 30). In your game, add them to yours.
    public enum MsgType : byte { Text = 1, Player = 2, Transform = 3, Replication = 4, Events = 5, Input = 6 }

    // Thrown by a TODO you have not done yet. The test scene catches it and reports SKIPPED.
    // Not called Todo or Lab7Todo, so it does not clash with the other handouts.
    public class Lab8TodoException : Exception
    {
        public readonly int number;
        public Lab8TodoException(int number, string detail) : base(detail) { this.number = number; }
    }

    public static class Lab8Todo
    {
        public static void NotDone(int number, string detail) { throw new Lab8TodoException(number, detail); }
    }
}
