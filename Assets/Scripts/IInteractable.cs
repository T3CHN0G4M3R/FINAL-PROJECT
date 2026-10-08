using UnityEngine;

public interface IInteractable
{
    void OnPickUp(GameObject player);
    void OnDrop();
    Vector3 HoldPositionOffset { get; }
    Vector3 HoldRotationOffset { get; }
    string ItemName { get; }
}
