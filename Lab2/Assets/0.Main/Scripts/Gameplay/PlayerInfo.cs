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

    public float yVelocity;

    
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
        yVelocity = 0;
        playerName = "Unknown";
        color = UnityEngine.Color.white;
        input = new PlayerMoveInput();
    }

    public PlayerInfo(int i, string name, Vector3 pos, Vector3 rot, Vector3 sc, float yVel, byte index, UnityEngine.Color m_color)
    {
        id = i; 
        playerName = name;
        position = pos;
        rotation = rot;
        scale = sc;
        yVelocity = yVel;
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
        yVelocity = p.yVelocity;
        colorIndex = p.colorIndex;
        color = p.color;
        input = new PlayerMoveInput();
    }

    //for client start game player spawning
    public PlayerInfo(int i, string name, Vector3 pos, Vector3 rot, float yVel, byte index)
    {
        id = i;
        playerName = name;
        position = pos;
        rotation = rot;
        yVelocity = yVel;
        colorIndex = index;
    }

    public void SetInput(float hor, float vet, float yaw, bool jump, bool grounded)
    {
        input.horizontal = hor;
        input.vertical = vet;
        input.yawRot = yaw;
        input.jump = jump;
        input.grounded = grounded;
    }

}

public class PlayerMoveInput
{
    public float horizontal;
    public float vertical;
    public float yawRot;

    public bool jump;
    public bool grounded;

    public PlayerMoveInput()
    {
        horizontal = 0;
        vertical = 0;
        yawRot = 0;
        jump = false;
        grounded = false;
    }
    
}
