# Pipes: First Iteration

## GitHub

https://github.com/Ivalpe/LabsXarxes/tree/main

## Description

Pipes is a multiclient network game, in which players are challenged to complete pipe puzzles together, with each player having an assigned color and only capable of interacting with the puzzle tiles of their color. Built with simultaneous TCP and UDP communication, the project includes a lobby with a synchronized chat before entering the game.

Clients can enter a server IP and custom display name to join the lobby room. The server dynamically updates connected player lists across all clients, assigns each user a color for gameplay and relays chat lines. The room's host may trigger the start of the game where all players can move, jump and interact with the puzzle board to play, all of which are streamed over UDP.

## Functionalities

### TCP \& UDP Networking

* Server opens both TCP and UDP at once, as well as clients connect to both when connecting to the server
* **TCP Network:** Used for lobby management (connected client list) and chat messaging.
* **UDP Network:** Used for low-latency state synchronization in the game scene (player movement, object movement) and heartbeat tracking (pings) to ensure clients are still connected.

### Lobby Management \& Dynamic Player Tracking

* **Server Authority and Assigning Host Client:** Server acts as the main owner of the room. When the first client connects, the server assigns it a host role, allowing this client to change to the Play Scene (for all clients)

  * When host user disconnects, all clients are also disconnected from the room
* **Client List:** Server sends to all clients the list of connected clients in the room, updating the list when users join or disconnect

### Chat Room

* In the waiting room, clients are able to send chat messages that all users receive in the format "\[username]: chat message" using TCP

### Gameplay

#### Player Movement

* Player movement is handled by the clients sending their inputs (horizontal and vertical axis, yaw rotation and a boolean for jumping)
* Using the received information, the server handles updating the player' positions
* This is helpful to avoid exploiting the system to cheat, since the server handles all the calculations and sets the players' position
* Additionally, we found it to be more of a standard in multiplayer games to send inputs and not direct states or coordinates

#### Object Movement

* **Client controled -> Puzzle tiles**

  * Players can interact with the puzzle tiles to rotate them
  * The change is shown to all users
  * Handled by UDP
* **Server controled -> Timer door**

  * The server itself moves a door within the game scene, slowly closing it as a built in timer to add time pressure to complete the puzzle
  * All players see the door movement at once
  * Handled by UDP

### Disconnect Handling

* **TCP Shutdown:** Detects client window closures or connection drops. The server removes the client, closes sockets safely, and re-broadcasts the updated client list to remaining users.
* **UDP Inactivity Disconnect:** Clients dispatch a ping every second. The server monitors timestamps and if no ping is received from an endpoint for 5 seconds, the server times out the client, removes it, and logs the disconnection.

### Changes from Previous Iteration

* **New Serializer script:** Sending packets is now serialized, sending a MsgType byte to identify what kind of message is being sent so the client or server know how to handle it
* **Player Movement:** users in the game scene can use WASD and the space bar to move around the scene, with the movement being shown to all users in the scene
* **Gameplay:** significant steps towards a fully playeable iteration, including randomly generated puzzle board, puzzle tile interaction (shown to all users), color assignments to users, win and defeat screens...

### Bonus Implementations

* **Jumping:** players can use the space bar to jump
* **More than two players:** more than two players can join, have a color assigned and interact with the board, with the bored using the number of colors that corresponds with the amount of players

## Instructions

* Alongside this README, there's two accompanying folders: ClientBuild and ServerBuild

  * ClientBuild --> contains the executable client
  * ServerBuild --> contains the executable server
* To run locally on the same device, run both executables before connecting to the server
* To run on separate devices:

  * On the device running the ServerBuild, use the console to run "ipconfig" to gain the ip
  * Type that ip into the device(s) using the ClientBuild before connecting to the server
* Gameplay

  * **Movement:** WASD
  * **Jumping:** Space Bar
  * Simply interact with the pipe tiles by clicking the right mouse button, using the mouse to look around

    * Remember! You can only interact with the tiles of the color you were assigned
* To open the Unity Project:

  * Open the folder Lab2 from Unity
  * Unity version: 6000.3.16f1
  * The main gameplay scene is located in 0.Main -> Scenes -> Client\_GameplayScene
  * Other relevant scenes (all located inside 0.Main -> Scenes)

    * Server -> the main server scene
    * Client\_WaitingRoom -> client waiting room which displays the chat and user list (the first visible scene when executing the ClientBuild)

## Team & Tasks

* Ivan Alvarez - [Ivalpe](https://github.com/Ivalpe)

  * Puzzle tile logic (randomly generated board, check if puzzle is solved)
* Lara Guevara - [LaraGuevara](https://github.com/LaraGuevara)

  * Serializer script
  * Player movement
* Bernat Loza - [BerniF0x](https://github.com/BerniF0x)

  * billboards
  * win and defeat logic
* Maria Besora - [mariabeo](https://github.com/mariabeo)

  * Door controller
  * Debug controlls
* Kai Caire - [KaiCaire](https://github.com/KaiCaire)
  * Player Jump (Server Authoritative Physics)
  * Server Scene View
  * Board Spawning & Syncing
  * Tile Emision & Rotation across Server and Clients
\*
* Jana Puig - [JanaPuig](https://github.com/JanaPuig)

  * Spawn +2 players
  * Door controller
  * Debug controlls

