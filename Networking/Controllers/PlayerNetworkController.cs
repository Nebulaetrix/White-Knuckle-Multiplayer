using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace White_Knuckle_Multiplayer.Networking.Controllers
{
    /// <summary>
    /// Synchronizes GAME player LOGIC, data is passed to it from <see cref="MessageHandler"/>
    /// </summary>
    public class PlayerNetworkController : MonoBehaviour
    {
        public ushort NetID { get; private set; }
        private float sendInterval = 1f / 60f; // 0.1 = 10 times per second
        private float sendTimer = 0f;
        // Lerping properties
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private float positionLerpSpeed = 5f;
        private float rotationLerpSpeed = 5f;
        private bool hasReceivedFirstUpdate = false;
        private HandsNetworkController HandRightController;
        private HandsNetworkController HandLeftController;
        private bool isLocal = false;

        private void OnEnable()
        {
            // Get ID From transform Name
            var nameNetID = transform.name.Split("_")[^1];
            
            // Workaround to get proper NetID
            // ehm, 100% ethical
            try
            {
                NetID = ushort.Parse(nameNetID);
            }
            catch
            {
                // If Local, Get Player ID from the client directly,
                // Why? Because it is the safest way to get correct LOCAL netID
                NetID = NetworkClient.Instance.Client.Id;
            }
            
            // Works, but throws and error complaining about wrong format
            // NetID = nameNetID == "Player" ? NetworkClient.Instance.Client.Id : ushort.Parse(transform.name.Split("_")[^1]);
            
            LogManager.Client.Warn($"Init PlayerNetworkController with netID {NetID}");
            
            isLocal = NetworkClient.Instance.Client.Id == NetID;
            
            // Initialize target transform values with current values
            targetPosition = transform.position;
            targetRotation = transform.rotation;
            
            InitHands(NetID);
        }

        private void InitHands(ushort netID)
        {
            var leftHand = transform
                .Find(
                    "Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera/Inventory-Root/Left_Hand_Target/Item_Hand_Left/Item_Hands_Left")
                .gameObject;

            var rightHand = transform
                .Find(
                    "Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera/Inventory-Root/Right_Hand_Target/Item_Hand_Right/Item_Hands_Right")
                .gameObject;
            
            if (leftHand == null || rightHand == null)
            {
                LogManager.Net.Warn("Couldn't find hand transforms when attaching controllers.");
                return;
            }

            HandLeftController = leftHand.GetComponent<HandsNetworkController>();
            HandRightController = rightHand.GetComponent<HandsNetworkController>();
            
            if (HandLeftController == null)
                HandLeftController = leftHand.AddComponent<HandsNetworkController>();

            if (HandRightController == null)
                HandRightController = rightHand.AddComponent<HandsNetworkController>();
            
            HandLeftController.Initialize(netID);
            HandRightController.Initialize(netID);
        }

        // !!! RUNS FOR BOTH NETWORKED AND LOCAL PLAYER !!!
        private void Update()
        {
            // Wait until NetID is set to a valid ID
            if (NetID == 0) return;
            
            // Update is ALWAYS sending data from a local player to network
            // It will NEVER send data from a networked player
            if (NetworkClient.Instance?.Client == null || !NetworkClient.Instance.Client.IsConnected) return;

            // For local player: send position updates
            sendTimer += Time.deltaTime;
            if (sendTimer < sendInterval) return;
            sendTimer = 0f;
            
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;
            
            if (!isLocal)
            {
                // Smoothly lerp towards target position and rotation
                transform.position = Vector3.Lerp(transform.position, targetPosition, positionLerpSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);
                // If first update or too far away, just teleport
                if (!hasReceivedFirstUpdate || Vector3.Distance(transform.position, pos) > 5f)
                {
                    LogManager.Client.Warn($"Player position for ID {NetID} teleported");
                    transform.position = pos;
                    transform.rotation = rot;
                    hasReceivedFirstUpdate = true;
                }
            }
            else
            {
                // Send current transform data, because we are a local player
                Vector3 handRightPos = HandRightController.handPosition;
                Vector3 handLeftPos = HandLeftController.handPosition;
                String handLeftState = HandLeftController.handState;
                String handRightState = HandRightController.handState;
                Color handLeftColor = HandLeftController.handColor;   
                Color handRightColor = HandRightController.handColor; 
                MessageSender.SendPlayerData(new PlayerData(
                        NetID,pos, rot, handLeftPos, handRightPos, handLeftState, handRightState, handLeftColor, handRightColor 
                    )
                );
            }
        }
        
        /// <summary>
        /// Synchronizes data for a networked player
        /// <remarks>This method is called when a network message is received</remarks>
        /// </summary>
        /// <param name="pos"><see cref="Vector3"/> Player position</param>
        /// <param name="rot"><see cref="Quaternion"/> Player Rotation</param>
        public void UpdatePositionRotation(Vector3 pos, Quaternion rot)
        {
            targetPosition = pos;
            targetRotation = rot;
        }


        private bool handsErrored;
        
        /// <summary>
        /// Synchronizes data for networked hands, passes them to <see cref="HandsNetworkController"/>
        /// <remarks>This method is called when a network message is received</remarks>
        /// </summary>
        /// <param name="handLeftpos"><see cref="Vector3"/> Left Hand Position</param>
        /// <param name="handRightpos"><see cref="Vector3"/> Right Hand Position</param>
        /// <param name="handLeftstate"><see cref="string"/> State of the Left Hand</param>
        /// <param name="handRightstate"><see cref="string"/> State of the Right hand</param>
        /// <param name = "handLeftColor"><see cref="Color"/> Color of the Left Hand</param>
        /// <param name = "handRightColor"><see cref="Color"/> Color of the Right Hand</param>
        public void UpdateHands(Vector3 handLeftpos, Vector3 handRightpos, string handLeftstate, string handRightstate, Color handLeftColor, Color handRightColor)
        {
            try
            {
                // Left Hand
                HandLeftController.UpdateHandPosition(handLeftpos);
                HandLeftController.UpdateHandState(handLeftstate);
                HandLeftController.UpdateHandColor(handLeftColor);
                // Right hand
                HandRightController.UpdateHandPosition(handRightpos);
                HandRightController.UpdateHandState(handRightstate);
                HandRightController.UpdateHandColor(handRightColor);
            }
            catch (Exception e)
            {
                if (!handsErrored)
                {
                    LogManager.Client.Error($"UpdateHands Error: {e.Message}");
                    handsErrored = true;
                }
            }
        }
    }
}