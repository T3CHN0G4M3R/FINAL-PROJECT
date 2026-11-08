using UnityEngine;

public class GameplaySettingsApplier : MonoBehaviour
{
    [Header("Gameplay References")]
    [SerializeField] private StarterAssets.FirstPersonController playerController;
    [SerializeField] private GameObject crosshair;
    private bool _showHUD;
    private bool _wasPaused;

    private void OnEnable()
    {
        GameSettings.Changed -= ApplySettings;
        GameSettings.Changed += ApplySettings;
        ApplySettings();
    }

    private void Start()
    {
        // Rebind even when entering Play Mode without a domain/scene reload.
        GameSettings.Changed -= ApplySettings;
        GameSettings.Changed += ApplySettings;
        ApplySettings();
    }

    private void OnDisable() => GameSettings.Changed -= ApplySettings;

    private void ApplySettings()
    {
        if (playerController != null)
            playerController.ApplyLookSettings(GameSettings.HorizontalSensitivity, GameSettings.VerticalSensitivity,
                GameSettings.MouseSmoothing, GameSettings.InvertMouse);
        _showHUD = GameSettings.ShowHUD;
        RefreshHUD();
    }

    private void LateUpdate()
    {
        if (_wasPaused != PauseMenuManager.IsGamePaused) RefreshHUD();
    }

    public void RefreshHUD()
    {
        _wasPaused = PauseMenuManager.IsGamePaused;
        if (crosshair != null) crosshair.SetActive(_showHUD && !_wasPaused);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) GameSettings.Save();
    }

    private void OnApplicationQuit() => GameSettings.Save();
}
