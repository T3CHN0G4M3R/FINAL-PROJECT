using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class GameplayFadeTravel : MonoBehaviour
{
    private DeathRetryTransition _overlay;
    private string _destination;
    private bool _pickup;
    private bool _previousAcquired;
    private bool _previousUsed;
    private bool _finished;
    private GameplaySaveAdapter _readyAdapter;
    private const float ReadinessTimeout = 30f;

    public static bool Begin(string destination, bool pickup, UIAlphaFadeEffect effect,
        UnityEngine.UI.Image image, float fadeOut, float fadeIn, float blackHold, out string error)
    {
        error = null;
        var manager = GameSaveManager.Instance;
        var transitioner = PixeLadder.EasyTransition.SceneTransitioner.Instance;
        if (manager == null || manager.Busy || manager.EncounterInProgress || NewGameSceneSequence.IntroActive ||
            (transitioner != null && transitioner.IsTransitioning) || effect == null || effect.transitionMaterial == null ||
            image == null || !Application.CanStreamedLevelBeLoaded(destination) || !SaveGameData.SupportedScene(destination))
        { error = "Travel is unavailable or fade references are missing."; return false; }
        var root = new GameObject("Baguette Gameplay Travel");
        var travel = root.AddComponent<GameplayFadeTravel>();
        travel._destination = destination;
        travel._pickup = pickup;
        travel._previousAcquired = manager.HasBaguette;
        travel._previousUsed = manager.BaguetteUsedOnCar;
        try
        {
            DontDestroyOnLoad(root);
            travel._overlay = new GameObject("Baguette Travel Fade").AddComponent<DeathRetryTransition>();
            travel._overlay.Initialize(effect, image, fadeOut, fadeIn, blackHold);
            if (!manager.BeginBaguetteTravel(destination, pickup, out error))
            { travel._finished = true; travel._overlay.Dispose(); Destroy(root); return false; }
            GameplaySaveAdapter.SceneReady += travel.OnReady;
            travel.StartCoroutine(travel.Travel());
            return true;
        }
        catch (System.Exception e)
        {
            error = e.Message;
            travel._finished = true;
            if (travel._overlay != null) travel._overlay.Dispose();
            if (manager.IntroTransitionInProgress)
            {
                manager.RestoreBaguetteTravelState(travel._previousAcquired, travel._previousUsed);
                manager.EndIntroTransition();
            }
            Destroy(root);
            return false;
        }
    }

    private void OnReady(GameplaySaveAdapter adapter)
    {
        if (adapter.ScenePath == _destination) _readyAdapter = adapter;
    }

    private IEnumerator Travel()
    {
        yield return _overlay.FadeOut();
        string failure = _overlay == null || !_overlay.IsUsable ? "Fade interrupted." : null;
        AsyncOperation operation = null;
        if (failure == null)
        {
            try
            {
                if (!_pickup) GameSaveManager.Instance.CommitBaguetteCarUse();
                operation = SceneManager.LoadSceneAsync(_destination);
            }
            catch (System.Exception e) { failure = e.Message; }
            if (operation != null) yield return operation;
            else failure = failure ?? "Scene load could not start.";
        }
        float deadline = Time.realtimeSinceStartup + ReadinessTimeout;
        while (failure == null && (_readyAdapter == null || !GameSaveManager.Instance.IsCinematicGameplayReady(_readyAdapter)) &&
            Time.realtimeSinceStartup < deadline) yield return null;
        if (failure == null && (_readyAdapter == null || !GameSaveManager.Instance.IsCinematicGameplayReady(_readyAdapter)))
            failure = "Destination restoration did not complete.";
        if (failure != null)
        {
            GameSaveManager.Instance.RestoreBaguetteTravelState(_previousAcquired, _previousUsed);
            GameSaveManager.Instance.Report("Travel failed: " + failure);
            Debug.LogError("Baguette travel failed: " + failure, this);
        }
        if (_overlay != null && _overlay.IsUsable) yield return _overlay.FadeIn();
        Finish();
    }

    private void Finish()
    {
        _finished = true;
        GameplaySaveAdapter.SceneReady -= OnReady;
        if (GameSaveManager.Instance != null) GameSaveManager.Instance.EndIntroTransition();
        if (_overlay != null) _overlay.Dispose();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        GameplaySaveAdapter.SceneReady -= OnReady;
        if (_finished) return;
        if (_overlay != null) _overlay.Dispose();
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.RestoreBaguetteTravelState(_previousAcquired, _previousUsed);
            GameSaveManager.Instance.EndIntroTransition();
            GameSaveManager.Instance.Report("Baguette travel was interrupted.");
        }
    }
}
