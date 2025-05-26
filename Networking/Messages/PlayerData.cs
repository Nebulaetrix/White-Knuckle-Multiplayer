using Riptide;
using UnityEngine;
using White_Knuckle_Multiplayer.Networking.Serializers;

namespace White_Knuckle_Multiplayer.Networking.Messages;

    /// <summary>
    /// Message containing all the data that the networked copies(other player) need to set on their end
    /// </summary>
    public struct PlayerData : IMessageSerializable
    {
        public ushort NetID;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 HandLeftPosition;
        public Vector3 HandRightPosition;
        public string HandLeftState;
        public string HandRightState;
        public Color HandLeftColor;  
        public Color HandRightColor; 
        public string LeftHandItemPrefabName;
        public string RightHandItemPrefabName; 

        public PlayerData(ushort playerID, Vector3 position, Quaternion rotation,
            Vector3 handLeftPosition, Vector3 handRightPosition, string handLeftState, string handRightState, Color handLeftColor, Color handRightColor, string leftHandItemPrefabName, string rightHandItemPrefabName)
        {
            NetID = playerID;
            Position = position;
            Rotation = rotation;
            HandLeftPosition = handLeftPosition;
            HandRightPosition = handRightPosition;
            HandLeftState = handLeftState;
            HandRightState = handRightState;
            HandLeftColor = handLeftColor;
            HandRightColor = handRightColor;
            LeftHandItemPrefabName = leftHandItemPrefabName;
            RightHandItemPrefabName = rightHandItemPrefabName;
            
        }

        // Data needs to be deconstructed to its core components,
        // So it can be sent over the network in this message
        public void Serialize(Riptide.Message message)
        {
            message.AddUShort(NetID);

            // Player Position - Vector3
            message.AddVector3(Position);
            
            // Player Rotation - Quaternion
            message.AddQuaternion(Rotation);
            
            // Left Hand - Vector3
            message.AddVector3(HandLeftPosition);
            
            // Right Hand - Vector3
            message.AddVector3(HandRightPosition);
            
            // Hand States - String
            message.AddString(HandLeftState);
            message.AddString(HandRightState);
                        
            // Hand Color - Vector4
            message.AddColor(HandLeftColor);
            message.AddColor(HandRightColor);
            
            // Item Prefab Names - String
            message.AddString(LeftHandItemPrefabName);
            message.AddString(RightHandItemPrefabName);
        }

        // Reconstruct the message from basic types to advanced ones
        // So that this struct can be used normally in code
        // Without doing anything extra to get the correct type
        public void Deserialize(Riptide.Message message)
        {
            NetID = message.GetUShort();

            // Player Position - Vector3
            Position = message.GetVector3();

            // Player rotation - Quaternion
            Rotation = message.GetQuaternion();
            
            // Left Hand - Vector3
            HandLeftPosition = message.GetVector3();
            
            // Right Hand - Vector3
            HandRightPosition = message.GetVector3();
            
            // Hand States - String
            HandLeftState = message.GetString();
            HandRightState = message.GetString();
            
            // Hand Color - Vector4
            HandLeftColor = message.GetColor();

            // Hand Color - Vector4
            HandRightColor = message.GetColor();
            
            // Item Prefab Names - String
            LeftHandItemPrefabName = message.GetString();
            RightHandItemPrefabName = message.GetString();
        }
    }