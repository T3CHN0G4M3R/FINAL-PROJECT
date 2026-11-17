using UnityEngine;

[DisallowMultipleComponent]
public sealed class SignSceneInteraction : MonoBehaviour
{
    [Header("Sign")]
    [SerializeField] private Collider _signCollider;
    [Header("Cinematic Route")]
    [SerializeField] private string _cinematicScene = "Assets/Scenes/NeighborHoodToGarden.unity";
    [SerializeField] private string _gameplayScene = "Assets/Scenes/GardenScene.unity";
    [SerializeField] private UIAlphaFadeEffect _fadeEffect;
    [SerializeField] private UnityEngine.UI.Image _fadeImagePrefab;
    [SerializeField, Min(0f)] private float _fadeOutSeconds = 1f;
    [SerializeField, Min(0f)] private float _fadeInSeconds = 1f;
    [SerializeField, Min(0f)] private float _blackHoldSeconds = 0.2f;
    private readonly RaycastHit[] _hits = new RaycastHit[64];
    public string Error { get; private set; }

    public bool Owns(GameObject target) => target != null && (target == gameObject || target.transform.IsChildOf(transform));

    public bool CanInteract(Transform player, Camera view, GameObject held, float range, LayerMask mask)
    {
        if (!isActiveAndEnabled || _signCollider == null || !_signCollider.enabled || view == null || player == null ||
            PauseMenuManager.IsGamePaused || NewGameSceneSequence.IntroActive ||
            (GameSaveManager.Instance != null && (GameSaveManager.Instance.Busy || GameSaveManager.Instance.EncounterInProgress))) return false;
        Ray ray = view.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int count = Physics.RaycastNonAlloc(ray, _hits, range, mask, QueryTriggerInteraction.Ignore);
        if (count == _hits.Length) return false;
        Collider nearest = null;
        float distance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var hit = _hits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(player) ||
                (held != null && hit.collider.transform.IsChildOf(held.transform))) continue;
            if (hit.distance >= distance) continue;
            nearest = hit.collider;
            distance = hit.distance;
        }
        return nearest == _signCollider;
    }

    public bool TryTravel(Transform player, Camera view, GameObject held, float range, LayerMask mask)
    {
        if (!CanInteract(player, view, held, range, mask)) return false;
        if (NewGameSceneSequence.BeginGameplayCinematic(_fadeEffect, _fadeImagePrefab, _cinematicScene,
            _gameplayScene, _fadeOutSeconds, _fadeInSeconds, _blackHoldSeconds, out string error))
        { Error = null; return true; }
        Error = error ?? "Unable to start cinematic.";
        if (GameSaveManager.Instance != null) GameSaveManager.Instance.Report(Error);
        return false;
    }
}
