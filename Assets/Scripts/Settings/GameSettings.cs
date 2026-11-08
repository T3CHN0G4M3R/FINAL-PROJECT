using System;
using UnityEngine;

public static class GameSettings
{
    public static event Action Changed;

    public static float MusicVolume => ReadFloat("MusicVolume", 0.75f, 1f);
    public static float HorizontalSensitivity => ReadFloat("XSensitivity", 1f, 10f);
    public static float VerticalSensitivity => ReadFloat("YSensitivity", 1f, 10f);
    public static float MouseSmoothing => ReadFloat("MouseSmoothing", 0.5f, 1f);
    public static bool ShowHUD => PlayerPrefs.GetInt("ShowHUD", 1) != 0;
    public static bool ToolTips => PlayerPrefs.GetInt("ToolTips", 1) != 0;
    public static bool InvertMouse => PlayerPrefs.GetInt("Inverted", 0) != 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSubscriptions()
    {
        Changed = null;
        Application.quitting -= Save;
        Application.quitting += Save;
    }

    private static float ReadFloat(string key, float fallback, float maximum)
    {
        float value = PlayerPrefs.GetFloat(key, fallback);
        float valid = float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, 0f, maximum);
        if (value != valid) PlayerPrefs.SetFloat(key, valid);
        return valid;
    }

    private static void SetFloat(string key, float value, float fallback, float maximum)
    {
        value = float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, 0f, maximum);
        PlayerPrefs.SetFloat(key, value);
        Changed?.Invoke();
    }

    private static void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        Changed?.Invoke();
    }

    public static void SetMusicVolume(float value) => SetFloat("MusicVolume", value, 0.75f, 1f);
    public static void SetHorizontalSensitivity(float value) => SetFloat("XSensitivity", value, 1f, 10f);
    public static void SetVerticalSensitivity(float value) => SetFloat("YSensitivity", value, 1f, 10f);
    public static void SetMouseSmoothing(float value) => SetFloat("MouseSmoothing", value, 0.5f, 1f);
    public static void SetShowHUD(bool value) => SetBool("ShowHUD", value);
    public static void SetToolTips(bool value) => SetBool("ToolTips", value);
    public static void SetInvertMouse(bool value) => SetBool("Inverted", value);
    public static void Save() => PlayerPrefs.Save();
}
