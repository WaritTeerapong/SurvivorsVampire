using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : NetworkBehaviour
{
    public static SceneController Instance;

    [SerializeField] private LoadingOverlay _loadingOverlay;
    private Dictionary<string, string> _loadedSceneBySlot = new();
    private bool _isBusy = false;

    // Flag for coroutine to wait for network op
    private bool _isNetworkSceneLoading = false;
    private bool _isNetworkSceneUnloading = false;
    private HashSet<string> _networkLoadedScenes = new();
    private bool _isNetworkActive => NetworkManager != null && NetworkManager.IsListening;
    private NetworkVariable<bool> IsOverlayBuild = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> IsNetworkOverlayFadeIn = new NetworkVariable<bool>(false);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (NetworkManager?.SceneManager == null) return;

        var sm = NetworkManager.SceneManager;
        if (IsServer)
        {
            sm.ActiveSceneSynchronizationEnabled = true;
        }

        sm.OnLoad += OnLoadHandler;
        sm.OnUnload += OnUnloadHandler;
        sm.OnLoadEventCompleted += HandleOnLoadEventComplete;
        sm.OnUnloadEventCompleted += HandleOnUnloadEventComplete;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager?.SceneManager == null) return;

        var sm = NetworkManager.SceneManager;
        sm.OnLoad -= OnLoadHandler;
        sm.OnUnload -= OnUnloadHandler;
        sm.OnLoadEventCompleted -= HandleOnLoadEventComplete;
        sm.OnUnloadEventCompleted -= HandleOnUnloadEventComplete;
    }

    #region Network Event Handlers

    private void OnLoadHandler(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
    {
        _isNetworkSceneLoading = true;
        NetworkOverlayFadeIn();
    }

    private void OnUnloadHandler(ulong clientId, string sceneName, AsyncOperation asyncOperation)
    {
        _isNetworkSceneUnloading = true;
        NetworkOverlayFadeIn();
    }

    private void HandleOnLoadEventComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        _isNetworkSceneLoading = false;
        NetworkOverlayFadeOut();
    }

    private void HandleOnUnloadEventComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        _isNetworkSceneUnloading = false;
        NetworkOverlayFadeOut();
    }

    #endregion

    #region Overlay Helpers

    private IEnumerator FadeInOverlayRoutine()
    {
        if (_loadingOverlay != null) yield return _loadingOverlay.FadeInBlack();
    }

    private IEnumerator FadeOutOverlayRoutine()
    {
        if (_loadingOverlay != null) yield return _loadingOverlay.FadeOutBlack();
    }

    private void NetworkOverlayFadeIn()
    {
        if (IsOverlayBuild.Value && !IsNetworkOverlayFadeIn.Value)
        {
            StartCoroutine(FadeInOverlayRoutine());
            if (IsServer) IsNetworkOverlayFadeIn.Value = true;
        }
    }
    private void NetworkOverlayFadeOut()
    {
        if (IsOverlayBuild.Value && IsNetworkOverlayFadeIn.Value)
        {
            StartCoroutine(FadeOutOverlayRoutine());
            if (IsServer) IsNetworkOverlayFadeIn.Value = false;
        }
    }

    #endregion

    // Instantiates a new scene transition plan using the Builder Pattern
    public SceneTransitionPlan NewTransition()
    {
        return new SceneTransitionPlan();
    }

    // Internal entry point to execute the transition plan
    private Coroutine ExecutePlan(SceneTransitionPlan plan)
    {
        if (_isBusy)
        {
            Debug.LogWarning("Scene Change in progress");
            return null;
        }

        if (IsServer) IsOverlayBuild.Value = plan.Overlay;
        _isBusy = true;
        return StartCoroutine(ChangeSceneRoutine(plan));
    }

    // Coroutine that performs the sequential transition logic (overlay fade, unload, reload)
    private IEnumerator ChangeSceneRoutine(SceneTransitionPlan plan)
    {
        if (plan.Overlay)
        {
            yield return FadeInOverlayRoutine();
            yield return new WaitForSeconds(0.5f);
        }

        foreach (var slotKey in plan.SceneToUnLoad)
        {
            yield return UnloadSceneRoutine(slotKey);
        }

        if (plan.ClearUnusedAssets)
        {
            yield return ClearUnusedAssetsRoutine();
        }

        foreach (var kvp in plan.SceneToLoad)
        {
            if (_loadedSceneBySlot.ContainsKey(kvp.Key))
            {
                yield return UnloadSceneRoutine(kvp.Key);
            }
            yield return LoadAdditiveSceneRoutine(kvp.Key, kvp.Value, plan.ActiveSceneName == kvp.Value);
        }

        if (!string.IsNullOrEmpty(plan.ActiveSceneName))
        {
            SetSceneActive(plan.ActiveSceneName);
        }

        if (plan.Overlay)
        {
            yield return FadeOutOverlayRoutine();
        }

        if (IsServer) IsOverlayBuild.Value = false;
        _isBusy = false;
    }

    private IEnumerator LoadAdditiveSceneRoutine(string slotKey, string sceneName, bool setActive)
    {
        if (_isNetworkActive)
        {
            yield return LoadNetworkSceneRoutine(sceneName);
            _networkLoadedScenes.Add(sceneName);
        }
        else
        {
            yield return LoadLocalSceneRoutine(sceneName);
        }

        if (setActive)
        {
            SetSceneActive(sceneName);
        }

        _loadedSceneBySlot[slotKey] = sceneName;
    }

    private IEnumerator UnloadSceneRoutine(string slotKey)
    {
        if (!_loadedSceneBySlot.TryGetValue(slotKey, out string sceneName)) yield break;
        if (string.IsNullOrEmpty(sceneName)) yield break;

        if (_isNetworkActive && _networkLoadedScenes.Contains(sceneName))
        {
            yield return UnloadNetworkSceneRoutine(sceneName);
            _networkLoadedScenes.Remove(sceneName);
        }
        else
        {
            yield return UnloadLocalSceneRoutine(sceneName);
            _networkLoadedScenes.Remove(sceneName);
        }

        _loadedSceneBySlot.Remove(slotKey);
    }

    #region Scene Load/Unload Implementations

    private IEnumerator LoadNetworkSceneRoutine(string sceneName)
    {
        if (!NetworkManager.IsServer) yield break;

        _isNetworkSceneLoading = true;
        var status = NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogError($"[SceneController] Network LoadScene failed for {sceneName}: {status}");
            _isNetworkSceneLoading = false;
            yield break;
        }

        while (_isNetworkSceneLoading) yield return null;
    }

    private IEnumerator LoadLocalSceneRoutine(string sceneName)
    {
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (loadOp == null) yield break;

        loadOp.allowSceneActivation = false;
        while (loadOp.progress < 0.9f) yield return null;

        loadOp.allowSceneActivation = true;
        while (!loadOp.isDone) yield return null;
    }

    private IEnumerator UnloadNetworkSceneRoutine(string sceneName)
    {
        if (!NetworkManager.IsServer) yield break;

        Scene sceneToUnload = SceneManager.GetSceneByName(sceneName);
        if (!sceneToUnload.IsValid() || !sceneToUnload.isLoaded) yield break;

        _isNetworkSceneUnloading = true;
        var status = NetworkManager.SceneManager.UnloadScene(sceneToUnload);
        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogError($"[SceneController] Network UnloadScene failed for {sceneName}: {status}");
            _isNetworkSceneUnloading = false;
            yield break;
        }

        while (_isNetworkSceneUnloading) yield return null;
    }

    private IEnumerator UnloadLocalSceneRoutine(string sceneName)
    {
        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(sceneName);
        if (unloadOp == null) yield break;
        while (!unloadOp.isDone) yield return null;
    }

    private void SetSceneActive(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid() && scene.isLoaded)
        {
            SceneManager.SetActiveScene(scene);
        }
    }

    private IEnumerator ClearUnusedAssetsRoutine()
    {
        AsyncOperation cleanupOp = Resources.UnloadUnusedAssets();
        while (!cleanupOp.isDone) yield return null;
    }

    #endregion

    // Builder Pattern : for transition plan
    public class SceneTransitionPlan
    {
        public Dictionary<string, string> SceneToLoad { get; } = new();
        public List<string> SceneToUnLoad { get; } = new();
        public string ActiveSceneName { get; private set; } = "";
        public bool ClearUnusedAssets { get; private set; } = false;
        public bool Overlay { get; private set; } = false;

        public SceneTransitionPlan Load(string slotKey, string sceneName, bool setActive = false)
        {
            SceneToLoad[slotKey] = sceneName;
            if (setActive) ActiveSceneName = sceneName;
            return this;
        }

        public SceneTransitionPlan SetSceneActive(string slotKey)
        {
            if (SceneToLoad.TryGetValue(slotKey, out string sceneToLoad))
            {
                ActiveSceneName = sceneToLoad;
            }
            else if (SceneController.Instance._loadedSceneBySlot.TryGetValue(slotKey, out string loadedScene))
            {
                ActiveSceneName = loadedScene;
            }
            return this;
        }

        public SceneTransitionPlan Unload(string slotKey)
        {
            SceneToUnLoad.Add(slotKey);
            return this;
        }

        public SceneTransitionPlan WithOverlay()
        {
            Overlay = true;
            return this;
        }

        public SceneTransitionPlan WithClearUnusedAssets()
        {
            ClearUnusedAssets = true;
            return this;
        }

        // Terminal operation of the builder pattern; triggers the execution of the plan
        public Coroutine Perform()
        {
            return SceneController.Instance.ExecutePlan(this);
        }
    }

    public string GetActiveSceneName() => SceneManager.GetActiveScene().name;
}
