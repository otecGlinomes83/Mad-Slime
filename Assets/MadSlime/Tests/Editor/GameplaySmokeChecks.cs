using System;
using Collectables;
using Cysharp.Threading.Tasks;
using DI;
using Game;
using Items;
using Player;
using PlayerInput;
using Quota;
using Saves;
using Shop;
using ShapeFill;
using UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace MadSlime.Tests
{
    public class GameplaySmokeChecks
    {
        private const string RunKey = "MadSlime.GameplaySmokeChecks";

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/MadSlime/Scenes/Menu.unity");
            SessionState.SetBool(RunKey, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Subscribe()
        {
            if (SessionState.GetBool(RunKey, false) == false)
            {
                return;
            }

            EditorApplication.playModeStateChanged += OnPlayStateChanged;
        }

        private static void OnPlayStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            Application.logMessageReceived += OnLog;
            CheckAsync().Forget();
        }

        private static void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                SessionState.SetBool(RunKey, false);
                EditorApplication.delayCall += ExitFailed;
            }
        }

        private static void ExitFailed()
        {
            EditorApplication.Exit(1);
        }

        private static async UniTaskVoid CheckAsync()
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(2), DelayType.Realtime);
                MenuLifetimeScope menuScope = GetComponent<MenuLifetimeScope>("Menu");
                IObjectResolver menuContainer = menuScope.Container;
                Ensure(menuContainer.Resolve<ISavesReadiness>().IsReady, "SDK data not ready");
                SceneNavigator navigator = menuContainer.Resolve<SceneNavigator>();
                Ensure(navigator.CurrentSceneId == SceneId.Menu, "Menu not initialized");
                Button settings = GetButton(menuContainer.Resolve<MainMenu>(), "_settingsButton");
                settings.onClick.Invoke();
                await UniTask.Delay(TimeSpan.FromSeconds(0.4), DelayType.Realtime);
                PauseMenu settingsWindow = GetComponent<PauseMenu>("Menu");
                Ensure(settingsWindow != null && settingsWindow.transform.localScale.sqrMagnitude > 0f, "Settings not shown");
                GetButton(settingsWindow, "_closeButton").onClick.Invoke();
                await UniTask.Delay(TimeSpan.FromSeconds(0.4), DelayType.Realtime);
                Ensure(GetComponent<PauseMenu>("Menu") == null, "Settings not released");

                await navigator.LoadShopAsync();
                await UniTask.Delay(TimeSpan.FromSeconds(0.5), DelayType.Realtime);
                ShopLifetimeScope shopScope = GetComponent<ShopLifetimeScope>("Shop");
                ShopPanel shop = shopScope.Container.Resolve<ShopPanel>();
                shop.ShowAllSkinsTab();
                await UniTask.NextFrame();
                Ensure(GetComponent<ShopItemView>("Shop") != null, "Skin page empty");
                shop.ShowUpgradesTab();
                await UniTask.NextFrame();
                Ensure(GetComponent<UpgradeItemView>("Shop") != null, "Upgrade page empty");
                shop.ShowRouletteTab();
                await UniTask.NextFrame();

                await navigator.LoadGameAsync();
                await UniTask.Delay(TimeSpan.FromSeconds(0.5), DelayType.Realtime);
                GameLifetimeScope gameScope = GetComponent<GameLifetimeScope>("Game");
                IObjectResolver gameContainer = gameScope.Container;
                GameplaySessionHandler session = gameContainer.Resolve<GameplaySessionHandler>();
                Movement.Movement movement = gameContainer.Resolve<Movement.Movement>();
                LevelGenerator generator = gameContainer.Resolve<LevelGenerator>();
                Ensure(generator.SpawnedItems.Count > 0, "Level did not generate");
                Ensure(GetComponent<QuotaPlateUI>("Game") != null, "Quota not shown after reset");
                Vector3 initialPosition = movement.transform.position;
                Keyboard keyboard = Keyboard.current;

                if (keyboard == null)
                {
                    keyboard = InputSystem.AddDevice<Keyboard>();
                }

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                await UniTask.Delay(TimeSpan.FromSeconds(0.3), DelayType.Realtime);
                Ensure(movement.transform.position != initialPosition, "Movement did not start from input");
                session.PauseByRequest();
                Vector3 pausedPosition = movement.transform.position;
                await UniTask.Delay(TimeSpan.FromSeconds(0.2), DelayType.Realtime);
                Ensure(movement.transform.position == pausedPosition, "Movement continues while paused");
                session.ResumeByRequest();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                await UniTask.NextFrame();
                Item collectible = null;

                foreach (Item item in generator.SpawnedItems)
                {
                    if (gameContainer.Resolve<CollectAvailability>().CanCollect(item.Definition.Tier) == true)
                    {
                        collectible = item;
                        break;
                    }
                }

                Ensure(collectible != null, "No collectible item at initial tier");
                int countBefore = gameContainer.Resolve<ICollectedItemsStorage>().CollectedItemsCount;
                collectible.transform.position = movement.transform.position;
                Physics.SyncTransforms();
                await UniTask.Delay(TimeSpan.FromSeconds(0.2), DelayType.Realtime);
                Ensure(gameContainer.Resolve<ICollectedItemsStorage>().CollectedItemsCount > countBefore, "Collection did not update lifetime count");
                int countAfter = gameContainer.Resolve<ICollectedItemsStorage>().CollectedItemsCount;
                await UniTask.Delay(TimeSpan.FromSeconds(0.2), DelayType.Realtime);
                Ensure(gameContainer.Resolve<ICollectedItemsStorage>().CollectedItemsCount == countAfter, "Collection counted twice");

                await navigator.LoadFillAsync();
                await UniTask.Delay(TimeSpan.FromSeconds(0.5), DelayType.Realtime);
                FillLifetimeScope fillScope = GetComponent<FillLifetimeScope>("Fill");
                GridBuilder grid = fillScope.Container.Resolve<GridBuilder>();
                Ensure(grid.FillCells.Count > 0, "Fill calculated on empty grid");
                Ensure(fillScope.Container.Resolve<FillResultCalculator>().Calculate(grid.FillCells.Count).TargetCubes > 0,
                    "Fill lost collected result");
                Ensure(navigator.PreviousSceneId == SceneId.Game && navigator.CurrentSceneId == SceneId.Fill,
                    "Scene identity stale after transition");
                await navigator.LoadMenuAsync();
                await UniTask.Delay(TimeSpan.FromSeconds(0.3), DelayType.Realtime);
                Ensure(GetComponent<GameLifetimeScope>("Game") == null, "Old scene not unloaded");
                SessionState.SetBool(RunKey, false);
                Debug.Log("GAMEPLAY SMOKE PASS: Menu/settings/Shop/pages/Game/movement/pause/collection/Fill/Menu");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(RunKey, false);
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static Button GetButton(Component component, string propertyName)
        {
            SerializedObject serialized = new SerializedObject(component);
            return (Button)serialized.FindProperty(propertyName).objectReferenceValue;
        }

        private static T GetComponent<T>(string sceneName) where T : Component
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);

            if (scene.isLoaded == false)
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                T component = roots[i].GetComponentInChildren<T>(true);

                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static void Ensure(bool condition, string message)
        {
            if (condition == false)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
