using System;
using System.Xml;
using NUnit.Framework;
using UnityEngine;

[Serializable]
public class PlayerInfo
{
    public int id;
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
    public string playerName;
    public byte colorIndex;
    public UnityEngine.Color color;

    //to handle input
    public PlayerMoveInput input;

    public PlayerInfo()
    {
        id = 0;
        position = Vector3.zero;
        rotation = Vector3.zero;
        scale = Vector3.one;
        playerName = "Unknown";
        color = UnityEngine.Color.white;
        input = new PlayerMoveInput();
    }

    public PlayerInfo(int i, string name, Vector3 pos, Vector3 rot, Vector3 sc, byte index, UnityEngine.Color m_color)
    {
        id = i; 
        playerName = name;
        position = pos;
        rotation = rot;
        scale = sc;
        colorIndex = index;
        color = m_color;
        input = new PlayerMoveInput();
    }

    public PlayerInfo(PlayerInfo p) //copy constructor
    {
        id = p.id;
        playerName = p.playerName;
        position = p.position;
        rotation = p.rotation;
        scale = p.scale;
        colorIndex = p.colorIndex;
        color = p.color;
        input = new PlayerMoveInput();
    }

    //for client start game player spawning
    public PlayerInfo(int i, string name, Vector3 pos, Vector3 rot, byte index)
    {
        id = i;
        playerName = name;
        position = pos;
        rotation = rot;
        colorIndex = index;
    }

    public void SetInput(float hor, float vet, float yaw)
    {
        input.horizontal = hor;
        input.vertical = vet;
        input.yawRot = yaw;
    }

}

public class PlayerMoveInput
{
    public float horizontal;
    public float vertical;
    public float yawRot;

    public PlayerMoveInput()
    {
        horizontal = 0;
        vertical = 0;
        yawRot = 0;
    }
}
