using UnityEngine;

public interface IUsable
{
    void Use(GameObject user);
    bool IsInUse { get; }
}
