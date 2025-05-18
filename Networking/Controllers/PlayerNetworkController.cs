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

        public void Initialize(ushort netID, HandsNetworkController leftHandController, HandsNetworkController rightHandController)
        {
            // Keep track of LOCAL netID
            NetID = netID;
            if (NetID == 0) Destroy(this);
            
            isLocal = NetworkClient.Instance.Client.Id == netID;
            
            // Initialize target transform values with current values
            targetPosition = transform.position;
            targetRotation = transform.rotation;
            
            // Keep track of hands, so we can update/get values for/from them
            HandLeftController = leftHandController;
            HandRightController = rightHandController;
        }

        // !!! RUNS FOR BOTH NETWORKED AND LOCAL PLAYER !!!
        private void Update()
        {
            // redundancy check
            if (NetID == 0) Destroy(this);
            
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

        /// <summary>
        /// Synchronizes data for networked hands, passes them to <see cref="HandsNetworkController"/>
        /// <remarks>This method is called when a network message is received</remarks>
        /// </summary>
        /// <param name="handLeftpos"><see cref="Vector3"/> Left Hand Position</param>
        /// <param name="handRightpos"><see cref="Vector3"/> Right Hand Position</param>
        /// <param name="handLeftstate"><see cref="string"/> State of the Left Hand</param>
        /// <param name="handRightstate"><see cref="string"/> State of the Right hand</param>
        public void UpdateHands(Vector3 handLeftpos, Vector3 handRightpos, string handLeftstate, string handRightstate, Color handLeftColor, Color handRightColor)
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
    }
}