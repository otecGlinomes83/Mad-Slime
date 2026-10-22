using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Editor
{
    [InitializeOnLoad]
    internal static class SceneLayoutPreview
    {
        private static List<ResponsiveCanvasLayout> s_layouts = new List<ResponsiveCanvasLayout>();
        private static List<ResponsiveCanvasLayout> s_removedLayouts = new List<ResponsiveCanvasLayout>();
        private static Dictionary<ResponsiveCanvasLayout, bool> s_orientations = new Dictionary<ResponsiveCanvasLayout, bool>();
        private static bool s_isHierarchyDirty = true;

        static SceneLayoutPreview()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private static void OnEditorUpdate()
        {
            ApplyCurrentOrientation();
        }

        public static void ApplyCurrentOrientation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Vector2 size = Handles.GetMainGameViewSize();

            if (size.x <= 0f || size.y <= 0f)
            {
                return;
            }

            if (s_isHierarchyDirty)
            {
                CollectLayouts();
                s_isHierarchyDirty = false;
            }

            bool isLandscape = size.x > size.y;
            bool isChanged = false;

            foreach (ResponsiveCanvasLayout layout in s_layouts)
            {
                if (layout == null)
                {
                    continue;
                }

                if (s_orientations.TryGetValue(layout, out bool wasLandscape) && wasLandscape == isLandscape)
                {
                    continue;
                }

                ApplyProfile(layout, isLandscape);
                s_orientations[layout] = isLandscape;
                isChanged = true;
            }

            if (isChanged)
            {
                Canvas.ForceUpdateCanvases();
                SceneView.RepaintAll();
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        private static void CollectLayouts()
        {
            s_layouts.Clear();
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (scene.isLoaded == false)
                {
                    continue;
                }

                if (prefabStage != null && scene == prefabStage.scene)
                {
                    continue;
                }

                GameObject[] roots = scene.GetRootGameObjects();

                foreach (GameObject root in roots)
                {
                    s_layouts.AddRange(root.GetComponentsInChildren<ResponsiveCanvasLayout>(true));
                }
            }

            RemoveClosedLayouts();
        }

        private static void RemoveClosedLayouts()
        {
            s_removedLayouts.Clear();

            foreach (KeyValuePair<ResponsiveCanvasLayout, bool> entry in s_orientations)
            {
                if (entry.Key == null || s_layouts.Contains(entry.Key) == false)
                {
                    s_removedLayouts.Add(entry.Key);
                }
            }

            foreach (ResponsiveCanvasLayout layout in s_removedLayouts)
            {
                s_orientations.Remove(layout);
            }

            s_removedLayouts.Clear();
        }

        private static void ApplyProfile(ResponsiveCanvasLayout layout, bool isLandscape)
        {
            if (layout.TryGetComponent(out RoulettePopupLayout roulette))
            {
                roulette.ApplyOrientation(isLandscape, false);
                return;
            }

            layout.ApplyOrientation(isLandscape);
        }

        private static void OnHierarchyChanged()
        {
            s_isHierarchyDirty = true;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            s_isHierarchyDirty = true;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            s_orientations.Clear();
            s_isHierarchyDirty = true;
        }

        private static void OnBeforeAssemblyReload()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            s_layouts.Clear();
            s_removedLayouts.Clear();
            s_orientations.Clear();
        }
    }
}
