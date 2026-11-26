// =================================================================================================
//  Lab 8 - Reliability over UDP: RELIABLE CHANNEL
//  XJO - Xarxes per a Jocs Online
// =================================================================================================
//
//  THE EXERCISE
//    Messages that must arrive, every one, once, in order (slide 24). Events use one channel,
//    inputs another (slide 30). Every tick the packet carries every message not confirmed yet,
//    oldest first, up to 32:
//      [MsgType = Events or Input][count]
//          [id][length, ushort][bytes...]
//          [id][length, ushort][bytes...]
//    When a packet is acked, every message it carried leaves the list. The packet is never sent
//    again, the messages are, in the next packets.
//    Given: Send, WritePacket, OnSent and HandleDeliveryFailure (slides 25 and 26).
//
//  WHAT YOU HAVE TO DO
//    TODO 5 and 6 below, after TODO 1 to 4 in DeliveryNotificationManager.cs.
//    Stuck? Its code is in ReliableChannelSolution.cs.
//
//  THEN, IN YOUR GAME
//    Sender, every tick:   byte[] p = channel.WritePacket();
//                          if (p != null) channel.OnSent(SendPacket(p, to));
//    Its manager:          onDeliverySuccess += channel.HandleDeliverySuccess;
//                          onDeliveryFailure += channel.HandleDeliveryFailure;
//    Receiver, in OnPacketReceived after reading MsgType.Events:
//                          channel.ReadPacket(reader, data => { your event });
//    One channel per connection and per direction. The server has one per ClientProxy.
//
// =================================================================================================

using System;
using System.Collections.Generic;
using System.IO;

namespace Lab8
{
    public class ReliableChannel
    {
        readonly MsgType m_type;                                    // Events or Input
        readonly List<(int id, byte[] data)> m_unacked = new List<(int, byte[])>();   // oldest first
        readonly Dictionary<int, int> m_lastIdIn = new Dictionary<int, int>();         // packet -> newest message in it
        int m_nextId, m_lastWritten;
        int m_nextExpected;                                         // the receiver's side, TODO 6

        public ReliableChannel(MsgType type) { m_type = type; }

        // For your debug overlay and the test scene.
        public int Unacked { get { return m_unacked.Count; } }
        public int timesWritten;                                    // messages written, copies included

        // ---------------------------------------------------------------- given, slides 25 and 26 ---

        public void Send(byte[] data) { m_unacked.Add((m_nextId++, data)); }

        public byte[] WritePacket()                                 // once per tick, null when nothing waits
        {
            if (m_unacked.Count == 0) return null;
            int count = Math.Min(m_unacked.Count, 32);              // under 1200 bytes, Lab 7 slide 17
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((byte)m_type);
            writer.Write(count);
            for (int i = 0; i < count; i++)                         // oldest first
            {
                writer.Write(m_unacked[i].id);
                writer.Write((ushort)m_unacked[i].data.Length);
                writer.Write(m_unacked[i].data);
            }
            m_lastWritten = m_unacked[count - 1].id;
            timesWritten += count;
            return stream.ToArray();
        }

        public void OnSent(int packet) { m_lastIdIn[packet] = m_lastWritten; }   // the number SendPacket returned

        public void HandleDeliveryFailure(int packet) { m_lastIdIn.Remove(packet); }   // its messages are still in m_unacked

        // --------------------------------------------------------------------------------- TODO 5 ---
        //  Goal: packet arrived (slide 26). Take it out of m_lastIdIn, which gives the newest message
        //  it carried, and remove that message and every older one from m_unacked.
        //  A packet that is not in m_lastIdIn was not written by this channel. Do nothing.
        //  Look it up: Dictionary.Remove(key, out value) and List.RemoveAll in the docs.
        //  Test scene when it works:  (k) no events, a success for a packet it never sent changes nothing
        public void HandleDeliverySuccess(int packet)
        {
            if (m_lastIdIn.Remove(packet, out int last))
                m_unacked.RemoveAll(m => m.id <= last);
        }

        // ------------------------------------------------------------------------- receiving ---

        public void ReadPacket(BinaryReader reader, Action<byte[]> handle)   // MsgType already read
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                int id = reader.ReadInt32();
                byte[] data = reader.ReadBytes(reader.ReadUInt16());   // read whole, even to skip it

                // ------------------------------------------------------------------------- TODO 6 ---
                //  Goal: every message once, in order (slide 27). An id lower than m_nextExpected is a
                //  copy from an earlier packet, already handled, skip it. Otherwise the next expected
                //  is id + 1, and handle(data).
                //  Test scene when it works:  (l) events at 20 % loss, all arrive, once, in order

                if (id < m_nextExpected) continue;
                m_nextExpected = id + 1;
                handle(data);
            }
        }
    }
}
