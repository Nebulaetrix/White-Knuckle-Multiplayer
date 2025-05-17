using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace White_Knuckle_Multiplayer.Networking
{
    public class PlayerNetworkController : MonoBehaviour
    {
        public ushort netID {  get; private set; }
        private float sendInterval = 0.1f; // 0.1 = 10 times per second
        private float sendTimer = 0f;

        public void Initialize(ushort NetID)
        {
            netID = NetID;
        }

        private void Update()
        {
            if (NetworkClient.Instance?.Client == null || !NetworkClient.Instance.Client.IsConnected) return;

            if (netID != NetworkClient.Instance.Client.Id) return;

            sendTimer += Time.deltaTime;
            if (sendTimer < sendInterval) return;
            sendTimer = 0f;

            // grab transforms and send them
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;
            MessageSender.SendPlayerData(new PlayerData(netID, pos, rot));
        }

        public void UpdatePositionRotation(Vector3 pos, Quaternion rot)
        {
            transform.SetPositionAndRotation(pos, rot);
        }
    }
}
