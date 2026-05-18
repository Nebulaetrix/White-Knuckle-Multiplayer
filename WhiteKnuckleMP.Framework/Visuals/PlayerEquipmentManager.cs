using UnityEngine;
using WhiteKnuckleMP.Framework.Controllers;
using WhiteKnuckleMP.Framework.Controllers.Player;
using WhiteKnuckleMP.Utils;

namespace WhiteKnuckleMP.Framework.Visuals;

public class PlayerEquipmentManager : MonoBehaviour
{
    private Transform _leftHandParent;
    private Transform _rightHandParent;
    private GameObject _leftItemInstance;
    private GameObject _rightItemInstance;

    /// <summary>
    /// Binds this equipment manager to a player state receiver, allowing it to react to changes in the player's held items.
    /// </summary>
    /// <param name="receiver">The player state receiver to bind to.</param>
    public void BindToReceiver(PlayerStateReceiver receiver)
    {
        // TODO: CORRERERERECT
        _leftHandParent = TransformUtils.FindChildRecursive(transform, "Item_Hand_Left");
        if (_leftHandParent == null)
        {
            LogManager.Net.Error("Left hand parent not found.");
        }

        _rightHandParent = TransformUtils.FindChildRecursive(transform, "Item_Hand_Right");
        if (_rightHandParent == null)
        {
            LogManager.Error("Right hand parent not found.");
        }
        
        receiver.OnLeftItemChanged += HandleLeftItemSpawn;
        receiver.OnRightItemChanged += HandleRightItemSpawn;
    }

    /// <summary>
    /// Handles the left item spawn event.
    /// </summary>
    /// <param name="prefabName">The name of the prefab to instantiate.</param>
    private void HandleLeftItemSpawn(string prefabName)
    {
        if (_leftItemInstance != null) Destroy(_leftItemInstance);
        if (prefabName.ToLower() == "none") return;

        GameObject prefab = CL_AssetManager.GetAssetGameObject(prefabName);
        if (prefab != null)
        {
            _leftItemInstance = Instantiate(prefab, _leftHandParent);
            _leftItemInstance.transform.localPosition = Vector3.zero;
            _leftItemInstance.transform.localRotation = Quaternion.identity;

            if (_leftItemInstance.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.useGravity = false;
                rb.isKinematic = true;
            }
        }
    }

    /// <summary>
    /// Handles the right item spawn event.
    /// </summary>
    /// <param name="prefabName">The name of the prefab to instantiate.</param>
    private void HandleRightItemSpawn(string prefabName)
    {
        if (_rightItemInstance != null) Destroy(_rightItemInstance);
        if (prefabName.ToLower() == "none") return;

        GameObject prefab = CL_AssetManager.GetAssetGameObject(prefabName);
        if (prefab != null)
        {
            _rightItemInstance = Instantiate(prefab, _rightHandParent);
            _rightItemInstance.transform.localPosition = Vector3.zero;
            _rightItemInstance.transform.localRotation = Quaternion.identity;

            if (_rightItemInstance.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.useGravity = false;
                rb.isKinematic = true;
            }
        }
    }
}