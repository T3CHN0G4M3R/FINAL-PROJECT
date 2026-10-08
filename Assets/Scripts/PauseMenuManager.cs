using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseMenuManager : MonoBehaviour
{
    public static bool IsGamePaused { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private GameObject mainPauseView;
    [SerializeField] private GameObject settingsView;

    [Header("Settings Sub-Panels")]
    [SerializeField] private GameObject panelGame;
    [SerializeField] private GameObject panelControls;
    [SerializeField] private GameObject panelVideo;

    [Header("Settings Tab Highlights")]
    [SerializeField] private GameObject lineGame;
    [SerializeField] private GameObject lineControls;
    [SerializeField] private GameObject lineVideo;

    [Header("Gameplay UI")]
    [SerializeField] private GameObject crosshair;

    [Header("Player References")]
    [SerializeField] private StarterAssetsInputs playerInputs;
    [SerializeField] private FirstPersonController playerController;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip whooshSound;

    [Header("Game Settings Controls")]
    [SerializeField] private TMP_Text normalDiffText;
    [SerializeField] private TMP_Text hardDiffText;
    [SerializeField] private GameObject normalDiffLine;
    [SerializeField] private GameObject hardDiffLine;
    [SerializeField] private TMP_Text showHudText;
    [SerializeField] private TMP_Text toolTipsText;
    [SerializeField] private Slider musicSlider;

    [Header("Controls Settings Controls")]
    [SerializeField] private Slider sensitivityXSlider;
    [SerializeField] private TMP_Text sensitivityXValText;
    [SerializeField] private Slider sensitivityYSlider;
    [SerializeField] private TMP_Text sensitivityYValText;
    [SerializeField] private TMP_Text invertMouseText;

    [Header("Video Settings Controls")]
    [SerializeField] private TMP_Text fullscreenText;
    [SerializeField] private TMP_Text vsyncText;
    [SerializeField] private GameObject shadowOffLine;
    [SerializeField] private GameObject shadowLowLine;
    [SerializeField] private GameObject shadowHighLine;
    [SerializeField] private GameObject textureLowLine;
    [SerializeField] private GameObject textureMedLine;
    [SerializeField] private GameObject textureHighLine;
    [SerializeField] private GameObject aaOffLine;
    [SerializeField] private GameObject aa2xLine;
    [SerializeField] private GameObject aa4xLine;
    [SerializeField] private GameObject aa8xLine;

    private void Awake()
    {
        AudioListener.volume = 1f;
        AudioListener.pause = false;

        if (audioSource != null)
        {
            audioSource.ignoreListenerPause = true;
        }

        if (playerInputs == null)
            playerInputs = FindFirstObjectByType<StarterAssetsInputs>();
        if (playerController == null)
            playerController = FindFirstObjectByType<FirstPersonController>();
    }

    private IEnumerator Start()
    {
        // Ensure paused state is clean on start
        IsGamePaused = false;
        Time.timeScale = 1f;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);
        if (mainPauseView != null)
            mainPauseView.SetActive(true);
        if (settingsView != null)
            settingsView.SetActive(false);

        InitSettingsFromPlayerPrefs();

        LockCursor();
        yield return null;
        LockCursor();
        yield return new WaitForSecondsRealtime(0.1f);
        LockCursor();
    }

    public void LockCursor()
    {
        if (IsGamePaused) return;

        if (playerInputs != null)
        {
            playerInputs.cursorLocked = true;
            playerInputs.cursorInputForLook = true;
            playerInputs.SetCursorState(true);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        bool escPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            escPressed = true;
        }
#endif
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            escPressed = true;
        }

        if (escPressed)
        {
            if (IsGamePaused)
            {
                if (settingsView != null && settingsView.activeSelf)
                {
                    CloseSettings();
                }
                else
                {
                    ResumeGame();
                }
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        IsGamePaused = true;
        Time.timeScale = 0f;

        if (crosshair != null)
            crosshair.SetActive(false);

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(true);
        if (mainPauseView != null)
            mainPauseView.SetActive(true);
        if (settingsView != null)
            settingsView.SetActive(false);

        // Unlock cursor for UI interaction
        if (playerInputs != null)
        {
            playerInputs.cursorLocked = false;
            playerInputs.cursorInputForLook = false;
            playerInputs.move = Vector2.zero;
            playerInputs.look = Vector2.zero;
            playerInputs.SetCursorState(false);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        PlayWhoosh();
    }

    public void ResumeGame()
    {
        IsGamePaused = false;
        Time.timeScale = 1f;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);
        if (settingsView != null)
            settingsView.SetActive(false);
        if (mainPauseView != null)
            mainPauseView.SetActive(true);

        if (crosshair != null)
            crosshair.SetActive(true);

        // Lock cursor back to first person camera
        if (playerInputs != null)
        {
            playerInputs.cursorLocked = true;
            playerInputs.cursorInputForLook = true;
            playerInputs.move = Vector2.zero;
            playerInputs.look = Vector2.zero;
            playerInputs.SetCursorState(true);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        PlayClick();
    }

    public void OpenSettings()
    {
        if (mainPauseView != null)
            mainPauseView.SetActive(false);
        if (settingsView != null)
            settingsView.SetActive(true);

        ShowGameTab();
        PlayClick();
    }

    public void CloseSettings()
    {
        if (settingsView != null)
            settingsView.SetActive(false);
        if (mainPauseView != null)
            mainPauseView.SetActive(true);

        PlayClick();
    }

    public void ExitToMainMenu()
    {
        PlayClick();
        IsGamePaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }

    // --- Tab Navigation ---

    public void ShowGameTab()
    {
        if (panelGame) panelGame.SetActive(true);
        if (panelControls) panelControls.SetActive(false);
        if (panelVideo) panelVideo.SetActive(false);

        if (lineGame) lineGame.SetActive(true);
        if (lineControls) lineControls.SetActive(false);
        if (lineVideo) lineVideo.SetActive(false);

        PlayClick();
    }

    public void ShowControlsTab()
    {
        if (panelGame) panelGame.SetActive(false);
        if (panelControls) panelControls.SetActive(true);
        if (panelVideo) panelVideo.SetActive(false);

        if (lineGame) lineGame.SetActive(false);
        if (lineControls) lineControls.SetActive(true);
        if (lineVideo) lineVideo.SetActive(false);

        PlayClick();
    }

    public void ShowVideoTab()
    {
        if (panelGame) panelGame.SetActive(false);
        if (panelControls) panelControls.SetActive(false);
        if (panelVideo) panelVideo.SetActive(true);

        if (lineGame) lineGame.SetActive(false);
        if (lineControls) lineControls.SetActive(false);
        if (lineVideo) lineVideo.SetActive(true);

        PlayClick();
    }

    // --- Settings Logic & PlayerPrefs ---

    private void InitSettingsFromPlayerPrefs()
    {
        // 1. Game Settings
        int normalDiff = PlayerPrefs.GetInt("NormalDifficulty", 1);
        SetDifficulty(normalDiff == 1);

        int showHud = PlayerPrefs.GetInt("ShowHUD", 1);
        if (showHudText) showHudText.text = showHud == 1 ? "on" : "off";

        int tooltips = PlayerPrefs.GetInt("ToolTips", 1);
        if (toolTipsText) toolTipsText.text = tooltips == 1 ? "on" : "off";

        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        if (musicSlider)
        {
            musicSlider.value = musicVol;
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        // 2. Controls Settings
        float sensX = PlayerPrefs.GetFloat("XSensitivity", 1f);
        if (sensitivityXSlider)
        {
            sensitivityXSlider.value = sensX;
            if (sensitivityXValText) sensitivityXValText.text = sensX.ToString("F1");
            sensitivityXSlider.onValueChanged.AddListener(OnSensitivityXChanged);
        }
        if (playerController != null)
        {
            playerController.RotationSpeed = sensX;
        }

        float sensY = PlayerPrefs.GetFloat("YSensitivity", 1f);
        if (sensitivityYSlider)
        {
            sensitivityYSlider.value = sensY;
            if (sensitivityYValText) sensitivityYValText.text = sensY.ToString("F1");
            sensitivityYSlider.onValueChanged.AddListener(OnSensitivityYChanged);
        }

        int inverted = PlayerPrefs.GetInt("Inverted", 0);
        if (invertMouseText) invertMouseText.text = inverted == 1 ? "on" : "off";

        // 3. Video Settings
        if (fullscreenText) fullscreenText.text = Screen.fullScreen ? "on" : "off";
        if (vsyncText) vsyncText.text = QualitySettings.vSyncCount > 0 ? "on" : "off";

        int shadows = PlayerPrefs.GetInt("Shadows", 2);
        UpdateShadowUI(shadows);

        int textures = PlayerPrefs.GetInt("Textures", 2);
        UpdateTextureUI(textures);

        int aa = PlayerPrefs.GetInt("AntiAliasing", 4);
        UpdateAntiAliasingUI(aa);
    }

    // Game
    public void ToggleNormalDifficulty()
    {
        SetDifficulty(true);
        PlayClick();
    }

    public void ToggleHardcoreDifficulty()
    {
        SetDifficulty(false);
        PlayClick();
    }

    private void SetDifficulty(bool isNormal)
    {
        PlayerPrefs.SetInt("NormalDifficulty", isNormal ? 1 : 0);
        PlayerPrefs.SetInt("HardCoreDifficulty", isNormal ? 0 : 1);
        if (normalDiffLine) normalDiffLine.SetActive(isNormal);
        if (hardDiffLine) hardDiffLine.SetActive(!isNormal);
    }

    public void ToggleShowHUD()
    {
        int cur = PlayerPrefs.GetInt("ShowHUD", 1);
        cur = (cur == 1) ? 0 : 1;
        PlayerPrefs.SetInt("ShowHUD", cur);
        if (showHudText) showHudText.text = cur == 1 ? "on" : "off";
        PlayClick();
    }

    public void ToggleToolTips()
    {
        int cur = PlayerPrefs.GetInt("ToolTips", 1);
        cur = (cur == 1) ? 0 : 1;
        PlayerPrefs.SetInt("ToolTips", cur);
        if (toolTipsText) toolTipsText.text = cur == 1 ? "on" : "off";
        PlayClick();
    }

    private void OnMusicVolumeChanged(float val)
    {
        PlayerPrefs.SetFloat("MusicVolume", val);
        if (audioSource != null)
        {
            audioSource.volume = val;
        }
    }

    // Controls
    public void OnSensitivityXChanged(float val)
    {
        PlayerPrefs.SetFloat("XSensitivity", val);
        if (sensitivityXValText) sensitivityXValText.text = val.ToString("F1");
        if (playerController != null)
        {
            playerController.RotationSpeed = val;
        }
    }

    public void OnSensitivityYChanged(float val)
    {
        PlayerPrefs.SetFloat("YSensitivity", val);
        if (sensitivityYValText) sensitivityYValText.text = val.ToString("F1");
    }

    public void ToggleInvertMouse()
    {
        int cur = PlayerPrefs.GetInt("Inverted", 0);
        cur = (cur == 1) ? 0 : 1;
        PlayerPrefs.SetInt("Inverted", cur);
        if (invertMouseText) invertMouseText.text = cur == 1 ? "on" : "off";
        PlayClick();
    }

    // Video
    public void ToggleFullScreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        if (fullscreenText) fullscreenText.text = Screen.fullScreen ? "on" : "off";
        PlayClick();
    }

    public void ToggleVSync()
    {
        int next = (QualitySettings.vSyncCount == 0) ? 1 : 0;
        QualitySettings.vSyncCount = next;
        if (vsyncText) vsyncText.text = next == 1 ? "on" : "off";
        PlayClick();
    }

    public void SetShadowsOff()
    {
        QualitySettings.shadowCascades = 0;
        QualitySettings.shadowDistance = 0;
        PlayerPrefs.SetInt("Shadows", 0);
        UpdateShadowUI(0);
        PlayClick();
    }

    public void SetShadowsLow()
    {
        QualitySettings.shadowCascades = 2;
        QualitySettings.shadowDistance = 75;
        PlayerPrefs.SetInt("Shadows", 1);
        UpdateShadowUI(1);
        PlayClick();
    }

    public void SetShadowsHigh()
    {
        QualitySettings.shadowCascades = 4;
        QualitySettings.shadowDistance = 500;
        PlayerPrefs.SetInt("Shadows", 2);
        UpdateShadowUI(2);
        PlayClick();
    }

    private void UpdateShadowUI(int level)
    {
        if (shadowOffLine) shadowOffLine.SetActive(level == 0);
        if (shadowLowLine) shadowLowLine.SetActive(level == 1);
        if (shadowHighLine) shadowHighLine.SetActive(level == 2);
    }

    public void SetTexturesLow()
    {
        QualitySettings.globalTextureMipmapLimit = 2;
        PlayerPrefs.SetInt("Textures", 0);
        UpdateTextureUI(0);
        PlayClick();
    }

    public void SetTexturesMed()
    {
        QualitySettings.globalTextureMipmapLimit = 1;
        PlayerPrefs.SetInt("Textures", 1);
        UpdateTextureUI(1);
        PlayClick();
    }

    public void SetTexturesHigh()
    {
        QualitySettings.globalTextureMipmapLimit = 0;
        PlayerPrefs.SetInt("Textures", 2);
        UpdateTextureUI(2);
        PlayClick();
    }

    private void UpdateTextureUI(int level)
    {
        if (textureLowLine) textureLowLine.SetActive(level == 0);
        if (textureMedLine) textureMedLine.SetActive(level == 1);
        if (textureHighLine) textureHighLine.SetActive(level == 2);
    }

    public void SetAntiAliasingOff()
    {
        QualitySettings.antiAliasing = 0;
        PlayerPrefs.SetInt("AntiAliasing", 0);
        UpdateAntiAliasingUI(0);
        PlayClick();
    }

    public void SetAntiAliasing2X()
    {
        QualitySettings.antiAliasing = 2;
        PlayerPrefs.SetInt("AntiAliasing", 2);
        UpdateAntiAliasingUI(2);
        PlayClick();
    }

    public void SetAntiAliasing4X()
    {
        QualitySettings.antiAliasing = 4;
        PlayerPrefs.SetInt("AntiAliasing", 4);
        UpdateAntiAliasingUI(4);
        PlayClick();
    }

    public void SetAntiAliasing8X()
    {
        QualitySettings.antiAliasing = 8;
        PlayerPrefs.SetInt("AntiAliasing", 8);
        UpdateAntiAliasingUI(8);
        PlayClick();
    }

    private void UpdateAntiAliasingUI(int level)
    {
        if (aaOffLine) aaOffLine.SetActive(level == 0);
        if (aa2xLine) aa2xLine.SetActive(level == 2);
        if (aa4xLine) aa4xLine.SetActive(level == 4);
        if (aa8xLine) aa8xLine.SetActive(level == 8);
    }

    // Audio Helpers
    public void PlayClick()
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    public void PlayWhoosh()
    {
        if (audioSource != null && whooshSound != null)
        {
            audioSource.PlayOneShot(whooshSound);
        }
    }
}
