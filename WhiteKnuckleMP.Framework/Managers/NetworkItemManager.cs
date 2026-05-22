using System.Collections.Generic;
using Riptide;
using UnityEngine;
using WhiteKnuckleMP.Networking;
using WhiteKnuckleMP.Networking.Messages;
using WhiteKnuckleMP.Utils;

namespace WhiteKnuckleMP.Framework.Managers;

public class NetworkItemManager : MonoBehaviour
{
    public static NetworkItemManager Instance { get; private set; } = null!;
    
    // Map for unique Item Net Id
    private readonly Dictionary<string, Item_Object> _trackedItems = new();
    private readonly Dictionary<string, Vector3> _lastSentPositions = new();

    private const float SendRate = 0.05f; // Send 20 times per second
    private float _sendTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterItem(Item_Object itemObj)
    {
        if (itemObj == null || itemObj.itemData == null) return;
        
        string guid = itemObj.itemData.itemGUID;
        if (string.IsNullOrEmpty(guid)) return;

        if (!_trackedItems.TryAdd(guid, itemObj)) return;

        _lastSentPositions[guid] = itemObj.transform.position;
        LogManager.Framework.Debug($"[ItemManager] Tracking item: {itemObj.name} | GUID: {guid}");
    }

    public void ClearRegistry()
    {
        _trackedItems.Clear();
        _lastSentPositions.Clear();
    }

    private void Update()
    {
        if (!NetworkManager.Instance.IsServer) return;
        
        _sendTimer += Time.unscaledDeltaTime;
        if (_sendTimer >= SendRate)
        {
            _sendTimer = 0f;
            BroadcastItemUpdates();
        }
    }

    private void BroadcastItemUpdates()
    {
        var updatesToPack = new List<ItemStateUpdate>();

        foreach (var kvp in _trackedItems)
        {
            string guid = kvp.Key;
            Item_Object item = kvp.Value;
            
            if (!item) continue;
            
            var distanceMoved = Vector3.Distance(item.transform.position, _lastSentPositions[guid]);
            // sync only if moved, to save network bandwidth
            if (distanceMoved > 0.01f)
            {
                updatesToPack.Add(new ItemStateUpdate
                {
                    ItemGuid = guid,
                    Position = item.transform.position,
                    Rotation = item.transform.rotation
                });
                
                _lastSentPositions[guid] = item.transform.position;
            }
        }
        
        // If nothing updated, dont send packet
        if (updatesToPack.Count == 0) return;

        var batchMessage = new ItemBatchSyncMessage(updatesToPack);
        // We do not actually care if others receive it, they will receive it either way
        using var packet = new NetworkPacket(batchMessage.MessageId, MessageSendMode.Unreliable);
        batchMessage.WriteTo(packet);
        
        NetworkManager.Instance.Server.SendToAll(packet.RawMessage);
    }
    
    #region Message Handlers

    [MessageHandler(MessageIds.ItemSync)]
    public static void HandleItemBatchUpdateServer(ushort fromNetId, Riptide.Message message)
    {
        if (NetworkManager.Instance.IsServer)
        {
            NetworkManager.Instance.Server.SendToAll(message);
        }
    }
    
    [MessageHandler(MessageIds.ItemSync)]
    public static void HandleItemBatchUpdateClient(Riptide.Message message)
    {
        if (NetworkManager.Instance.IsServer) return;

        using var packet = new NetworkPacket(message);
        var batch = ItemBatchSyncMessage.FromPacket(packet);

        foreach (var update in batch.Updates)
        {
            if (Instance._trackedItems.TryGetValue(update.ItemGuid, out var itemObj))
            {
                if (itemObj == null) continue;
                
                // TODO: Interpolate
                itemObj.transform.position = update.Position;
                itemObj.transform.rotation = update.Rotation;
            }
        }
    }
    
    #endregion
}