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
                MessageSender.SendPlayerData(new PlayerData(netID, pos, rot));
            
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
    }
}
