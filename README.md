# Lab Session 2.2: Chat and Lobby
## GitHub
https://github.com/Ivalpe/LabsXarxes/tree/main

## Description
This project implements a multi-client networking lobby, synchronized chat, and game transition system in Unity using low-level socket programming. The architecture features a dedicated server instance managing both TCP and UDP communication simultaneously.   

Clients can enter a server IP and custom display name to join the lobby room. The server dynamically updates connected player lists across all clients, relays chat lines, and handles host-driven transitions into a synchronized game scene where entity transforms (a cube's rotation) are streamed over UDP.

## Functionalities
### TCP & UDP Networking
* Server opens both TCP and UDP at once, as well as clients connect to both when connecting to the server
* **TCP Network:** Used for lobby management (connected client list) and chat messaging. 
* **UDP Network:** Used for low-latency state synchronization in the game scene and heartbeat tracking (pings) to ensure clients are still connected.

### Lobby Management & Dynamic Player Tracking
* **Server Authority and Assigning Host Client:** Server acts as the main owner of the room. When the first client connects, the server assigns it a host role, allowing this client to change to the Play Scene (for all clients)
  * When host user disconnects, all clients are also disconnected from the room
* **Client List:** Server sends to all clients the list of connected clients in the room, updating the list when users join or disconnect
  * **One user increments:** Informs clients that a new client has joined, adding the new username to the client list
  * **Whole list refresh:** Refreshes the entire client list when a client leaves or disconnects

### Chat Room
* In the waiting room, clients are able to send chat messages that all users receive in the format "[username]: chat message" using TCP

### Disconnect Handling
* **TCP Shutdown:** Detects client window closures or connection drops. The server removes the client, closes sockets safely, and re-broadcasts the updated client list to remaining users.
* **UDP Inactivity Disconnect:** Clients dispatch a ping every second. The server monitors timestamps and if no ping is received from an endpoint for 5 seconds, the server times out the client, removes it, and logs the disconnection.

### Bonus Implementations
* **Host-Initiated Scene Launch:** The room host can trigger game start over UDP, instructing all connected clients to switch to "Client_GameScene" simultaneously.
* **Entity State Sync:** Once inside the game scene, a cube rotates in real-time, relayed to all clients.

## Instructions
* Alongside this README, there's two accompanying folders: ClientBuild and ServerBuild
  * ClientBuild --> contains the executable client 
  * ServerBuild --> contains the executable server
* To run locally on the same device, run both executables before connecting to the server

## Team
* Ivan Alvarez - [Ivalpe](https://github.com/Ivalpe)
* Lara Guevara - [LaraGuevara](https://github.com/LaraGuevara)
* Bernat Loza - [BerniF0x](https://github.com/BerniF0x)
* Maria Besora - [mariabeo](https://github.com/mariabeo)
* Kai Caire - [KaiCaire](https://github.com/KaiCaire)
