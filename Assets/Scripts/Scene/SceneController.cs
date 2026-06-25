using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

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
    private bool IsNetworkActive => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (NetworkManager.Singleton?.SceneManager == null) return;

        var sm = NetworkManager.Singleton.SceneManager;
        sm.OnLoad += OnLoadHandler;
        sm.OnUnload += OnUnloadHandler;
        sm.OnLoadEventCompleted += HandleOnLoadEventComplete;
        sm.OnUnloadEventCompleted += HandleOnUnloadEventComplete;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton?.SceneManager == null) return;

        var sm = NetworkManager.Singleton.SceneManager;
        sm.OnLoad -= OnLoadHandler;
        sm.OnUnload -= OnUnloadHandler;
        sm.OnLoadEventCompleted -= HandleOnLoadEventComplete;
        sm.OnUnloadEventCompleted -= HandleOnUnloadEventComplete;
    }

    #region Network Event Handlers

    private void OnLoadHandler(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            StartCoroutine(FadeInOverlayRoutine());
        }
    }

    private void OnUnloadHandler(ulong clientId, string sceneName, AsyncOperation asyncOperation)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            StartCoroutine(FadeInOverlayRoutine());
        }
    }

    private void HandleOnLoadEventComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        _isNetworkSceneLoading = false;
        StartCoroutine(FadeOutOverlayRoutine());
    }

    private void HandleOnUnloadEventComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        _isNetworkSceneUnloading = false;
        StartCoroutine(FadeOutOverlayRoutine());
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

        if (plan.Overlay)
        {
            yield return FadeOutOverlayRoutine();
        }

        _isBusy = false;
    }

    private IEnumerator LoadAdditiveSceneRoutine(string slotKey, string sceneName, bool setActive)
    {
        if (IsNetworkActive)
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

        if (IsNetworkActive && _networkLoadedScenes.Contains(sceneName))
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
        if (!NetworkManager.Singleton.IsServer) yield break;

        _isNetworkSceneLoading = true;
        var status = NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
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
        if (!NetworkManager.Singleton.IsServer) yield break;

        Scene sceneToUnload = SceneManager.GetSceneByName(sceneName);
        if (!sceneToUnload.IsValid() || !sceneToUnload.isLoaded) yield break;

        _isNetworkSceneUnloading = true;
        var status = NetworkManager.Singleton.SceneManager.UnloadScene(sceneToUnload);
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
}





//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.SceneManagement;
//using Unity.Netcode;

//public class SceneController : NetworkBehaviour
//{
//    public static SceneController Instance;

//    [SerializeField] private LoadingOverlay _loadingOverlay;
//    private Dictionary<string, string> _loadedSceneBySlot = new();
//    private bool _isBusy = false;

//    // Flags for server-side coroutine throttling
//    private bool _isNetworkSceneLoading = false;
//    private bool _isNetworkSceneUnloading = false;
//    private HashSet<string> _networkLoadedScenes = new();

//    private bool IsNetworkActive => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

//    private void Awake()
//    {
//        if (Instance == null) Instance = this;
//        else Destroy(gameObject);
//    }

//    public override void OnNetworkSpawn()
//    {
//        if (NetworkManager.Singleton?.SceneManager == null) return;

//        // Using the modern, unified event handler channel
//        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnGlobalSceneEvent;
//    }

//    public override void OnNetworkDespawn()
//    {
//        if (NetworkManager.Singleton?.SceneManager == null) return;

//        NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnGlobalSceneEvent;
//    }

//    #region Unified Network Event Handler (Handles Clients and Late Joins)

//    private void OnGlobalSceneEvent(SceneEvent sceneEvent)
//    {
//        // SAFETY GUARD: If a client is late-joining, let Netcode auto-sync assets.

//        switch (sceneEvent.SceneEventType)
//        {
//            case SceneEventType.Load: // Triggered when a machine begins loading
//                if (sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId)
//                {
//                    StartCoroutine(FadeInOverlayRoutine());
//                }
//                break;

//            case SceneEventType.Unload: // Triggered when a machine begins unloading
//                if (sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId)
//                {
//                    StartCoroutine(FadeInOverlayRoutine());
//                }
//                break;

