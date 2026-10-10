using System;
using System.Threading;
using Saves;
using VContainer;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Game
{
    public class SceneNavigator : MonoBehaviour
    {
        private const float ActivationProgressThreshold = 0.9f;

        [Tooltip("Экран загрузки: полноэкранный Canvas, показывается на время перехода между сценами.")]
        [SerializeField] private Canvas _loadingScreenPrefab;

        private SceneId _currentSceneId;
        private SceneId _previousSceneId;
        private bool _isInitialized;
        private bool _isTransitioning;
        private bool _isDailyShownThisSession;
        private Func<CancellationToken, UniTask> _pendingPreparation;
        private Action _pendingSceneStart;
        private Action _pendingSceneFinish;
        private CancellationToken _pendingSessionToken;
        private Action _currentSessionStart;
        private Action _currentSessionFinish;
        private Canvas _loadingScreenInstance;

        public SceneId CurrentSceneId => _currentSceneId;

        public SceneId PreviousSceneId => _previousSceneId;

        private ISavesReadiness _readiness;

        [Inject]
        public void Construct(ISavesReadiness readiness)
        {
            _readiness = readiness;
        }

        public bool IsTransitioning => _isTransitioning;

        public bool TryMarkDailyShown()
        {
            if (_isDailyShownThisSession)
            {
                return false;
            }

            _isDailyShownThisSession = true;
            return true;
        }

        public void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            if (TryResolveLoadedSceneId(out SceneId bootSceneId) == false)
            {
                throw new InvalidOperationException(
                    "SceneNavigator: no loaded scene resolves to a known SceneId (Menu, Game, Fill, Shop). " +
                    "The boot scene must be loaded before SceneNavigator is used.");
            }

            _currentSceneId = bootSceneId;
            _previousSceneId = _currentSceneId;
            _isInitialized = true;
        }

        public void RegisterSession(Func<CancellationToken, UniTask> preparation, Action sessionStart,
            Action sessionFinish, CancellationToken sessionToken)
        {
            if (preparation == null || sessionStart == null || sessionFinish == null)
            {
                throw new ArgumentNullException(nameof(preparation), "Scene session lifecycle callbacks are required.");
            }

            if (_isTransitioning)
            {
                _pendingPreparation = preparation;
                _pendingSceneStart = sessionStart;
                _pendingSceneFinish = sessionFinish;
                _pendingSessionToken = sessionToken;
                return;
            }

            StartInitialSessionAsync(preparation, sessionStart, sessionFinish, sessionToken).Forget();
        }

        public void UnregisterSession(Action sessionStart)
        {
            if (_currentSessionStart == sessionStart)
            {
                _currentSessionStart = null;
                _currentSessionFinish = null;
            }

            if (_pendingSceneStart == sessionStart)
            {
                ClearPendingSession();
            }
        }

        private async UniTaskVoid StartInitialSessionAsync(Func<CancellationToken, UniTask> preparation,
            Action sessionStart, Action sessionFinish, CancellationToken sessionToken)
        {
            try
            {
                ShowLoadingScreen();
                await UniTask.WaitUntil(IsSaveReady, cancellationToken: sessionToken);
                EnsureInitialized();
                await preparation.Invoke(sessionToken);
                sessionToken.ThrowIfCancellationRequested();
                _currentSessionStart = sessionStart;
                _currentSessionFinish = sessionFinish;
                sessionStart.Invoke();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                HideLoadingScreen();
            }
        }

        private bool IsSaveReady()
        {
            return _readiness.IsReady;
        }

        private bool IsIncomingSessionRegistered()
        {
            return _pendingPreparation != null;
        }

        private void ClearPendingSession()
        {
            _pendingPreparation = null;
            _pendingSceneStart = null;
            _pendingSceneFinish = null;
        }

        public UniTask LoadMenuAsync()
        {
            return LoadAsync(SceneId.Menu);
        }

        public UniTask LoadGameAsync()
        {
            return LoadAsync(SceneId.Game);
        }

        public UniTask LoadFillAsync()
        {
            return LoadAsync(SceneId.Fill);
        }

        public UniTask LoadShopAsync()
        {
            return LoadAsync(SceneId.Shop);
        }

        private async UniTask LoadAsync(SceneId targetSceneId)
        {
            if (_isTransitioning)
            {
                return;
            }

            EnsureInitialized();

            if (targetSceneId == _currentSceneId)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetSceneId),
                    targetSceneId,
                    "SceneNavigator: cannot load the scene that is already active.");
            }

            _isTransitioning = true;

            try
            {
                await TransitionAsync(targetSceneId);
            }
            finally
            {
                ClearPendingSession();
                HideLoadingScreen();
                _isTransitioning = false;
            }
        }

        private async UniTask TransitionAsync(SceneId targetSceneId)
        {
            ShowLoadingScreen();
            _currentSessionFinish?.Invoke();
            CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();

            SceneId sourceSceneId = _currentSceneId;
            Scene sourceScene = SceneManager.GetSceneByName(GetSceneName(sourceSceneId));

            if (sourceScene.isLoaded == true)
            {
                DisableOutgoingSystems(sourceScene);
            }

            string targetSceneName = GetSceneName(targetSceneId);
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);

            if (loadOperation == null)
            {
                throw new InvalidOperationException(
                    $"SceneNavigator: failed to start loading scene '{targetSceneName}'. Check that it is present in the build settings.");
            }

            loadOperation.allowSceneActivation = false;

            while (loadOperation.progress < ActivationProgressThreshold)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            loadOperation.allowSceneActivation = true;
            await loadOperation.ToUniTask(cancellationToken: cancellationToken);

            Scene targetScene = SceneManager.GetSceneByName(targetSceneName);

            if (targetScene.isLoaded == false)
            {
                throw new InvalidOperationException(
                    $"SceneNavigator: scene '{targetSceneName}' did not finish loading.");
            }

            SceneManager.SetActiveScene(targetScene);
            await UniTask.WaitUntil(IsIncomingSessionRegistered, cancellationToken: cancellationToken);
            await _pendingPreparation.Invoke(_pendingSessionToken);
            _pendingSessionToken.ThrowIfCancellationRequested();

            if (sourceScene.isLoaded)
            {
                await SceneManager.UnloadSceneAsync(sourceScene).ToUniTask();
            }

            RestoreIncomingEventSystem(targetScene);

            Action sessionStart = _pendingSceneStart;

            _currentSessionStart = _pendingSceneStart;
            _currentSessionFinish = _pendingSceneFinish;
            ClearPendingSession();
            _previousSceneId = sourceSceneId;
            _currentSceneId = targetSceneId;

            sessionStart?.Invoke();

            HideLoadingScreen();
        }

        private void ShowLoadingScreen()
        {
            if (_loadingScreenPrefab == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Loading screen prefab is not assigned. Drag a loading screen Canvas prefab into the _loadingScreenPrefab field.");
            }

            if (_loadingScreenInstance != null)
            {
                Destroy(_loadingScreenInstance.gameObject);
            }

            _loadingScreenInstance = Instantiate(_loadingScreenPrefab, transform);
        }

        private void HideLoadingScreen()
        {
            if (_loadingScreenInstance == null)
            {
                return;
            }

            Destroy(_loadingScreenInstance.gameObject);
            _loadingScreenInstance = null;
        }

        private static void DisableOutgoingSystems(Scene sourceScene)
        {
            GameObject[] roots = sourceScene.GetRootGameObjects();

            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                DisableAll<AudioListener>(roots[rootIndex]);
                DisableAll<EventSystem>(roots[rootIndex]);
                DisableAll<Canvas>(roots[rootIndex]);
            }
        }

        private static void DisableAll<TComponent>(GameObject root)
        {
            TComponent[] components = root.GetComponentsInChildren<TComponent>(true);

            for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                Behaviour behaviour = components[componentIndex] as Behaviour;

                if (behaviour != null)
                {
                    behaviour.enabled = false;
                }
            }
        }

        private static void RestoreIncomingEventSystem(Scene targetScene)
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject[] roots = targetScene.GetRootGameObjects();

            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                EventSystem[] eventSystems = roots[rootIndex].GetComponentsInChildren<EventSystem>(true);

                if (eventSystems.Length > 0)
                {
                    eventSystems[0].enabled = true;
                    return;
                }
            }
        }

        private static string GetSceneName(SceneId sceneId)
        {
            switch (sceneId)
            {
                case SceneId.Menu:
                    return "Menu";

                case SceneId.Game:
                    return "Game";

                case SceneId.Fill:
                    return "Fill";

                case SceneId.Shop:
                    return "Shop";

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(sceneId),
                        sceneId,
                        "SceneNavigator: unknown scene id.");
            }
        }

        private static bool TryResolveLoadedSceneId(out SceneId sceneId)
        {
            Scene activeScene = SceneManager.GetActiveScene();

            if (activeScene.IsValid() == true && TryResolveSceneId(activeScene.name, out sceneId) == true)
            {
                return true;
            }

            int sceneCount = SceneManager.sceneCount;

            for (int index = 0; index < sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);

                if (scene.IsValid() == false)
                {
                    continue;
                }

                if (TryResolveSceneId(scene.name, out sceneId) == true)
                {
                    return true;
                }
            }

            sceneId = default;
            return false;
        }

        private static bool TryResolveSceneId(string sceneName, out SceneId sceneId)
        {
            switch (sceneName)
            {
                case "Menu":
                    sceneId = SceneId.Menu;
                    return true;

                case "Game":
                    sceneId = SceneId.Game;
                    return true;

                case "Fill":
                    sceneId = SceneId.Fill;
                    return true;

                case "Shop":
                    sceneId = SceneId.Shop;
                    return true;

                default:
                    sceneId = default;
                    return false;
            }
        }
    }
}
