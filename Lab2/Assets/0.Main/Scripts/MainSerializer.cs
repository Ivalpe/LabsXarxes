using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class MainSerializer : MonoBehaviour
{
    //types of messages
    public enum MsgType : byte 
    { 
        Ping = 1,
        Command = 2,
        Text = 3,
        Input = 4,
        World = 5,
        CubeRot = 6,
        BoardState = 7,
        BoardChange = 8
    }

    //types of commands
    public enum CommandType : byte
    {
        None = 0,
        ConnectedTCP = 1,
        ConnectedUDP = 2,
        StartGame = 3,
        AddPlayer = 4,
        UserList = 5,
        Host = 6,
        AssignID = 7
    }


    public abstract class BaseSerializer
    {
        //SERIALIZERS
        //for chat messages + commands with string variables
        public byte[] SerializeText(string text, CommandType command = CommandType.None)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            if(command == CommandType.None)
            {
                writer.Write((byte)MsgType.Text);
                //write the given text
                writer.Write(text);
            }
            else
            {
                //for string variable commands
                writer.Write((byte)MsgType.Command);
                writer.Write((byte)command);
                writer.Write(text);
            }

                return stream.ToArray();
        }
        //for pings + commands with no variables
        public byte[] SerializeTypes(bool ping = false, CommandType command = CommandType.None)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            if (ping)
            {
                //just sending ping
                writer.Write((byte)MsgType.Ping);
            }
            else
            {
                //for simple commands with no variables
                writer.Write((byte)MsgType.Command);
                writer.Write((byte)command);
            }

            return stream.ToArray();
        }
        //for cube rotation value
        public byte[] SerializeCubeRot(float rot)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write((byte)MsgType.CubeRot);
            writer.Write(rot);

            return stream.ToArray();
        }
        //for user list
        public byte[] SerializeUserList(List<string> list)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write((byte)MsgType.Command);
            writer.Write((byte)CommandType.UserList);

            //count of users
            writer.Write(list.Count);
            //all usernames
            foreach(string user in list) writer.Write(user);

            return stream.ToArray();
        }
        //for sending player ID
        public byte[] SerializePlayerId(int id)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write((byte)MsgType.Command);
            writer.Write((byte)CommandType.AssignID);

            writer.Write(id);

            return stream.ToArray();
        }
        //for sending player input
        public byte[] SerializePlayerInput(int id, float hor, float ver, float yaw, bool jump)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write((byte)MsgType.Input);

            //send user id (so server knows what player is sending this input)
            writer.Write(id);

            writer.Write(hor);
            writer.Write(ver);
            writer.Write(yaw);

            writer.Write(jump);
            //writer.Write(grounded);
            

            return stream.ToArray();
        }
        //for sending all player positions
        public byte[] SerializeWorldPos(List<PlayerInfo> playerList, bool StartGame = false, float doorOpening=5.0f)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            if(StartGame)
            {
                writer.Write((byte)MsgType.Command);
                writer.Write((byte)CommandType.StartGame);
            }
            else writer.Write((byte)MsgType.World);

            //write player count
            writer.Write(playerList.Count);

            foreach(var player in playerList)
            {
                //player id
                writer.Write(player.id);
                //if start game -> send player name too
                if (StartGame) writer.Write(player.playerName);
                //player position
                writer.Write(player.position.x);
                writer.Write(player.position.y);
                writer.Write(player.position.z);
                //player orientation
                writer.Write(player.rotation.y);
                //writer.Write(player.yVelocity);
                //if start game -> send player color too
                if (StartGame) writer.Write(player.colorIndex);
            }

            if (!StartGame)
            {
                writer.Write(doorOpening);
            }

            return stream.ToArray();
        }

        public byte[] SerializeBoardState(Pipe[,] pipeMap)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write((byte)MsgType.BoardState);

            writer.Write(pipeMap.GetLength(0));
            writer.Write(pipeMap.GetLength(1));

            for (int i = 0; i < pipeMap.GetLength(0); ++i)
            {
                for(int j = 0; j<pipeMap.GetLength(1); ++j)
                {
                    //corner or straight
                    writer.Write(pipeMap[i, j].type); 
                    //not storing exit bc it can be reconstructed with type
                    //not storing prefab nor tileObj cuz they're GameObjects and not serializable

                    //position
                    writer.Write(pipeMap[i, j].boardPos.x);
                    writer.Write(pipeMap[i, j].boardPos.y);
                    //rotation
                    writer.Write(pipeMap[i, j].rotIndex);
                    //tileColor
                    writer.Write(pipeMap[i, j].tileColor.r);
                    writer.Write(pipeMap[i, j].tileColor.g);
                    writer.Write(pipeMap[i, j].tileColor.b);
                    writer.Write(pipeMap[i, j].tileColor.a);
                    
                }
            }

            byte[] data = stream.ToArray();

            return data;
        }

        public byte[] SerializeTile(Pipe p)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write((byte)MsgType.BoardChange);

            int x = 0;
            int y = 0;

            if (p.tileGO.TryGetComponent<TubeTile>(out TubeTile tileScript))
            {
                x = tileScript.xIndex;
                y = tileScript.yIndex;
            }

            writer.Write(x);
            writer.Write(y);
            
            
            byte[] data = stream.ToArray();

            return data;
        }

        //DESERIALIZER 
        public abstract void Deserialize(byte[] data, Socket TCPfrom = null, EndPoint UDPfrom = null);
    }

    public class ClientSerializer : BaseSerializer
    {
        ClientNetworkManager networkManager;
        public ClientSerializer(ClientNetworkManager manager)
        {
            networkManager = manager;
        }
        public override void Deserialize(byte[] data, Socket TCPfrom = null, EndPoint UDPfrom = null)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(data));
            //get type of message
            MsgType type = (MsgType)reader.ReadByte();

            switch (type)
            {
                case MsgType.Command:
                    //get command type first to handle it correctly
                    CommandType command = (CommandType)reader.ReadByte();
                    HandleCommand(command, reader);
                    break;
                case MsgType.Text:
                    HandleText(reader.ReadString());
                    break;
                case MsgType.CubeRot:
                    networkManager.cubeRot = reader.ReadSingle();
                    break;
                case MsgType.World:
                    networkManager.OnWorldUpdate(reader);
                    break;
                case MsgType.BoardState:
                    networkManager.OnBoardCreated(reader);
                    break;
                case MsgType.BoardChange:
                    networkManager.OnBoardUpdate(reader);
                    break;
                default:
                    networkManager.Log("[ERROR] Received unknown message type.");
                    break;
            }
        }

        public void HandleText(string text)
        {
           //log chat message
           networkManager.Log(text);
        }

        public void HandleCommand(CommandType type, BinaryReader reader)
        {
            switch (type)
            {
                case CommandType.ConnectedTCP:
                    string TCPserverName = reader.ReadString();
                    networkManager.Log("[CLIENT] TCP connection established with server: " + TCPserverName);
                    break;
                case CommandType.ConnectedUDP:
                    string UDPserverName = reader.ReadString();
                    networkManager.Log("[CLIENT] UDP connection established with server: " + UDPserverName);
                    break;
                case CommandType.StartGame:
                    networkManager.LoadGameScene(reader);
                   
                    break;
                case CommandType.Host:
                    networkManager.isHost = true;
                    networkManager.Log("[SERVER] You are this server's Host.");
                    break;
                case CommandType.AddPlayer:
                    networkManager.usersList.Add(reader.ReadString());
                    break;
                case CommandType.UserList:
                    HandleUserList(reader);
                    break;
                case CommandType.AssignID:
                    networkManager.userId = reader.ReadInt32();
                    networkManager.Log("[SERVER] Received unique user ID.");
                    break;
                default:
                    networkManager.Log("[ERROR] Received unknown command.");
                    break;
            }
        }

        public void HandleUserList(BinaryReader reader)
        {
            //if list is already full --> clear it to replace with newly received list
            if (networkManager.usersList.Count > 0) networkManager.usersList.Clear();

            //get count
            int count = reader.ReadInt32();

            //add all users
            for(int i = 0; i < count; i++)
            {
                networkManager.usersList.Add(reader.ReadString());
            }
        }
    }

    public class ServerSerializer : BaseSerializer
    {
        ConnectionManager networkManager;
        public ServerSerializer(ConnectionManager manager)
        {
            networkManager = manager; // this is the server
        }
        
        public override void Deserialize(byte[] data, Socket TCPfrom = null, EndPoint UDPfrom = null)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(data));
            //get type of message
            MsgType type = (MsgType)reader.ReadByte();

            switch (type)
            {
                case MsgType.Ping:
                    //just UDP ping --> no need to do anything 
                    break;
                case MsgType.Text:
                    //networkManager.ctpc.TCPSendChatMessage(TCPfrom, reader.ReadString());
                    string messageText = reader.ReadString();

                    if (messageText == "Win")
                    {
                        networkManager.cudp.UDPStartGame(); // El servidor arranca la partida
                        networkManager.DebugWinGame();      // Cierra la puerta e imprime "Win Game"
                    }
                    else if (messageText == "Lose")
                    {
                        networkManager.cudp.UDPStartGame(); // El servidor arranca la partida
                        networkManager.DebugLoseGame();     // Cierra la puerta e imprime "Lose Game"
                    }
                    else
                    {
                        // Si es texto normal, se envía al chat
                        networkManager.ctpc.TCPSendChatMessage(TCPfrom, messageText);
                    }
                    break;
                case MsgType.Command:
                    //get command type first to handle it correctly
                    CommandType command = (CommandType)reader.ReadByte();
                    HandleCommand(command, reader, TCPfrom, UDPfrom);
                    break;
                case MsgType.Input:
                    HandleInput(reader);
                    break;
                case MsgType.CubeRot:
                    networkManager.cudp.UDPUpdateCube(reader.ReadSingle());
                    break;
                case MsgType.BoardChange:
                    networkManager.UpdateBoard(reader);
                    break;
                default:
                    networkManager.Log("[ERROR] Received unknown message type.");
                    break;
            }
        }

        public void HandleInput(BinaryReader reader)
        { 
            int id = reader.ReadInt32();
            float hor = reader.ReadSingle();
            float ver = reader.ReadSingle();
            float yaw = reader.ReadSingle();

            bool jump = reader.ReadBoolean();
            //bool grounded = reader.ReadBoolean();
           

            if (networkManager.playerInfoDic.TryGetValue(id, out PlayerInfo player))
            {
                player.SetInput(hor, ver, yaw, jump /*grounded*/);
            }
        }

        public void HandleCommand(CommandType type, BinaryReader reader, Socket tcpFrom, EndPoint udpFrom)
        {
            switch (type)
            {
                case CommandType.ConnectedTCP:
                    string userName = reader.ReadString();
                    networkManager.ctpc.TCPNewUser(tcpFrom, userName);
                    break;
                case CommandType.ConnectedUDP:
                    if(udpFrom != null) networkManager.cudp.RegisterUDPClient(udpFrom);
                    break;
                case CommandType.StartGame:
                    networkManager.cudp.UDPStartGame();
                    break;
                default:
                    networkManager.Log("[ERROR] Received unknown command.");
                    break;
            }
        }
    }
}
