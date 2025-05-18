using UnityEngine;
using System.Collections.Generic;

namespace White_Knuckle_Multiplayer.Networking.Controllers
{
    /// <summary>
    /// Handles synchronization of hands,
    /// passing <see cref="handPosition"/> and <see cref="handState"/> to <see cref="PlayerNetworkController"/>
    /// </summary>
    public class HandsNetworkController : MonoBehaviour
    {
        public ushort NetID { get; private set; }
        
        // Expose final variables to other scripts
        public Vector3 handPosition = Vector3.zero;
        public string handState = "hands_idle";
        
        // Internal Variables
        private SpriteRenderer spriteRenderer;
        private string lastState = "hands_idle";
        private bool isLocal = false;
        private Transform handParent;
        
        // Lerping variables
        private bool hasReceivedFirstUpdate = false;
        private float positionLerpSpeed = 5f;
        private Vector3 targetPosition;

        public HandsNetworkController Initialize(ushort netID)
        {
            NetID = netID;
            
            // Did Initialize not receive a valid ID? Destroy (Valid ID is: number > 0)
            if (NetID == 0) Destroy(this);
            
            isLocal = NetworkClient.Instance.Client.Id == netID;
            
            return this;
        }
        
        private void OnEnable()
        {
            // The parent of the hand, also changes position
            // propably both hand and its parent need to be manipulated to get exact position
            handParent = transform.parent;
            
            spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            targetPosition = handParent.transform.position;
        }

        
        // Update all the data, don't send anything
        // Sending is taken care of in PlayerNetworkController.cs
        private void Update()
        {
            // redundant check
            if (NetID == 0) Destroy(this);
            
            // shorthand variables for transform data
            Vector3 pos = handParent.transform.position;
            
            if (!isLocal)
            {
                // Smoothly lerp towards target position and rotation
                handParent.transform.position = Vector3.Lerp(handParent.transform.position, targetPosition, positionLerpSpeed * Time.deltaTime);
                // If first update or too far away, just teleport
                if (!hasReceivedFirstUpdate || Vector3.Distance(handParent.transform.position, pos) > 5f)
                {
                    LogManager.Client.Warn($"Player Hand Position for ID {NetID} teleported");
                    handParent.transform.position = pos;
                    hasReceivedFirstUpdate = true;
                }
            }
            else
            {
                // Player is local, set final vars for sending
                handState = spriteRenderer.sprite.name;
                handPosition = handParent.transform.position;
            }
        }

        
        /// <summary>
        /// Updates the position for a networked hand
        /// <param name="pos">Hand Position (Global Vector3)</param>
        /// </summary>
        public void UpdateHandPosition(Vector3 pos)
        {
            targetPosition = pos;
        }

        
        /// <summary>
        /// Updates the visual state of the hand
        /// </summary>
        /// <param name="state">The name of the state (used for sprite)</param>
        public void UpdateHandState(string state)
        {
            if (lastState == state) return;
            
            var gotSprite = SpriteCache.Get(state);
            if (gotSprite != null)
            {
                spriteRenderer.sprite = SpriteCache.Get(state);
            }
            else
            {
                LogManager.Client.Error($"Hand state not found: {state}");
            }

            lastState = state;
        }
    }
    
    /// <summary>
    /// Class for loading/storing certain Sprites, e.g., Hands.
    /// </summary>
    public static class SpriteCache
    {
        private static Dictionary<string, Sprite> _cache;
        
        /// <summary>
        /// Preloads all the sprites that we want to use
        /// <example>
        /// SpriteCache.Preload("Hands_idle", "Hands_grab", "HandsReach")
        /// </example>
        /// </summary>
        /// <param name="spriteNames">Sprites to load</param>
        public static void Preload(params string[] spriteNames)
        {
            if (_cache != null) return; // only once

            var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            
            _cache = new Dictionary<string, Sprite>(spriteNames.Length);

            var lookup = new HashSet<string>(spriteNames);
            foreach (var s in allSprites)
            {
                if (lookup.Contains(s.name))
                    _cache[s.name] = s;
            }
        }

        /// <summary>
        /// Retrieve the <see cref="Sprite"/> object we preloaded
        /// </summary>
        /// <param name="name">The Name of the <see cref="Sprite"/> we want to get</param>
        /// <returns>The actual <see cref="Sprite"/> that was preloaded, if not found <c>null</c></returns>
        public static Sprite Get(string name)
        {
            if (_cache != null && _cache.TryGetValue(name, out var s))
                return s;
            return null;
        }
    }
}

