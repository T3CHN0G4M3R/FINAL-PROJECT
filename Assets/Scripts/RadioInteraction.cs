using UnityEngine;

[DisallowMultipleComponent]
public sealed class RadioInteraction : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private PlayerInterferenceController _interference;
    [SerializeField] private Collider _radioCollider;
    [SerializeField] private Collider _playerBody;
    [SerializeField] private InteractionSystem _interactions;
    [Header("Interaction")]
    [Tooltip("Proximity to the actual Radio collider; aiming is not required.")]
    [Min(0.01f)] [SerializeField] private float _interactionRadius = 3f;
    [Tooltip("Solid environment blocking access to the Radio. Triggers/player/held items are ignored.")]
    [SerializeField] private LayerMask _obstructionMask = ~0;
    private readonly RaycastHit[] _hits = new RaycastHit[64];

    public bool CanInteract(Transform player, Camera view)
    {
        if (!isActiveAndEnabled || _interference == null || !_interference.CanUseRadio || player == null ||
            player != _interference.Player || view == null || _radioCollider == null || !_radioCollider.enabled ||
            !_radioCollider.gameObject.activeInHierarchy || _playerBody == null) return false;
        Vector3 body = _playerBody.bounds.center;
        Vector3 nearest = _radioCollider.ClosestPoint(body);
        if ((nearest - body).sqrMagnitude > _interactionRadius * _interactionRadius) return false;
        // Check from the body, not a tall/remote camera, so floors/walls cannot be bypassed.
        Vector3 delta = _radioCollider.bounds.center - body;
        float distance = delta.magnitude;
        if (distance < 0.001f) return true;
        int count = Physics.RaycastNonAlloc(body, delta / distance, _hits, distance, _obstructionMask, QueryTriggerInteraction.Ignore);
        if (count == _hits.Length) return false;
        var held = _interactions != null ? _interactions.HeldObject : null;
        for (int i = 0; i < count; i++)
        {
            Transform hit = _hits[i].transform;
            if (hit == null || hit.IsChildOf(player) || hit.IsChildOf(transform) || (held != null && hit.IsChildOf(held.transform))) continue;
            return false;
        }
        return true;
    }

    public bool TryResetInterference(Transform player, Camera view) => CanInteract(player, view) && _interference.ResetInterference();

    private void OnValidate()
    {
        if (float.IsNaN(_interactionRadius) || float.IsInfinity(_interactionRadius)) _interactionRadius = 3;
        _interactionRadius = Mathf.Max(0.01f, _interactionRadius);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_radioCollider != null ? _radioCollider.bounds.center : transform.position, _interactionRadius);
    }
}
