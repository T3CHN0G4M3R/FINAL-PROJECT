using System;
using UnityEngine;

public sealed class SaveSlotPanel : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Text _title;
    [SerializeField] private TMPro.TMP_Text _status;
    [SerializeField] private UnityEngine.UI.Button[] _slots;
    [SerializeField] private UnityEngine.UI.Button _confirm;
    [SerializeField] private UnityEngine.UI.Button _cancel;
    [SerializeField] private UnityEngine.UI.Button _extra;
    [SerializeField] private UnityEngine.UI.Button _back;
    private Action _onBack;
    private Action<int> _onSelected;
    private bool _saving;
    private Action _confirmation;
    private Action _extraAction;
    private Action _cancelAction;
    private GameSaveManager _manager;
    private int _recoverSlot = -1;
    public bool IsOpen => gameObject.activeSelf;
    private void Awake()
    {
        for (int i = 0; i < _slots.Length; i++) { int slot = i; _slots[i].onClick.AddListener(() => Choose(slot)); }
        _confirm.onClick.AddListener(() => _confirmation?.Invoke());
        _cancel.onClick.AddListener(() => _cancelAction?.Invoke());
        _extra.onClick.AddListener(() => _extraAction?.Invoke());
        _back.onClick.AddListener(Back);
    }
    private void OnEnable()
    {
        _manager = GameSaveManager.Instance;
        if (_manager != null) _manager.Changed += RefreshStatus;
    }
    private void OnDisable() { if (_manager != null) _manager.Changed -= RefreshStatus; }
    private void RefreshStatus() { if (_status != null) _status.text = _manager.Status; }
    private static void Label(UnityEngine.UI.Button button, string text)
    {
        var label = button.GetComponentInChildren<TMPro.TMP_Text>(true);
        label.text = text;
        label.color = Color.white;
    }
    public void ShowSlots(bool saving, Action<int> selected, Action back)
    {
        _saving = saving; _onSelected = selected; _onBack = back;
        gameObject.SetActive(true);
        _title.text = saving ? "SAVE GAME" : "LOAD GAME";
        _confirm.gameObject.SetActive(false); _cancel.gameObject.SetActive(false); _extra.gameObject.SetActive(false); _back.gameObject.SetActive(true);
        Label(_back, "Back");
        _recoverSlot = -1;
        var list = GameSaveManager.Instance.Repository.List();
        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i].gameObject.SetActive(!saving || i != 0);
            Label(_slots[i], list[i].Label);
            _slots[i].interactable = saving || list[i].Valid;
            if (!saving && !list[i].Valid && GameSaveManager.Instance.Repository.HasBackup(i))
            {
                _slots[i].interactable = true;
                Label(_slots[i], list[i].Label + "\nRecover backup");
            }
        }
        RefreshStatus();
        SelectFirst();
    }
    private void Choose(int slot)
    {
        if (GameSaveManager.Instance.Busy) return;
        if (!_saving && !GameSaveManager.Instance.Repository.Read(slot).Valid)
        {
            _recoverSlot = slot;
            ShowConfirmation("Recover the validated backup for this slot?", "Recover", () =>
            {
                if (GameSaveManager.Instance.Repository.Recover(_recoverSlot, out string error)) GameSaveManager.Instance.Report("Backup recovered");
                else GameSaveManager.Instance.Report("Recovery failed: " + error);
                ShowSlots(false, _onSelected, _onBack);
            }, "Cancel", () => ShowSlots(false, _onSelected, _onBack));
            return;
        }
        if (_saving && GameSaveManager.Instance.Repository.Exists(slot))
            ShowConfirmation("Overwrite slot " + slot + "?", "Save", () => _onSelected?.Invoke(slot), "Cancel", () => ShowSlots(true, _onSelected, _onBack));
        else _onSelected?.Invoke(slot);
    }
    public void ShowConfirmation(string title, string confirmLabel, Action confirm, string cancelLabel, Action cancel, string extraLabel = null, Action extra = null)
    {
        gameObject.SetActive(true);
        _title.text = title;
        foreach (var slot in _slots) slot.gameObject.SetActive(false);
        _back.gameObject.SetActive(false);
        _confirmation = confirm; _cancelAction = cancel; _extraAction = extra;
        _confirm.gameObject.SetActive(true); Label(_confirm, confirmLabel);
        _cancel.gameObject.SetActive(true); Label(_cancel, cancelLabel);
        _extra.gameObject.SetActive(extra != null); if (extra != null) Label(_extra, extraLabel);
        RefreshStatus(); SelectFirst();
    }
    private void SelectFirst()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null) return;
        foreach (var button in GetComponentsInChildren<UnityEngine.UI.Button>(false)) if (button.interactable) { button.Select(); break; }
    }
    public void CancelOrBack()
    {
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.Busy) return;
        if (_cancel.gameObject.activeSelf) _cancelAction?.Invoke();
        else Back();
    }
    public void Back() { gameObject.SetActive(false); _onBack?.Invoke(); }
    public void Close() => gameObject.SetActive(false);
}
