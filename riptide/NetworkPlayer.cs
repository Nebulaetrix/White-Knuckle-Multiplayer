using System;
using UnityEngine;
using Riptide;

namespace White_Knuckle_Multiplayer.Networking
{
    
    public class NetworkPlayer : MonoBehaviour
    {
        // The client ID this player belongs to
        public ushort Id { get; private set; }
        
        // Reference to the main RiptideNetworking component
        private RiptideNetworking networking;
        
        
        private Transform cachedTransform;
        
        // Reference to the actual CL_Player in the scene
        private GameObject clPlayer;
        private Transform clPlayerTransform;
        
        // Lerp settings
        [SerializeField] private float positionLerpSpeed = 15f; // Fast lerp = Smoother movement?? I think so.
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool hasReceivedFirstUpdate = false;
        
        // Network update settings
        private float updateTimer = 0f;
        private float updateInterval = 0.05f; //  updates 20 per second
        private int maxFindAttempts = 3;
        private int findAttempts = 0;
        
        // Check to see if the player has moved enough to send an update.
        private Vector3 lastSentPosition;
        private float significantMovementThreshold = 0.1f;
        
        // Bool for local playe
        private bool isLocalPlayer;
        
        // Message IDs for this component
        private enum MessageId : ushort
        {
            PlayerPosition = 10,
            PlayerRotation = 11,
            PlayerAction = 12
        }
        
        private void Awake()
        {
            cachedTransform = transform;
            networking = FindObjectOfType<RiptideNetworking>();
            
            if (networking == null)
            {
                Debug.LogError("No RiptideNetworking found in the scene!");
            }
            
            // Start position tracking but less frequently
            InvokeRepeating("CheckPlayerMovement", 1.0f, 2.0f);
        }
        
        private void Start()
        {
            if (networking == null)
            {
                networking = FindObjectOfType<RiptideNetworking>();
                if (networking == null)
                {
                    Debug.LogError("No RiptideNetworking found in the scene!");
                    return;
                }
            }
            
            // Determine if this is the local player
            isLocalPlayer = IsLocalPlayer();
            
            // Log detection details - important for debugging host vs client
            if (networking.IsServer)
            {
                Debug.Log($"Player {Id} on HOST side - isLocalPlayer: {isLocalPlayer}, ClientId: {networking.GetClientId()}");
            }
            else
            {
                Debug.Log($"Player {Id} on CLIENT side - isLocalPlayer: {isLocalPlayer}, ClientId: {networking.GetClientId()}");
            }
            
            // Try to find the CL_Player
            FindCLPlayer();
        }
        
        private void LateUpdate()
        {
            
            if (isLocalPlayer && networking != null && Id != 0)
            {
                // do this to prevent lag
                updateTimer -= Time.deltaTime;
                
                if (clPlayerTransform != null)
                {
                    // Check for big changes in position
                    float distanceMoved = Vector3.Distance(clPlayerTransform.position, lastSentPosition);
                    bool significantMovement = distanceMoved > significantMovementThreshold;
                    
                    if (updateTimer <= 0 || significantMovement)
                    {
                        updateTimer = updateInterval;
                        lastSentPosition = clPlayerTransform.position;
                        
                        // Host and client both use the same method to send position, yet it doesn't work :()
                        SendPositionUpdateFromCLPlayer();
                        
                        // Log every position update for debugging, this could be turned off to makke client less laggy.
                        Debug.Log($"POSITION SENT: {(networking.IsServer ? "HOST" : "CLIENT")} player {Id} at {lastSentPosition}");
                    }
                }
                else if (updateTimer <= 0 && findAttempts < maxFindAttempts)
                {
                    // TOD: REMOVE
                    updateTimer = updateInterval;
                    findAttempts++;
                    FindCLPlayer();
                    Debug.Log($"Attempting to find CL_Player (attempt {findAttempts}/{maxFindAttempts})");
                }
            }
        }
        