//            case SceneEventType.LoadComplete: // Triggered individually when ANY machine finishes loading
//                if (sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId)
//                {
//                    // Every machine updates its local tracking layout database
//                    string slotKey = Scenes.GetSlotForScene(sceneEvent.SceneName);
//                    if (!string.IsNullOrEmpty(slotKey))
//                    {
//                        _loadedSceneBySlot[slotKey] = sceneEvent.SceneName;
//                    }

//                    // Safely set the scene active on the machine that just finished loading it
//                    // (Works for Host, dedicated servers, and remote clients safely)
//                    if (IsServer && NetworkManager.Singleton.LocalClientId == sceneEvent.ClientId)
//                    {
//                        SetSceneActive(sceneEvent.SceneName);
//                    }
//                    else if (!IsServer) // Clients set active individually when they complete
//                    {
//                        SetSceneActive(sceneEvent.SceneName);
//                    }
//                }
//                break;

//            case SceneEventType.UnloadComplete: // Triggered individually when ANY machine finishes unloading
//                if (sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId)
//                {
//                    string slotKey = Scenes.GetSlotForScene(sceneEvent.SceneName);
//                    if (!string.IsNullOrEmpty(slotKey) && _loadedSceneBySlot.TryGetValue(slotKey, out var scene) && scene == sceneEvent.SceneName)
//                    {
//                        _loadedSceneBySlot.Remove(slotKey);
//                    }
//                }
//                break;

//            case SceneEventType.LoadEventCompleted: // Triggered strictly on Server/Host when everyone arrives
//                if (IsServer)
//                {
//                    _isNetworkSceneLoading = false;
//                }
//                StartCoroutine(FadeOutOverlayRoutine());
//                break;

//            case SceneEventType.UnloadEventCompleted: // Triggered strictly on Server/Host when everyone clears
//                if (IsServer)
//                {
//                    _isNetworkSceneUnloading = false;
//                }
//                StartCoroutine(FadeOutOverlayRoutine());
//                break;
//        }
//    }

//    #endregion

//    #region Overlay Helpers

//    private IEnumerator FadeInOverlayRoutine()
//    {
//        if (_loadingOverlay != null) yield return _loadingOverlay.FadeInBlack();
//    }

//    private IEnumerator FadeOutOverlayRoutine()
//    {
//        if (_loadingOverlay != null) yield return _loadingOverlay.FadeOutBlack();
//    }

//    #endregion

//    public SceneTransitionPlan NewTransition()
//    {
//        return new SceneTransitionPlan();
//    }

//    private Coroutine ExecutePlan(SceneTransitionPlan plan)
//    {

//        if (_isBusy)
//        {
//            Debug.LogWarning("Scene Change in progress");
//            return null;
//        }

//        _isBusy = true;
//        return StartCoroutine(ChangeSceneRoutine(plan));
//    }

//    private IEnumerator ChangeSceneRoutine(SceneTransitionPlan plan)
//    {
//        if (plan.Overlay)
//        {
//            yield return FadeInOverlayRoutine();
//            yield return new WaitForSeconds(0.5f);
//        }

//        // 1. Process explicit unloads
//        foreach (var slotKey in plan.SceneToUnLoad)
//        {
//            yield return UnloadSceneRoutine(slotKey);
//        }

//        if (plan.ClearUnusedAssets)
//        {
//            yield return ClearUnusedAssetsRoutine();
//        }

//        // 2. Process explicit loads
//        foreach (var kvp in plan.SceneToLoad)
//        {
//            if (_loadedSceneBySlot.ContainsKey(kvp.Key))
//            {
//                yield return UnloadSceneRoutine(kvp.Key);
//            }
//            yield return LoadAdditiveSceneRoutine(kvp.Key, kvp.Value);
//        }

//        if (plan.Overlay)
//        {
//            yield return FadeOutOverlayRoutine();
//        }

//        _isBusy = false;
//    }

//    private IEnumerator LoadAdditiveSceneRoutine(string slotKey, string sceneName)
//    {
//        if (IsNetworkActive)
//        {
//            // The server waits here until the whole network finishes loading
//            yield return LoadNetworkSceneRoutine(sceneName);
//        }
//        else
//        {
//            yield return LoadLocalSceneRoutine(sceneName);
//            // Local tracking is only updated here during offline mode; 
//            // Network mode uses the OnGlobalSceneEvent callback instead.
//            _loadedSceneBySlot[slotKey] = sceneName;
//            SetSceneActive(sceneName);
//        }
//    }

