using UnityEngine;

[DisallowMultipleComponent]
public class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("Hold Configuration")]
    [Tooltip("Local position offset when attached to the player's hand")]
    [SerializeField] private Vector3 holdPositionOffset = Vector3.zero;

    [Tooltip("Local rotation (Euler angles) offset when attached to the player's hand")]
    [SerializeField] private Vector3 holdRotationOffset = Vector3.zero;

    [Header("Item Details")]
    [SerializeField] private string itemName = "";

    public Vector3 HoldPositionOffset
    {
        get => holdPositionOffset;
        set => holdPositionOffset = value;
    }

    public Vector3 HoldRotationOffset
    {
        get => holdRotationOffset;
        set => holdRotationOffset = value;
    }

    public string ItemName => string.IsNullOrEmpty(itemName) ? gameObject.name : itemName;

    public virtual void OnPickUp(GameObject player)
    {
    }

    public virtual void OnDrop()
    {
    }
}
