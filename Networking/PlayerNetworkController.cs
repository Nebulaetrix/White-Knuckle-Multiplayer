using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace White_Knuckle_Multiplayer.Networking
{
    public class PlayerNetworkController : MonoBehaviour
    {
        public ushort netID { get; private set; }
        private float sendInterval = 0.1f; // 0.1 = 10 times per second
        private float sendTimer = 0f;
        // Lerping properties
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private float positionLerpSpeed = 15f;
        private float rotationLerpSpeed = 15f;
        private bool hasReceivedFirstUpdate = false;
        private GameObject HandRight;
        private GameObject HandLeft;
        public void Initialize(ushort NetID)
        {
            netID = NetID;
            
            // Initialize target transform values with current values
            targetPosition = transform.position;
            targetRotation = transform.rotation;
        }
        private void Update()
        {
            if (NetworkClient.Instance?.Client == null || !NetworkClient.Instance.Client.IsConnected) return;

            // For local player: send position updates
                sendTimer += Time.deltaTime;
                if (sendTimer < sendInterval) return;
                sendTimer = 0f;

                // Send current transform data
                Vector3 pos = transform.position;
                Quaternion rot = transform.rotation;
                Vector3 handRightPos = HandRight.transform.position;
                Vector3 handLeftPos = HandLeft.transform.position;
                String handLeftState = Sprite.GetName(HandLeft.GetComponent<SpriteRenderer>().sprite);
                String handRightState = Sprite.GetName(HandRight.GetComponent<SpriteRenderer>().sprite);
                MessageSender.SendPlayerData(new PlayerData(netID,pos, rot, handRightPos, handLeftPos, handLeftState, handRightState));
        }

        private void OnEnable()
        {
            
            HandRight = transform.Find("Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera/Inventory-Root/Right_Hand_Target/Item_Hand_Right/Item_Hands_Right").gameObject; 
            HandLeft = transform.Find("Main Cam Root/Main Camera Shake Root/Main Camera/Inventory Camera/Inventory-Root/Left_Hand_Target/Item_Hand_Left/Item_Hands_Left").gameObject;
        }
        
        public void UpdatePositionRotation(Vector3 pos, Quaternion rot)
        {
            targetPosition = pos;
            targetRotation = rot;
            // Smoothly lerp towards target position and rotation
            transform.position = Vector3.Lerp(transform.position, targetPosition, positionLerpSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);
            // If first update or too far away, just teleport
            if (!hasReceivedFirstUpdate || Vector3.Distance(transform.position, pos) > 5f)
            {
                transform.position = pos;
                transform.rotation = rot;
                hasReceivedFirstUpdate = true;
            }
        }

        public void UpdateHands(Vector3 handLeftpos, Vector3 handRightpos, string handLeftstate, string handRightstate)
        {
            HandLeft.transform.position = handLeftpos;
            HandRight.transform.position = handRightpos;
        }
    }
}