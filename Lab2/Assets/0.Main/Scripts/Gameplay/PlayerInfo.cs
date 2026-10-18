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
    public UnityEngine.Color color;

    public PlayerInfo()
    {
        position = Vector3.zero;
        rotation = Vector3.zero;
        scale = Vector3.one;
        playerName = "Unknown";
        color = UnityEngine.Color.white;
    }

    public PlayerInfo(int i, string name, Vector3 pos, Vector3 rot, Vector3 sc, UnityEngine.Color m_color)
    {
        id = i; 
        playerName = name;
        position = pos;
        rotation = rot;
        scale = sc;
        color = m_color;
    }

    public PlayerInfo(PlayerInfo p) //copy constructor
    {
        id = p.id;
        playerName = p.playerName;
        position = p.position;
        rotation = p.rotation;
        scale = p.scale;
        color = p.color;
    }

}
