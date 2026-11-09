using System.Linq;
using UnityEngine;

public sealed class SaveMenuController : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button _continue;
    [SerializeField] private UnityEngine.UI.Button _load;
    [SerializeField] private UnityEngine.UI.Button _newGame;
    [SerializeField] private SaveSlotPanel _panel;
    [SerializeField] private GameObject _playChoices;
    [SerializeField] private NewGameSceneSequence _sequence;
    [SerializeField] private TMPro.TMP_Text _status;
    [SerializeField] private CanvasGroup _mainCommands;
    private GameSaveManager _manager;
    private void Start()
    {
        _manager = GameSaveManager.Instance;
        if (_manager != null) _manager.Changed += Refresh;
        Refresh();
    }
    private void OnDestroy() { if (_manager != null) _manager.Changed -= Refresh; }
    private void Refresh()
    {
        if (_manager == null) return;
        bool valid = _manager.Repository.List().Any(s => s.Valid);
        bool backup = Enumerable.Range(0,4).Any(s => _manager.Repository.HasBackup(s));
        _continue.interactable = valid && !_manager.Busy;
        _load.interactable = (valid || backup) && !_manager.Busy;
        _newGame.interactable = !_manager.Busy && (_sequence == null || !_sequence.IsRunning);
        if (_mainCommands != null) _mainCommands.interactable = !_manager.Busy && !_panel.IsOpen && (_sequence == null || !_sequence.IsRunning);
        if (_status != null) _status.text = _manager.Status;
    }
    public void RefreshPlayOptions() => Refresh();
    public void ContinueGame()
    {
        if (_manager == null || _manager.Busy) return;
        var latest = _manager.Repository.Newest();
        if (latest != null) LoadSlot(latest.Slot);
        else Refresh();
    }
    public void OpenLoadGame()
    {
        if (_manager == null || _manager.Busy) return;
        _playChoices.SetActive(false);
        _panel.ShowSlots(false, LoadSlot, ClosePanel);
        Refresh();
    }
    private void LoadSlot(int slot)
    {
        if (_manager.Busy) return;
        _manager.Load(slot, (success,error) =>
        {
            if (this == null) return;
            if (!success) { ClosePanel(); Refresh(); }
        });
        Refresh();
    }
    public void NewGame()
    {
        if (_manager == null || _manager.Busy || _sequence == null || _sequence.IsRunning) return;
        if (_manager.Repository.Exists(0))
        {
            _playChoices.SetActive(false);
            _panel.ShowConfirmation("Start a new game?\nManual saves will be kept.\nThe autosave will be replaced.", "New Game", StartNewGame, "Cancel", ClosePanel);
        }
        else StartNewGame();
        Refresh();
    }
    private void StartNewGame()
    {
        _sequence.BeginNewGame();
        if (_sequence.IsRunning) { _panel.Close(); _playChoices.SetActive(false); }
        Refresh();
    }
    private void ClosePanel()
    {
        _panel.Close();
        _playChoices.SetActive(true);
        Refresh();
        if (_continue.interactable) _continue.Select(); else _newGame.Select();
    }
}
