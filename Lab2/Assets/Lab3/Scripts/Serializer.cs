// =================================================================================================
//  Lab 3 - Data serialization: SERIALIZER
//  XJO - Xarxes per a Jocs Online
// =================================================================================================
//
//  THE EXERCISE
//    Turn a PlayerDTO into a byte[] and back, with BinaryWriter and BinaryReader (slides 5 to 7).
//    Then the same for PlayerDTO2, and once more with JsonUtility.
//    No sockets here: this file works whatever your Lab 2 code looks like.
//
//  HOW TO RUN IT
//    Open the scene S_SerializationTest and press Play. Each test says PASS, FAIL or SKIPPED,
//    and prints the bytes you produced.
//
//  WHAT YOU HAVE TO DO
//    The 5 TODOs below, in order. In each one, delete the Todo.NotDone(...) line and write yours.
//    Stuck? Disable the GameObject "SerializationTest", enable "SerializationTestSolution", and
//    compare its output. Its code is in SerializerSolution.cs.
//
//  CHALLENGE 3
//    Copy PlayerDTO.cs and this file into your lobby project, and send a PlayerDTO2 through your
//    own SendPacket / Send (slides 17 to 20).
//
// =================================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class Serializer
{
    // --------------------------------------------------------------------------------- TODO 1 ---
    //  Goal: write every field of 'dto' into 'writer'.
    //  Order: playerName, level, then the list. For a list, first how many, then each one (slide 6).
    //  Look it up: BinaryWriter.Write in the docs. It has one overload per type.
    //  Test scene when it works:  {"Ash", 12, [Pikachu, Charmander]} -> serialized 31 bytes
    public static byte[] Serialize(PlayerDTO dto)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        // your code here
        Todo.NotDone(1, "Serialize(PlayerDTO) writes nothing yet");

        return stream.ToArray();
    }

    // --------------------------------------------------------------------------------- TODO 2 ---
    //  Goal: read the fields back from 'reader' into 'dto'.
    //  Same order and same types as TODO 1 (slide 7). The bytes do not say what they are.
    //  Look it up: BinaryReader.ReadString / ReadInt32 in the docs.
    //  Test scene when it works:  playerName PASS, level PASS, ownedPokemons PASS
    public static PlayerDTO DeserializePlayerDTO(byte[] data)
    {
        BinaryReader reader = new BinaryReader(new MemoryStream(data));
        PlayerDTO dto = new PlayerDTO();
        dto.ownedPokemons = new List<string>();

        // your code here
        Todo.NotDone(2, "DeserializePlayerDTO() reads nothing yet");

        return dto;
    }

    // --------------------------------------------------------------------------------- TODO 3 ---
    //  Goal: the same as TODO 1, for PlayerDTO2. The list now holds Pokemon objects (slide 16).
    //  You cannot write an object: write the fields it has.
    //  Test scene when it works:  [Challenge 2] same bytes as Challenge 1: yes
    public static byte[] Serialize(PlayerDTO2 dto)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        // your code here
        Todo.NotDone(3, "Serialize(PlayerDTO2) writes nothing yet");

        return stream.ToArray();
    }

    // --------------------------------------------------------------------------------- TODO 4 ---
    //  Goal: the same as TODO 2, for PlayerDTO2. Create one Pokemon per item and fill its fields.
    //  Test scene when it works:  playerName PASS, level PASS, ownedPokemons PASS
    public static PlayerDTO2 DeserializePlayerDTO2(byte[] data)
    {
        BinaryReader reader = new BinaryReader(new MemoryStream(data));
        PlayerDTO2 dto = new PlayerDTO2();
        dto.ownedPokemons = new List<Pokemon>();

        // your code here
        Todo.NotDone(4, "DeserializePlayerDTO2() reads nothing yet");

        return dto;
    }

    // --------------------------------------------------------------------------------- TODO 5 ---
    //  Goal: the same round trip with JSON (slides 10 and 11). Two methods, one line each.
    //  ToJson: object -> JSON string -> bytes.  FromJson: bytes -> JSON string -> object.
    //  Look it up: JsonUtility.ToJson / FromJson, and Encoding.UTF8.GetBytes / GetString.
    //  Test scene when it works:  JSON is 90 bytes, binary is 31
    public static byte[] ToJson(PlayerDTO2 dto)
    {
        Todo.NotDone(5, "ToJson() returns nothing yet");
        return null;
    }

    public static PlayerDTO2 FromJson(byte[] data)
    {
        Todo.NotDone(5, "FromJson() returns nothing yet");
        return new PlayerDTO2();
    }

    // =============================================================================================
    //  GIVEN CODE
    // =============================================================================================

    // "03 41 73 68 0C 00 00 00": the bytes as you saw them on slide 8.
    public static string ToHex(byte[] data)
    {
        if (data == null) return "(null)";
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < data.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(data[i].ToString("X2"));
        }
        return sb.ToString();
    }
}

// Thrown by a TODO you have not done yet. The test scene catches it and reports SKIPPED.
public class TodoException : Exception
{
    public readonly int number;
    public TodoException(int number, string detail) : base(detail) { this.number = number; }
}

public static class Todo
{
    public static void NotDone(int number, string detail) { throw new TodoException(number, detail); }
}
