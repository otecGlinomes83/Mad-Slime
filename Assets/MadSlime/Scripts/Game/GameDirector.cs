using Core;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Game
{
    public sealed class GameDirector
    {
        private const float ActivationProgressThreshold = 0.9f;

        private readonly IAdsService _adsService;

        private SceneId _currentSceneId;
        private SceneId _previousSceneId;
        private bool _isInitialized;
        private bool _isTransitioning;

        public SceneId CurrentSceneId
        {
            get
            {
                EnsureInitialized();
                return _currentSceneId;
            }
        }

        public SceneId PreviousSceneId
        {
            get
            {
                EnsureInitialized();
                return _previousSceneId;
            }
        }

        public bool IsTransitioning => _isTransitioning;

        public GameDirector(IAdsService adsService)
        {
            _adsService = adsService;
        }

        public void EnsureInitialized()
        {
            if (_isInitialized == true)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();

            if (activeScene.isLoaded == false)
            {
                throw new InvalidOperationException(
                    "GameDirector: no active scene to initialize from. The boot scene must be loaded before GameDirector is used.");
            }

            _currentSceneId = ResolveSceneId(activeScene.name);
            _previousSceneId = _currentSceneId;
            _isInitialized = true;
        }

        public async UniTask LoadAsync(SceneId targetSceneId)
        {
            if (_isTransitioning == true)
            {
                throw new InvalidOperationException(
                    "GameDirector: a scene transition is already running. Do not start a new one before LoadAsync completes.");
            }

            EnsureInitialized();

            if (targetSceneId == _currentSceneId)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetSceneId),
                    targetSceneId,
                    "GameDirector: cannot load the scene that is already active.");
            }

            _isTransitioning = true;

            try
            {
                await TransitionAsync(targetSceneId);
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        private async UniTask TransitionAsync(SceneId targetSceneId)
        {
            SceneId sourceSceneId = _currentSceneId;
            Scene sourceScene = SceneManager.GetSceneByName(GetSceneName(sourceSceneId));

            if (sourceScene.isLoaded == true)
            {
                DisableOutgoingSystems(sourceScene);
            }

            if (_adsService.IsPauseGame == false)
            {
                Time.timeScale = 1f;
            }

            string targetSceneName = GetSceneName(targetSceneId);
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);

            if (loadOperation == null)
            {
                throw new InvalidOperationException(
                    $"GameDirector: failed to start loading scene '{targetSceneName}'. Check that it is present in the build settings.");
            }

            loadOperation.allowSceneActivation = false;

            while (loadOperation.progress < ActivationProgressThreshold)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            loadOperation.allowSceneActivation = true;
            await loadOperation.ToUniTask();

            Scene targetScene = SceneManager.GetSceneByName(targetSceneName);

            if (targetScene.isLoaded == false)
            {
                throw new InvalidOperationException(
                    $"GameDirector: scene '{targetSceneName}' did not finish loading.");
            }

            float desiredTimeScale = Time.timeScale;

            if (sourceScene.isLoaded == true)
            {
                await SceneManager.UnloadSceneAsync(sourceScene).ToUniTask();
            }

            SceneManager.SetActiveScene(targetScene);

            if (_adsService.IsPauseGame == false)
            {
                Time.timeScale = desiredTimeScale;
            }

            RestoreIncomingEventSystem(targetScene);

            _previousSceneId = sourceSceneId;
            _currentSceneId = targetSceneId;
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
                        "GameDirector: unknown scene id.");
            }
        }

        private static SceneId ResolveSceneId(string sceneName)
        {
            switch (sceneName)
            {
                case "Menu":
                    return SceneId.Menu;

                case "Game":
                    return SceneId.Game;

                case "Fill":
                    return SceneId.Fill;

                case "Shop":
                    return SceneId.Shop;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(sceneName),
                        sceneName,
                        "GameDirector: the active scene is not one of the known scenes (Menu, Game, Fill, Shop).");
            }
        }
    }
}