//    private IEnumerator UnloadSceneRoutine(string slotKey)
//    {
//        if (!_loadedSceneBySlot.TryGetValue(slotKey, out string sceneName)) yield break;
//        if (string.IsNullOrEmpty(sceneName)) yield break;

//        if (IsNetworkActive && _networkLoadedScenes.Contains(sceneName))
//        {
//            yield return UnloadNetworkSceneRoutine(sceneName);
//        }
//        else
//        {
//            yield return UnloadLocalSceneRoutine(sceneName);
//            _loadedSceneBySlot.Remove(slotKey);
//        }
//    }

//    #region Raw Execution Coroutines

//    private IEnumerator LoadNetworkSceneRoutine(string sceneName)
//    {
//        _isNetworkSceneLoading = true;
//        var status = NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
//        if (status != SceneEventProgressStatus.Started)
//        {
//            Debug.LogError($"[SceneController] Network LoadScene failed for {sceneName}: {status}");
//            _isNetworkSceneLoading = false;
//            yield break;
//        }

//        // Server execution stalls cleanly until LoadEventCompleted clears the flag
//        yield return new WaitUntil(() => !_isNetworkSceneLoading);
//        _networkLoadedScenes.Add(sceneName);
//    }

//    private IEnumerator UnloadNetworkSceneRoutine(string sceneName)
//    {
//        Scene sceneToUnload = SceneManager.GetSceneByName(sceneName);
//        if (!sceneToUnload.IsValid() || !sceneToUnload.isLoaded) yield break;

//        _isNetworkSceneUnloading = true;
//        var status = NetworkManager.Singleton.SceneManager.UnloadScene(sceneToUnload);
//        if (status != SceneEventProgressStatus.Started)
//        {
//            Debug.LogError($"[SceneController] Network UnloadScene failed for {sceneName}: {status}");
//            _isNetworkSceneUnloading = false;
//            yield break;
//        }

//        yield return new WaitUntil(() => !_isNetworkSceneUnloading);
//    }

//    private IEnumerator LoadLocalSceneRoutine(string sceneName)
//    {
//        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
//        if (loadOp == null) yield break;

//        loadOp.allowSceneActivation = false;
//        while (loadOp.progress < 0.9f) yield return null;

//        loadOp.allowSceneActivation = true;
//        while (!loadOp.isDone) yield return null;
//    }

//    private IEnumerator UnloadLocalSceneRoutine(string sceneName)
//    {
//        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(sceneName);
//        if (unloadOp == null) yield break;
//        while (!unloadOp.isDone) yield return null;
//    }

//    private void SetSceneActive(string sceneName)
//    {
//        Scene scene = SceneManager.GetSceneByName(sceneName);
//        if (scene.IsValid() && scene.isLoaded)
//        {
//            SceneManager.SetActiveScene(scene);
//            Debug.Log($"[Local System] Active Scene set to: {sceneName}");
//        }
//    }

//    private IEnumerator ClearUnusedAssetsRoutine()
//    {
//        AsyncOperation cleanupOp = Resources.UnloadUnusedAssets();
//        while (!cleanupOp.isDone) yield return null;
//    }

//    #endregion

//    public class SceneTransitionPlan
//    {
//        public Dictionary<string, string> SceneToLoad { get; } = new();
//        public List<string> SceneToUnLoad { get; } = new();
//        public string ActiveSceneName { get; private set; } = "";
//        public bool ClearUnusedAssets { get; private set; } = false;
//        public bool Overlay { get; private set; } = false;

//        public SceneTransitionPlan Load(string slotKey, string sceneName, bool setActive = false)
//        {
//            SceneToLoad[slotKey] = sceneName;
//            if (setActive) ActiveSceneName = sceneName;
//            return this;
//        }

//        public SceneTransitionPlan Unload(string slotKey)
//        {
//            SceneToUnLoad.Add(slotKey);
//            return this;
//        }

//        public SceneTransitionPlan WithOverlay()
//        {
//            Overlay = true;
//            return this;
//        }

//        public SceneTransitionPlan WithClearUnusedAssets()
//        {
//            ClearUnusedAssets = true;
//            return this;
//        }

//        public Coroutine Perform()
//        {
//            return SceneController.Instance.ExecutePlan(this);
//        }
//    }
//}