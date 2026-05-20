using System;
using UnityEngine;

namespace WhiteKnuckleMP.Framework.Controllers.Player;

public class PlayerStateReceiver : MonoBehaviour
{
    private ushort _netID;
    
    private Vector3 _targetPosition;
    private Quaternion _targetRotation;
    private const float LerpSpeed = 5f;
    private bool _hadFirstUpdate;

    private string _currentLeftItem = "None";
    private string _currentRightItem = "None";

    public event Action<string>? OnLeftItemChanged;
    public event Action<string>? OnRightItemChanged;

    public void Initialize(ushort netId)
    {
        _netID = netId;
        // TODO: Implement back hand sync
        _targetPosition = transform.position;
        _targetRotation = transform.rotation;
    }

    private void Update()
    {
        if (!_hadFirstUpdate) return;
        
        transform.position = Vector3.Lerp(transform.position, _targetPosition, LerpSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, _targetRotation, LerpSpeed * Time.deltaTime);
    }
    
    /// <summary>
    /// Applies network data to the player's state.
    /// </summary>
    /// <param name="pos">The new position of the player.</param>
    /// <param name="rot">The new rotation of the player.</param>
    /// <param name="leftItem">The new item in the left hand.</param>
    /// <param name="rightItem">The new item in the right hand.</param>
    public void ApplyNetworkData(Vector3 pos, Quaternion rot, string leftItem, string rightItem)
    {
        if (!_hadFirstUpdate || Vector3.Distance(transform.position, pos) > 5)
        {
            // Snap if too far
            transform.position = pos;
        }
        
        _targetPosition = pos;
        _targetRotation = rot;
        _hadFirstUpdate = true;
        
        // TODO: Update hands
        
        // Fire Events if items changed.
        if (leftItem != _currentLeftItem)
        {
            _currentLeftItem = leftItem;
            OnLeftItemChanged?.Invoke(leftItem);
        }

        if (rightItem != _currentRightItem)
        {
            _currentRightItem = rightItem;
            OnRightItemChanged?.Invoke(rightItem);
        }
    }
}