# Pipes: First Iteration

## GitHub

https://github.com/Ivalpe/LabsXarxes/tree/main

## Description

Pipes is a multiclient network game, in which players are challenged to complete pipe puzzles together, with each player having an assigned color and only capable of interacting with the puzzle tiles of their color. Built with simultaneous TCP and UDP communication, the project includes a lobby with a synchronized chat before entering the game.

Clients can enter a server IP and custom display name to join the lobby room. The server dynamically updates connected player lists across all clients, assigns each user a color for gameplay and relays chat lines. The room's host may trigger the start of the game where all players can move, jump (handled by UDP) and interact with the puzzle board to play (handled by TCP).

## Functionalities

### TCP \& UDP Networking

* Server opens both TCP and UDP at once, as well as clients connect to both when connecting to the server
* **TCP Network:** Used for lobby management (connected client list), chat messaging. 
* **UDP Network:** Used for low-latency state synchronization in the game scene (player movement, object movement) and heartbeat tracking (pings) to ensure clients are still connected. Also tile updates and win/lose screens.

### Lobby Management \& Dynamic Player Tracking

* **Server Authority and Assigning Host Client:** Server acts as the main owner of the room and the host, allowing them to change to the Play Scene (for all clients)

  * When host user disconnects, all clients are also disconnected from the room
* **Client List:** Server sends to all clients the list of connected clients in the room, updating the list when users join or disconnect

### Chat Room

* In the waiting room, users are able to send chat messages that all users receive in the format "\[username]: chat message" using TCP

### Gameplay

#### Player Movement

* Player movement is handled by the users sending their inputs (horizontal and vertical axis, yaw rotation and a boolean for jumping)
* Using the received information, the server handles updating the player' positions
* This is helpful to avoid exploiting the system to cheat, since the server handles all the calculations and sets the players' position
* Additionally, we found it to be more of a standard in multiplayer games to send inputs and not direct states or coordinates

#### Object Movement

* **Client-controlled -> Puzzle tiles**

  * Players can interact with the board by clicking on lit up puzzle tiles matching their color, which rotates them 90º clockwise
  * The change is notified to the server, which changes the server board, and then it notifies all users so they update their own

* **Server-controlled -> Timer door**

  * The server itself moves a door within the game scene, slowly closing it as a built in timer to add time pressure to complete the puzzle
  * All players see the door movement simultaneously

### Disconnect Handling

* **TCP Shutdown:** Detects client window closures or connection drops. The server removes the client, closes sockets safely, and re-broadcasts the updated client list to remaining users.
* **UDP Inactivity Disconnect:** Clients dispatch a ping every second. The server monitors timestamps and if no ping is received from an endpoint for 5 seconds, the server times out the client, removes it, and logs the disconnection.

### Changes from Previous Iteration

* **Replication (passive):** It handles over 3 kind of data (doorData). We choose a passive model because it reduces the chances of cheating, if each client calculated its own state one could modify the timer or open the door befor the timer runs out.Also the code is simpler it searched the object by id and updates the variables without recalculating the simulation.

* **Game Loop** The game can be completed and it has both lose and win conditions

* **Fixes from previous Handling**

### Bonus Implementations

* Background Music and SFX
* VFX
*

## Known Bugs

* 

## Low Latency & Reliability

* **Keeping lag low:** frequent data (player input, world positions, door state) is sent over UDP in small binary packets (`BinaryWriter` + 1-byte `MsgType`). A lost packet is simply replaced by the next one, so we never wait for retransmissions.
* **Making sure important messages arrive:** lobby data, player IDs, the generated board, tile rotations and win/lose screens are sent over TCP, which guarantees delivery and order.

  * Tile rotations are sent to the server as commands (not object rotations, so it's much computationally cheaper). The server applies them and broadcasts the result, so all clients share the same board.

## Instructions

* Alongside this README, there's two accompanying folders: CATasthrophyLineBuild
* To run locally on the same device, run both executables before connecting to the server
* To run on separate devices:

  * On the host device, use the console to run "ipconfig" to gain the ip
  * Type that ip into the other device(s) before connecting to the server

* Gameplay

  * **Movement:** WASD
  * **Jumping:** Space Bar
  * Simply interact with the pipe tiles by hovering the cursor to highlight them, and clicking with the Left Mouse Button to rotate them.

    * Remember! You can only interact with the tiles of the color you were assigned

* Debug Controls (Host only!):
  * **1**: automatically starts the game
  * **2**: forces a win
  * **3**: forces a defeat

* To open the Unity Project:

  * Open the folder Lab2 from Unity
  * Unity version: 6000.3.16f1
  * The main gameplay scene is located in 0.Main -> Scenes -> Client\_GameScene
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

  * Billboards
  * Win and Defeat Logic
* Maria Besora - [mariabeo](https://github.com/mariabeo)

  * Door controller
  * Debug controls
* Kai Caire - [KaiCaire](https://github.com/KaiCaire)
  * Player Jump (+Server Authoritative Physics)
  * Server Scene View
  * Board Spawning & Syncing
  * Tile Emission & Rotation across Server and Clients

* Jana Puig - [JanaPuig](https://github.com/JanaPuig)

  * Spawn +2 players
  * Door controller
  * Debug controls

