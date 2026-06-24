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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public SceneTransitionPlan NewTransition()
    {
        return new SceneTransitionPlan();
    }

    private Coroutine ExecutePlan(SceneTransitionPlan plan)
    {
        if (_isBusy)
        {
            Debug.LogWarning("Scene Change in progress");
            return null;
        }

        _isBusy=true;
        return StartCoroutine(ChangeSceneRoutine(plan));
    }
    private IEnumerator ChangeSceneRoutine(SceneTransitionPlan plan)
    {
        if (plan.Overlay)
        {
            yield return _loadingOverlay.FadeInBlack();
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
            yield return LoadAdditiveSceneRoutine(kvp.Key,kvp.Value,plan.ActiveSceneName == kvp.Value);
        }

        if (plan.Overlay)
        {
            yield return _loadingOverlay.FadeOutBlack();
        }

        _isBusy = false;
    }

    private IEnumerator LoadAdditiveSceneRoutine(string slotKey, string sceneName, bool setActive)
    {
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName,LoadSceneMode.Additive);
        if (loadOp == null) yield break;
        loadOp.allowSceneActivation = false;
        while (loadOp.progress < 0.9f)
        {
            yield return null;
        }

        loadOp.allowSceneActivation = true;
        while (!loadOp.isDone)
        {
            yield return null;
        }

        if (setActive)
        {
            Scene newScene = SceneManager.GetSceneByName(sceneName);
            if(newScene.IsValid() && newScene.isLoaded)
            {
                SceneManager.SetActiveScene(newScene);
            }
        }
        _loadedSceneBySlot[slotKey] = sceneName;
    }

    private IEnumerator UnloadSceneRoutine(string slotKey)
    {
        if(!_loadedSceneBySlot.TryGetValue(slotKey, out string sceneName)) yield break;
        if (string.IsNullOrEmpty(sceneName)) yield break;
        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(sceneName);
        if (unloadOp == null) yield break;
        while (!unloadOp.isDone)
        {
            yield return null;
        }
        _loadedSceneBySlot.Remove(slotKey);
    }

    private IEnumerator ClearUnusedAssetsRoutine()
    { 
        AsyncOperation cleanupOp = Resources.UnloadUnusedAssets();
        while (!cleanupOp.isDone)
        {
            yield return null;
        }

    }

    // Builder Pattern : for transition plan
    public class SceneTransitionPlan
    {
        public Dictionary<string, string> SceneToLoad { get; } = new();
        public List<string> SceneToUnLoad { get; } = new();
        public string ActiveSceneName { get; private set; } = "";
        public bool ClearUnusedAssets { get; private set; } = false;
        public bool Overlay { get; private set; } = false;

        public SceneTransitionPlan Load(string slotKey, string sceneName, bool setActive = false) {
            SceneToLoad[slotKey] = sceneName; 
            if(setActive)ActiveSceneName = sceneName;
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
        public Coroutine Perform()
        {
            return SceneController.Instance.ExecutePlan(this);
        }
    }
}
