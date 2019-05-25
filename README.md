#### DISCLAIMER
##### Using this project in any test- or productive-environment is at your own discretion!
##### This project is still in heavy developement and large parts may change at any point in time.

---
# Table of contents 
[1. Introduction](#introduction)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[1.1 What is it?](#what-is-it)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[1.2 Demonstration](#demonstration)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[1.3 When to use it?](#when-to-use-it)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[1.4 When not to use it?](#when-not-to-use-it)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[1.5 Features](#features)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[1.6 Installation and Setup](#installation-and-setup)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[1.7 Overview of important classes, properties and functions](#overview-of-important-classes-properties-and-functions)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[1.8 Overview (abstract)](#overview-abstract)<br/>

[2. How to ...?](#how-to-)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[2.1 Create a NetworkScene](#create-a-networkscene)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[2.2 Create a NetworkScene on another Server](#create-a-networkscene-on-another-server)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[2.3 Create a NetworkBehavior in a NetworkScene](#create-a-networkbehavior-in-a-networkscene)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[2.4 Create a NetworkBehavior in a NetworkScene on another Server](#create-a-networkbehavior-in-a-networkscene-on-another-server)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[2.5 Transport the Player to another NetworkScene on any Server](#transport-the-player-to-another-networkscene-on-any-server)<br/>

[3. The NodeManager](#the-nodemanager)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[3.1 What does the NodeManager do?](#what-does-the-nodemanager-do)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[3.2 NodeManager-Parameters](#nodemanager-parameters)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[3.3 Server-To-Server Communication](#server-to-server-communication)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[3.4 NodeMaps](#nodemaps)<br/>

[4. The NetworkSceneManager](#the-networkscenemanager)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[4.1 What does the NetworkSceneManager do?](#what-does-the-networkscenemanager-do)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[4.2 NetworkSceneManager-Parameters](#networkscenemanager-parameters)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[4.3 NetworkBehaviorLists](#networkbehaviorlists)<br/>

[5. Best Practices](#best-practices)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[5.1 Best Practice #1: Change parts you don't like!](#best-practice-1-change-parts-you-dont-like)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[5.2 Best Practice #2: Prefix your Unity-Scenes!](#best-practice-2-prefix-your-unity-scenes)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[5.3 Best Practice #3: Change to a better Serializer!](#best-practice-3-change-to-a-better-serializer)<br/>

[6. Unity Limitations](#unity-limitations)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[6.1 NavMeshes and SceneOffset](#navmeshes-and-sceneoffset)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[6.2 Static GameObjects and SceneOffset](#static-gameobjects-and-sceneoffset)<br/>

[7. FAQ](#faq)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[7.1 Which is the correct IsServer I should use?](#which-is-the-correct-isserver-i-should-use)<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[7.2 My Scene is not being created or a wrong scene is created.](#my-scene-is-not-being-created-or-a-wrong-scene-is-created)<br/>


# Introduction
## What is it?
This project is an alternative implementation of the standard `NetworkManager` that comes out-of-the-box with Forge Networking Remastered as an attempt to provide functionality like a persistent world, dungeon instancing or the concept or 'rooms' in one or more servers.

## Demonstration
![Demonstration](https://raw.githubusercontent.com/k77torpedo/ForgeAndUnity/master/Documentation/demonstration.gif)

## When to use it?
* You need your game to be split up into smaller parts and/or want to be able to run one game on multiple servers. 
* You want your clients to only connect to and see one part of the world instead of everything. 
* You want functionality like a persistent world or dungeon instances

## When not to use it?
* If you have no prior experience with Networking or Forge Networking Remastered. Gain experience with the framework first.
* If your project is an arena- or lobby-style game stick with the default `NetworkManager`. 

Additionally, at the time of writing the native Steam-Integration of the standard `NetworkManager`, the standard implementation of a `MasterServer`, compatability with the `Webserver` or `Matchmaking` in Forge Networking Remastered are not integrated. While these features might be implemented at a later time please know that you will need to provide them yourself currently.

Be aware that if you want to use this over the standard `NetworkManager` or not depends on the scope and features of your own project and is at your own discretion.

## Features
_Info: the term "NetworkScene" describes a Unity-Scene with a `NetworkManager` that is only handling the `NetworkBehaviors` in that Unity-Scene._

* A Scene-based `NetworkManager` for easy creation of `NetworkScenes`
* Multiple `NetworkScenes` per Server-Instance
* Multiple Server-Instances (Can run the first 5 `NetworkScenes` of your game on "Server_1" and another 3 `NetworkScenes` on "Server_2")
* Provides extendable interconnection between Server-Instances out-of-the-box without a database
* Supports creating `NetworkScenes` from one Server in another Server
* Supports instantiating `NetworkBehaviors` from one Server in another Server
* Clients can be instructed to change `NetworkScenes` by the Server
* Concept of "Static-Scenes": `NetworkScenes` that are and should always be reachable under a certain IP and Port, basically the "static  world"/"overworld"
* Concept of "Dynamic-Scenes": `NetworkScenes` that are created on demand for things like Dungeon Instances or Player Housing Instances etc. and that will be destroyed again at some point
* Port-recycling for "Dynamic Scenes": If a "Dynamic Scene" is destroyed the port can be reused at a later time from a range of allowed Ports
* A global registration system for "Dynamic Scenes" that all servers can lookup connection information in and for preventing name-collision of `NetworkScenes` across Server-Instances
* Creating `NetworkScenes` with a position-offset to prevent them physically overlapping each other
* `NetworkScenes` can try to reconnect/rebind after a set delay when disconnected

## Installation and Setup
I recommend at least Unity 2018.3.6f1. If you have issues try upgrading to this or a higher version of Unity.

1) Create a new and empty Unity-Project.

2) Import the latest version of the "Forge Networking Remastered"-unitypackage into the empty Unity-Project from the official GitHub-Page found here: https://github.com/BeardedManStudios/ForgeNetworkingRemastered

2) Download the `ForgeAndUnity.unitypackage` from here: https://github.com/k77torpedo/ForgeAndUnity/tree/master/UnityPackages

3) Import the `ForgeAndUnity.unitypackage` into the project.

4) Register the Unity-Scenes found in the MultiServer-Example-Project in the BuildSettings and in the order shown below: 

![Setup Image](https://raw.githubusercontent.com/k77torpedo/ForgeAndUnity/master/Documentation/ForgeAndUnity%20Setup.JPG "Setup Image")

5) You need to recompile the `NetworkBehaviors` in Forge Networking Remastered in order for the `NetworkBehaviors` to work. To do so you simply need to save any `NetworkBehavior` in the Network Contract Wizard as shown below:

![Recompile](https://raw.githubusercontent.com/k77torpedo/ForgeAndUnity/master/Documentation/ForgeAndUnity%20Compile%20Forge.jpg "Recompile")

6) Open the `Login`-scene in the Unity-Editor.

7) Press Start!

## Overview of important classes, properties and functions
Click on the images below to enlarge.

![Overview classes](https://raw.githubusercontent.com/k77torpedo/ForgeAndUnity/master/Documentation/ForgeAndUnity%20Classes.jpeg "Overview classes")

## Overview (abstract)
![Overview abstract](https://raw.githubusercontent.com/k77torpedo/ForgeAndUnity/master/Documentation/ForgeAndUnity%20Overview.jpeg "Overview abstract")


# How to ...?
## Create a NetworkScene
Every `NetworkScene` is created from a `NetworkSceneTemplate`. Creating a `NetworkSceneTemplate` during runtime is very easy and straight-forward as shown below:
```
// Create the connection-information for the NetworkSceneTemplate
NetworkSceneManagerSetting setting = new NetworkSceneManagerSetting();
setting.MaxConnections = 64;
setting.UseTCP = false;
setting.UseMainThreadManagerForRPCs = true;
setting.ServerAddress = new NetworkSceneManagerEndpoint("127.0.0.1", 15000);
setting.ClientAddress = new NetworkSceneManagerEndpoint("127.0.0.1", 15000);

// Create the NetworkSceneTemplate with our connection-information
NetworkSceneTemplate template = new NetworkSceneTemplate();
template.BuildIndex = 1;
template.SceneName = "My_Custom_NetworkScene_Name";
template.Settings = setting;

//Create the NetworkScene
NodeManager.Instance.CreateNetworkScene(template);
```
First you set up the connection-information. Then you set the BuildIndex of the scene you want to create as a `NetworkScene`. Finally, you choose a custom-name for your `NetworkScene`. The `NodeManager` will do the rest for you. 

You can have 500 `NetworkScenes` of the same BuildIndex as long as they all have unique custom-names - this is especially important when creating dungeon-instances where you want to create the same dungeon over and over for different players. To take it even one step further you can hook up to events that will be emitted during scene-creation. This lets you know when exactly your scene is ready to instantiate your `NetworkBehaviors`:

```
//Create the NetworkScene
NetworkSceneItem scene = NodeManager.Instance.CreateNetworkScene(template);
if (!scene.IsReady) {
    scene.OnReady += (pItem) => {
        Debug.Log("Your NetworkScene is ready!!!");
    };
}
```

## Create a NetworkScene on another Server
Creating a `NetworkScene` on another server is pretty easy and very similar to creating it locally as shown below:
```
// Create the connection-information for the NetworkSceneTemplate
NetworkSceneManagerSetting setting = new NetworkSceneManagerSetting();
setting.MaxConnections = 64;
setting.UseTCP = false;
setting.UseMainThreadManagerForRPCs = true;
setting.ServerAddress = new NetworkSceneManagerEndpoint("127.0.0.1", 15000);
setting.ClientAddress = new NetworkSceneManagerEndpoint("127.0.0.1", 15000);

// Create the NetworkSceneTemplate with our connection-information
NetworkSceneTemplate template = new NetworkSceneTemplate();
template.BuildIndex = 1;
template.SceneName = "My_Custom_NetworkScene_Name";
template.Settings = setting;

//Create the NetworkScene on another Node
uint targetNodeId = 2;
NodeManager.Instance.CreateNetworkSceneInNode(targetNodeId, template);
```
You will need to know the `NodeId` of the server you want to create the `NetworkScene` in. You can set each `NodeId` on the `NodeMap` (see [3.4 NodeMaps](#nodemaps) for more information). Also be aware that you need to have at least one `NodeManager` running as a `MasterNode` as otherwise no Server-To-Server communication can happen. You can set a `Node` as `IsMasterNode` in the `NodeMap` of the `NodeManager`. 

As with locally creating a `NetworkScene` we also have the option to hook up on events to know if anything went wrong or the scene has been created successfully:

```
//Create the NetworkScene on another Node
uint targetNodeId = 2;
ServiceCallback callback = NodeManager.Instance.CreateNetworkSceneInNode(targetNodeId, template);
if (callback.State == ServiceCallbackStateEnum.AWAITING_RESPONSE) {
    callback.OnResponse += (pResponseTime, pResponseData, pSender) => {
        if (pSender.State == ServiceCallbackStateEnum.RESPONSE_SUCCESS) {
            Debug.Log("Your NetworkScene is ready!!!");
        }
    };
}
```
Altough it involves a bit more code we can still check for success!

## Create a NetworkBehavior in a NetworkScene
You can instantiate new `NetworkBehaviors` as shown below:
```
// Instantiate a new NetworkBehavior in the current NetworkScene
string currentSceneName = gameObject.scene.name;
int playerCreateCode = 1;
NetworkBehavior behavior = NodeManager.Instance.InstantiateInScene(currentSceneName, playerCreateCode);
```
If you prefer readability you can also instantiate via name:
```
// Instantiate a new NetworkBehavior in the current NetworkScene
string currentSceneName = gameObject.scene.name;
NetworkBehavior behavior = NodeManager.Instance.InstantiateInScene(currentSceneName, "Player");
```
You can setup the names for each `NetworkBehavior` in the `NetworkBehaviorList` of the `NodeManager` (see [4.3 NetworkBehaviorLists](#networkbehaviorlists) for more information).

## Create a NetworkBehavior in a NetworkScene on another Server
Instantiating a new `NetworkBehavior` on another server is very similar to instantiating it locally as shown below:
```
// Instantiate a new NetworkBehavior in a NetworkScene on another Node
uint targetNodeId = 2;
string sceneName = "NetworkSceneName_On_Node2";
int monsterCreateCode = 2;
NodeManager.Instance.InstantiateInNode(targetNodeId, sceneName, monsterCreateCode);
```
As we sometimes need to make sure that something is successfully instantiated on another server we have the option to hook up on events to get notified on success or failure:

```
// Instantiate a new NetworkBehavior in a NetworkScene on another Node
uint targetNodeId = 2;
string sceneName = "NetworkSceneName_On_Node2";
int monsterCreateCode = 2;
ServiceCallback callback = NodeManager.Instance.InstantiateInNode(targetNodeId, sceneName, monsterCreateCode);
if (callback.State == ServiceCallbackStateEnum.AWAITING_RESPONSE) {
    callback.OnResponse += (pResponseTime, pResponseData, pSender) => {
        if (pSender.State == ServiceCallbackStateEnum.RESPONSE_SUCCESS) {
            Debug.Log("Your NetworkBehavior has successfully been instantiated!!!");
        }
    };
}
```

## Transport the Player to another NetworkScene on any Server
Transferring a player to another `NetworkScene` has many obstacles we need to overcome. Timouts, errors, what if the `NetworkScene` is not there anymore? The `NetworkSceneTeleporter` is a prefab that already handles these things out-of-the box and gives you a starting point to implement your own logic if you want to.

Please see the `NetworkSceneTeleporter`-Prefab and `NetworkSceneTeleporter`-Script that are included in this project for more information.

# The NodeManager
## What does the NodeManager do?
The `NodeManager` helps you with the following tasks:
* Create and remove `NetworkScenes` without name-collision
* Redirect instantiation of `NetworkBehaviors` to the correct `NetworkSceneManagers`
* Establish and maintain Server-To-Server communication

The `NodeManager` is the central point for creating, removing and looking up `NetworkScenes`. It supports you with several helper-functions like `FindNetworkSceneItem()` or `FindNetworkSceneTemplate()` to make handling all your scenes easier. It exposes a variety of events for you within `NetworkScene`-creation and `NetworkBehavior`-instantiaton.

You can start the `NodeManager` as a server or a client through its respective `StartAsServer()`- or `StartAsClient()`-functions.

## NodeManager-Parameters
![NodeManager in Unity](https://raw.githubusercontent.com/k77torpedo/ForgeAndUnity/master/Documentation/NodeManager.JPG "NodeManager")
* **NetworkSceneManagerPrefab**: The `NetworkSceneManager` on this Prefab will be instantiated for all `NetworkScenes` to handle networking. Use this to provide a custom `NetworkSceneManager` if needed.
* **AutoReconnectMasterManager**: When enabled the `NodeManager` will try to reconnect if it has lost its connection to the `MasterNode` in order to maintain Server-To-Server communication. If you only have one `Node` running as a server you should disable this.
* **AutoReconnectMasterManagerInterval**: The interval in seconds to which the `NodeManager` should try to reconnect to the `MasterNode`.
* **EnableRegisterDynamicScenes**: When enabled the `NodeManager` will register its dynamic scenes to the `MasterNode` so other `NodeManagers` can look up its `NetworkScenes`. If you only have one `Node` running as a server you should disable this.
* **RegisterDynamicScenesRequireConfirmation**: When enabled a `NetworkScene` can only be fully initialized if its `NetworkScene` has been successfully registered on the `MasterNode` else it will be deleted. Registering `NetworkScenes`on the `MasterNode` is done to prevent scene-name-collision across servers. If you only have one `Node` running as a server you should disable this.
* **EnableSceneLookUpCaching**: When enabled all lookups of `NetworkScenes` for connection-data from other servers through the `MasterNode` will be cached to prevent unnecessary querying of the `MasterNode`.
* **NodeMapSO**: When starting as a server the `NodeManager` will lookup the `Node` it should start as and initialize all `NetworkSceneTemplates` for that `Node` as _static_ `NetworkScenes`.
* **ServiceNetworkBehaviorListSO**: All `NetworkBehaviors` in this `NetworkBehaviorList` will be initialized by the `MasterNode` to act as services for Server-To-Server communication. The only service is currently the `NodeService` which provides functionality to create `NetworkScenes` and `NetworkBehaviors` on other servers.
* **NetworkSceneBehaviorListSO**: A list of all `NetworkBehaviors` that can be instantiated by any `NetworkSceneManager` for its respective `NetworkScene`.

## Server-To-Server Communication
![Server-To-Server](https://raw.githubusercontent.com/k77torpedo/ForgeAndUnity/master/Documentation/ForgeAndUnity%20Server2Server.jpeg "Server-To-Server")
In order for `NetworkBehaviors` and especially Players to move across server-instances we need Server-To-Server communication so that if a Player in 'Server A' moves to 'Server B' the data of the Player can be properly transmitted.

How do servers communicate with each other you might ask? The answer to that would be very simple: all servers play their own little Forge-Game to transmit information with each other where one server is the host (the `MasterNode`) and the other servers are the clients. The `NetworkSceneManager` for this game is located on `NodeManager.MasterManager`. 

Additionally, if you look at the `NodeService`-Script that is currently used for Server-To-Server communication you will find that it is just a simple `NetworkBehavior` instantiated like any other on the game that the servers are playing with each other.

This gives you all the flexibility of Forge Networking Remastered to extend Server-To-Server communication without introducing extra logic or restrictions. If you want the servers to communicate more information or add stuff like database-functionality across servers you can just make a `NetworkBehavior` and instantiate it on the game the servers are playing with each other - easy as that!

## NodeMaps
`NodeMaps` describe all `Nodes` with their respective `NetworkScenes` that a `NodeManager` should instantiate. Theese `NetworkScenes` are guaranteed to always be reachable under a certain Ip or Port and thus form your 'persistent world' or 'overworld' - they are refered to as _static_ `NetworkScenes`. Every `NodeManager` needs a `NodeMap` to start. You can create a `NodeMap` by right-clicking in your project-window and choosing 'Create > ForgeAndUnity > NodeMapSO' as shown below:

![NodeMaps](https://raw.githubusercontent.com/k77torpedo/ForgeAndUnity/master/Documentation/NodeMaps.png "NodeMaps")

This will create a Scriptable-Object holding your `NodeMap` that you can edit and assign to the `NodeManager`.

# The NetworkSceneManager
## What does the NetworkSceneManager do?
A `NetworkSceneManager`is always responsible for exactly one Unity-Scene. The `NetworkSceneManager` instantiates `NetworkBehaviors` and makes sure they are moved to its Unity-Scene. This way the world can be easily split up into many parts which each part being a Unity-Scene handled by a `NetworkSceneManager`. A Unity-Scene and a `NetworkSceneManager` together are refered to as a `NetworkScene`. 

The client will only ever be connected to one `NetworkSceneManager` and only see the part of the world the `NetworkSceneManager` is handling.

## NetworkSceneManager-Parameters
![NetworkSceneManager](https://raw.githubusercontent.com/k77torpedo/ForgeAndUnity/master/Documentation/NetworkSceneManager.JPG "NetworkSceneManager")
* **NetworkBehaviorListSO**: A list of all `NetworkBehaviors` the `NetworkSceneManager` can instantiate. If the `NetworkSceneManager` has been created by a `NodeManager` it will use the `NetworkBehaviorListSO` of the `NodeManager`.
* **AutoReconnect**: When enabled the `NetworkSceneManager` will try to reconnect if connection has been lost.
* **AutoReconnectInterval**: The interval in seconds the `NetworkSceneManager` should try to reconnect.

## NetworkBehaviorLists
`NetworkBehaviorLists` are lists of `NetworkBehaviors` that a `NetworkSceneManager` can instantiate over the network. You can create a `NetworkBehaviorList` by right-clicking in your project-window and choosing 'Create > ForgeAndUnity > NetworkBehaviorListSO' as shown below:

![NetworkBehaviorList](https://raw.githubusercontent.com/k77torpedo/ForgeAndUnity/master/Documentation/NetworkBehaviorLists.png "NetworkBehaviorList")

This will create a Scriptable-Object holding your `NetworkBehaviorList` that you can edit and assign to your `NetworkSceneManager` and `NodeManager`.

# Best Practices
## Best Practice #1: Change parts you don't like!
You can change and extend most of the code from the project without touching the core-files. Nearly everything in the `NodeManager` and `NetworkSceneManager` for example is marked as _virtual_ and you are encouraged to derive from these classes and _override_ them or extend parts and functions according to your project. Even the Prefabs that come with the project like the `NetworkSceneTeleporter` have all their functions marked as _virtual_ so you can quickly extend them.

Also note that the `NodeManager` has a public parameter for providing your own `NetworkSceneManager`-Prefab that it should instantiate for all `NetworkScenes` which makes it even more easy to provide your custom-logic. 

TL;DR: You can and should derive from all classes. You are encouraged to change bits and parts depending on the needs of your game!

## Best Practice #2: Prefix your Unity-Scenes!
At any time and especially during scene-creation all Unity-Scenes must be named unique. Please prefix your Unity-Scene-Files so name-collision can be avoided. Instead of '_Level_1_' use '_template_Level_1_' or '_t_Level_1_' as this ensures there are no name-collisions during scene-creation that may cause unexpected behavior.


Explanation:<br/>
To create a new `NetworkScene` the framework will first create an _empty_ Unity-Scene with your desired scene-name (here 'Level_1') and put a `NetworkSceneManager` in it so it can be connected to.

Then a second - _the actual_ - scene will be created with the BuildIndex provided from the Unity-Scene-File. This scene is to be merged with the other scene we created previously. The new scene will also be named 'Level_1' - in accordance to the Unity-Scene-File-Name. Now we have a name-collision. We have 2 Scenes with the name 'Level_1' that can't be merged because they both have the same name.

## Best Practice #3: Change to a better Serializer!
_Info: This is more reserved for the end of your project so don't overstress it._ 

All internal serialization of data is currently done via C#s `BitFormatter` which is very flexible and undemanding in what it can serialize and deserialize but an atrocity in efficiency and performance. I just want to mention that you might want to switch to a different serializer at some point in time like _MsgPack_, _Ceras_ or _ZeroFormatter_. 


# Unity Limitations
## NavMeshes and SceneOffset
_Info: The `NetworkSceneTemplate.SceneOffset` allows a `NetworkScene` to be created with an offset so that it does not physically overlap with existing scenes._

Be aware that when you create a dynamic scene like a new dungeon instance or a player housing instance that the `NetworkSceneTemplate.SceneOffset` will not move the NavMesh associated with the scene. I recommend using the NavMeshTools from Unity to be able to create NavMeshes during runtime solve this problem.

## Static GameObjects and SceneOffset
_Info: The `NetworkSceneTemplate.SceneOffset` allows a `NetworkScene` to be created with an offset so that it does not physically overlap with existing scenes._

Be aware that when you create a dynamic scene like a new dungeon instance or a player housing instance that the `NetworkSceneTemplate.SceneOffset` can't be properly applied to the Unity-Scene when the `GameObjects` of the Unity-Scene are marked as `static`.

# FAQ
### Which is the correct IsServer I should use?
To check if you are the server or the client you can either globally check for `NodeManager.IsServer` or locally check for `networkObject.IsServer` in your `NetworkBehaviors` from Forge Networking Remastered.

Do not use `NodeManager.MasterManager.IsServer` as this is an indication if a `Node` is connected to a `MasterNode` as a client or host within Server-To-Server communication. Also do not use `NetworkManager.IsServer` as that is the default `NetworkManager` of Forge Networking Remastered we are not using.

### My Scene is not being created or a wrong scene is created.
Make sure that all Unity-Scenes you want to create as a `NetworkScene` are added in your Build-Settings. A Scene that is not added to your Build-Settings can't be created during runtime.

Also make sure you have the correct BuildIndex when creating your `NetworkScene`.

# Todo (Please bare with me :) )
- What are services? How to make a service?
- How are the Nodes communicating with each other?
- Cleaner terminology througout documentation
- Better use of markup, what a mess lol
## More to come :)


# MIT License
Copyright (c) 2018 k77torpedo

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