        private void Update()
        {
            // Remote player interpolation
            if (!isLocalPlayer && hasReceivedFirstUpdate && cachedTransform != null)
            {
                // Smoothly move to target position and rotation using good erping
                cachedTransform.position = Vector3.Lerp(cachedTransform.position, targetPosition, Time.deltaTime * positionLerpSpeed);
                cachedTransform.rotation = Quaternion.Slerp(cachedTransform.rotation, targetRotation, Time.deltaTime * positionLerpSpeed);
            }
        }
        
        // Find the CL_Player GameObject in scene
        private void FindCLPlayer()
        {
            try
            {
                if (clPlayer != null) return; // Already found
                
                // Try to find the CL_Player in the scene
                clPlayer = GameObject.Find("CL_Player");
                
                if (clPlayer != null)
                {
                    clPlayerTransform = clPlayer.transform;
                    lastSentPosition = clPlayerTransform.position;
                    findAttempts = maxFindAttempts; // Stop trying once found
                }
                else
                {
                    // Try to find by tag as fallback, but only if this is our first attempt
                    if (findAttempts <= 1)
                    {
                        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
                        if (players.Length > 0)
                        {
                            clPlayer = players[0];
                            clPlayerTransform = clPlayer.transform;
                            lastSentPosition = clPlayerTransform.position;
                            findAttempts = maxFindAttempts; 
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error finding CL_Player: {ex.Message}");
                findAttempts = maxFindAttempts; 
            }
        }
        
        // Send position update using the CL_Player position
        public void SendPositionUpdateFromCLPlayer()
        {
            // SIMPLIFIED: Same condition for both host and client
            if (networking == null || Id == 0 || clPlayerTransform == null) 
                return;
            
            // Use the CL_Player position and rotation directly
            Vector3 currentPos = clPlayerTransform.position;
            Quaternion currentRot = clPlayerTransform.rotation;
            
            try
            {
                Debug.Log($"CREATING POSITION MESSAGE: Player {Id} at {currentPos} (IsServer: {networking.IsServer}, IsClient: {networking.IsClient})");
                
                // Create position message exactly the same way for both host and client
                Riptide.Message message = Riptide.Message.Create(MessageSendMode.Unreliable, (ushort)RiptideNetworking.MessageId.PlayerMovement);
                message.Add(Id);
                RiptideNetworking.AddVector3(message, currentPos);
                RiptideNetworking.AddQuaternion(message, currentRot);
                
                // Host and client handle sending differently
                if (networking.IsServer)
                {
                    // Host sends directly to all clients
                    Debug.Log($"HOST SENDING: Position update to all clients");
                    networking.SendToAll(message);
                }
                else
                {
                    // Client sends to server
                    Debug.Log($"CLIENT SENDING: Position update to server");
                    networking.SendToServer(message);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending position: {ex.Message}");
            }
        }
        
        // Not needed, just in case.
        public void SendPositionUpdate()
        {
            if (networking == null || !networking.IsClient || Id == 0) 
                return;
            
            if (cachedTransform == null)
                cachedTransform = transform;
            
            Vector3 currentPos = cachedTransform.position;
            Quaternion currentRot = cachedTransform.rotation;
            lastSentPosition = currentPos;
            
            try
            {
                Riptide.Message message = Riptide.Message.Create(MessageSendMode.Unreliable, (ushort)RiptideNetworking.MessageId.PlayerMovement);
                message.Add(Id);
                RiptideNetworking.AddVector3(message, currentPos);
                RiptideNetworking.AddQuaternion(message, currentRot);
                
                networking.SendToServer(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending position: {ex.Message}");
            }
        }
        
        // This should work for people
        public void UpdatePosition(Vector3 newPosition, Quaternion newRotation)
        {
            try
            {
                // Skip if this is our own player, we shoudl be controlling this.
                if (isLocalPlayer)
                {
                    Debug.Log($"Skipping position update for my own player {Id}");
                    return;
                }
                
                // Apply the update to this player's position
                Debug.Log($"Applying position update for player {Id} at {newPosition}");
                
                if (cachedTransform == null)
                    cachedTransform = transform;
                
                // First update snaps, later updates use lerping
                if (!hasReceivedFirstUpdate)
                {
                    // Snap on first update
                    cachedTransform.position = newPosition;
                    cachedTransform.rotation = newRotation;
                    hasReceivedFirstUpdate = true;
                    targetPosition = newPosition;
                    targetRotation = newRotation;
                    Debug.Log($"First position update - snapped player {Id} to {newPosition}");
                }
                else
                {
                    // Set target for lerping
                    targetPosition = newPosition;
                    targetRotation = newRotation;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in UpdatePosition: {ex.Message}");
            }
        }
        
        // Initialize this player with a specific client ID
        public void Initialize(ushort clientId)
        {
            try 
            {
                // Validate client ID - never use ID 0 :)
                if (clientId == 0)
                {
                    Debug.LogError("Cannot initialize player with ID 0, which is invalid!");
                    return;
                }
                
                Id = clientId;
                gameObject.name = $"Player_{Id}";
                
                // Ensure we have a cached transform, better efficiencyyy
                if (cachedTransform == null)
                {
                    cachedTransform = transform;
                }
                
                // Safety check for networking
                if (networking == null)
                {
                    networking = FindObjectOfType<RiptideNetworking>();
                }
                
                // check if this is locla player
                if (networking != null)
                {
                    isLocalPlayer = IsLocalPlayer();
                    
                    // Try to find CL_Player immediately
                    FindCLPlayer();
                    
                    // If found as a remote player, initialize the lerping targets
                    if (!isLocalPlayer)
                    {
                        targetPosition = cachedTransform.position;
                        targetRotation = cachedTransform.rotation;
                    }
                    
                    // Force an immediate position update if this is the local player
                    if (isLocalPlayer && networking.IsClient)
                    {
                        // Use CL_Player position.
                        if (clPlayerTransform != null)
                        {
                            lastSentPosition = clPlayerTransform.position;
                            SendPositionUpdateFromCLPlayer();
                        }
                        else
                        {
                            SendPositionUpdate();
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Failed to find networking for player {Id}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in Initialize for player {clientId}: {ex.Message}");
            }
        }
        
        // Call this method when the server broadcasts an action from a player, use this later!!!
        public void HandleAction(byte actionId, Riptide.Message message)
        {
            // Handle the action based on the actionId
            switch (actionId)
            {
                case 0: // Disconnect action
                    // Player disconnected, remove them if not local
                    if (!isLocalPlayer)
                    {
                        Debug.Log($"Remote player {Id} disconnected");
                        Destroy(gameObject);
                    }
                    break;
                    
                default:
                    break;
            }
        }
        
        // Improved local player check, so many duplicated methods, im so sorry Galfar, I really need to get better at coding oml.
        private bool IsLocalPlayer()
        {
            if (networking == null) return false;
            
            ushort myClientId = networking.GetClientId();
            
            // Log the check every time for better debugging
            Debug.Log($"LOCAL CHECK: Player {Id} checks if local - client ID: {myClientId}, IsServer: {networking.IsServer}, IsClient: {networking.IsClient}");
            
            
            if (networking.IsServer && networking.IsClient)
            {
                // On the host, the host's player must be treated as local
                // to ensure it sends position updates
                Debug.Log($"HOST MODE DETECTION: Player {Id}, Host ID: {myClientId}");
                
                if (Id == myClientId)
                {
                    Debug.Log($"HOST PLAYER FOUND: Player {Id} is the host's local player");
                    return true;
                }
                return false;
            }
            // Pure client mode
            else if (networking.IsClient)
            {
                bool isLocal = Id == myClientId;
                Debug.Log($"CLIENT MODE: Player {Id} local status: {isLocal}");
                return isLocal;
            }
            // Pure server mode (should never really happen in this design)
            else if (networking.IsServer)
            {
                Debug.Log($"PURE SERVER MODE: Player {Id} cannot be local");
                return false;
            }
            
            return false;
        }
        
       
        
    }
}
