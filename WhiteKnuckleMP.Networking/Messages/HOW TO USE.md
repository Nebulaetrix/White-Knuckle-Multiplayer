# PlayerSyncMessage.cs

## Overview
The `PlayerSyncMessage` class is used for synchronizing player data across the network. It implements the `INetworkMessage` interface and provides methods for creating, reading, and writing the message.

## Usage

### Creating a Message
To create an instance of `PlayerSyncMessage`, use the constructor:

```csharp
ushort netId = 1;
Vector3 position = new Vector3(0, 0, 0);
Quaternion rotation = Quaternion.identity;
string leftItemName = "Item_Beans";
string rightItemName = "None";

PlayerSyncMessage message = new PlayerSyncMessage(netId, position, rotation, leftItemName, rightItemName);
```

### Reading a Message
To read a `PlayerSyncMessage` from a `NetworkPacket`, use the static method `FromPacket`:

```csharp
NetworkPacket packet = ...; // The packet that was recieved on the network

PlayerSyncMessage message = PlayerSyncMessage.FromPacket(packet);
```

### Writing a Message
To write a `PlayerSyncMessage` to a `NetworkPacket`, call the `WriteTo` method:

```csharp
var playerSyncMessage = new PlayerSyncMessage(_netID, transform.position, transform.rotation, leftItem, rightItem);
        
using (var packet = new NetworkPacket((ushort)MessageId.PlayerSync))
{
    playerSyncMessage.WriteTo(packet);

    // send over network
    NetworkManager.Instance.Client.Send(packet.RawMessage);
}
```

## Creating Custom Messages

### Implementing INetworkMessage
To create your own custom network message, follow these steps:

1. Define a **readonly** struct that implements the `INetworkMessage` interface.
2. Add properties for the message data.
3. Implement the `MessageId`, `FromPacket`, and `WriteTo` methods.

#### Example Custom Message

```csharp
using UnityEngine;

namespace WhiteKnuckleMP.Networking.Messages;

public readonly struct CustomSyncMessage : INetworkMessage
{
    public ushort MessageId => (ushort)Networking.MessageId.CustomSync;

    public int SomeIntValue { get; }

    public CustomSyncMessage(int someIntValue)
    {
        SomeIntValue = someIntValue;
    }
    
    public static CustomSyncMessage FromPacket(NetworkPacket packet)
    {
        int someIntValue = packet.ReadInt();

        return new CustomSyncMessage(someIntValue);
    }
    
    public void WriteTo(NetworkPacket packet)
    {
        packet.Write(SomeIntValue);
    }
}
```

## Registering Custom Messages

### Adding to MessageIDs.cs
To register your custom message with the network system, add it to the `MessageId` enum in the `MessageIDs.cs` file.

```csharp
// Existing code...
    public enum MessageId : ushort
    {
        // ... other message IDs ...
        CustomSync, // <- Add own message id
    }
// Rest of existing code...
```

Ensure the value for `CustomSync` is unique. This will allow custom message to be recognized and processed by the network system.