using UnityEngine;

[DisallowMultipleComponent]
public sealed class DoorSceneTransition : MonoBehaviour
{
    [Header("Opening Cinematic")]
    [SerializeField] private string _cinematicScene = "Assets/Scenes/MomScene.unity";
    [SerializeField] private string _gameplayScene = "Assets/Scenes/HouseScene.unity";
    [SerializeField] private UIAlphaFadeEffect _fadeEffect;
    [SerializeField] private UnityEngine.UI.Image _fadeImagePrefab;
    [SerializeField, Min(0f)] private float _fadeOutSeconds = 1f;
    [SerializeField, Min(0f)] private float _fadeInSeconds = 1f;
    [SerializeField, Min(0f)] private float _blackHoldSeconds = 0.2f;
    public string Error { get; private set; }

    public bool TryBegin()
    {
        var manager = GameSaveManager.Instance;
        if (!isActiveAndEnabled || manager == null || manager.Busy || manager.EncounterInProgress ||
            PauseMenuManager.IsGamePaused || !Application.isFocused)
        { Error = "Door travel is unavailable."; return false; }
        bool started = NewGameSceneSequence.BeginGameplayCinematic(_fadeEffect, _fadeImagePrefab,
            _cinematicScene, _gameplayScene, _fadeOutSeconds, _fadeInSeconds, _blackHoldSeconds, out string error);
        Error = error;
        if (!started) manager.Report(error ?? "Door travel could not start.");
        return started;
    }
}
