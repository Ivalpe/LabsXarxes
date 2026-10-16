using System;
using UnityEngine;

[Serializable]
public class PlayerInfo : MonoBehaviour
{
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
        color = new UnityEngine.Color(255, 255, 255, 255);
    }

    public PlayerInfo(string name, Vector3 pos, Vector3 rot, Vector3 sc, UnityEngine.Color m_color)
    {
        playerName = name;
        position = pos;
        rotation = rot;
        scale = sc;
        color = m_color;
    }

    public PlayerInfo(PlayerInfo p) //copy constructor
    {
        playerName = p.playerName;
        position = p.position;
        rotation = p.rotation;
        scale = p.scale;
        color = p.color;
    }

}
