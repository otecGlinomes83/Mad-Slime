using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UI.Editor
{
    [InitializeOnLoad]
    internal static class PrefabLayoutPreview
    {
        public enum PreviewSource
        {
            GameView,
            Portrait,
            Landscape
        }

        private const float PortraitWidth = 1080f;
        private const float PortraitHeight = 1920f;
        private const float ResizeThresholdSquared = 0.5f;
        private const string SpinButtonProperty = "_spinButton";
        private const string AdButtonProperty = "_adButton";

        private static PrefabStage s_stage;
        private static PrefabStage s_pendingStage;
        private static ResponsiveCanvasLayout s_layout;
        private static PreviewSource s_source;
        private static bool s_isLandscape;
        private static bool s_isWithoutControls;
        private static bool s_isSaving;
        private static Vector2 s_size;
        private static List<PrefabCanvasSurface> s_surfaces = new List<PrefabCanvasSurface>();
        private static Dictionary<GameObject, bool> s_rouletteControls = new Dictionary<GameObject, bool>();

        public static bool IsLandscape => s_isLandscape;

        static PrefabLayoutPreview()
        {
            PrefabStage.prefabStageOpened += OnStageOpened;
            PrefabStage.prefabSaving += OnBeforeSave;
            PrefabStage.prefabSaved += OnAfterSave;
            PrefabStage.prefabStageClosing += OnStageClosing;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.update += OnEditorUpdate;
            Undo.undoRedoPerformed += OnUndoRedo;

            PrefabStage currentStage = PrefabStageUtility.GetCurrentPrefabStage();

            if (currentStage != null)
            {
                OnStageOpened(currentStage);
            }
        }

        public static PreviewSource GetSource(ResponsiveCanvasLayout layout)
        {
            if (s_stage != null && s_stage == PrefabStageUtility.GetPrefabStage(layout.gameObject))
            {
                return s_source;
            }

            return PreviewSource.GameView;
        }

        public static void Configure(ResponsiveCanvasLayout layout, PreviewSource source, bool withoutControls, bool forceProfile = false)
        {
            PrefabStage stage = PrefabStageUtility.GetPrefabStage(layout.gameObject);

            if (stage == null || s_isSaving)
            {
                return;
            }

            Vector2 size = GetSurfaceSize(source);
            bool isLandscape = size.x > size.y;
            bool isOpened = s_stage != stage;
            bool shouldApplyProfile = isOpened || forceProfile || s_isLandscape != isLandscape || s_source != source ||
                (s_layout == layout && s_isWithoutControls != withoutControls);
            bool isResized = isOpened || (s_size - size).sqrMagnitude > ResizeThresholdSquared;

            if (isOpened)
            {
                OpenStage(stage);
            }

            s_layout = layout;
            s_source = source;
            s_size = size;
            s_isLandscape = isLandscape;
            s_isWithoutControls = withoutControls;

            if (shouldApplyProfile)
            {
                ApplyProfiles();
            }

            if (isResized == false && shouldApplyProfile == false)
            {
                return;
            }

            RefreshSurfaces();
            FrameSurface();
        }

        private static Vector2 GetSurfaceSize(PreviewSource source)
        {
            Vector2 size;

            switch (source)
            {
                case PreviewSource.Landscape:
                    size = new Vector2(PortraitHeight, PortraitWidth);
                    break;
                case PreviewSource.Portrait:
                    size = new Vector2(PortraitWidth, PortraitHeight);
                    break;
                default:
                    size = Handles.GetMainGameViewSize();
                    break;
            }

            if (size.x <= 0f || size.y <= 0f)
            {
                size = new Vector2(PortraitWidth, PortraitHeight);
            }

            Vector2 reference = new Vector2(PortraitWidth, PortraitHeight);

            if (size.x > size.y)
            {
                reference = new Vector2(PortraitHeight, PortraitWidth);
            }

            float scale = Mathf.Min(size.x / reference.x, size.y / reference.y);
            return size / scale;
        }

        private static void OpenStage(PrefabStage stage)
        {
            Clear();
            s_stage = stage;
            HashSet<Canvas> seenCanvases = new HashSet<Canvas>();
            Canvas[] canvases = stage.prefabContentsRoot.GetComponentsInChildren<Canvas>(true);

            foreach (Canvas canvas in canvases)
            {
                Canvas[] parents = canvas.GetComponentsInParent<Canvas>(true);

                for (int i = parents.Length - 1; i >= 0; i--)
                {
                    if (seenCanvases.Add(parents[i]))
                    {
                        s_surfaces.Add(new PrefabCanvasSurface(parents[i]));
                    }
                }
            }
        }

        private static void ApplyProfiles()
        {
            Undo.RegisterFullObjectHierarchyUndo(s_stage.prefabContentsRoot, "Edit orientation profile");
            ResponsiveCanvasLayout[] profiles = s_stage.prefabContentsRoot.GetComponentsInChildren<ResponsiveCanvasLayout>(true);

            foreach (ResponsiveCanvasLayout profile in profiles)
            {
                if (profile.TryGetComponent(out RoulettePopupLayout roulette))
                {
                    RememberControls(roulette);
                    roulette.ApplyOrientation(s_isLandscape, s_isWithoutControls);
                }
                else
                {
                    profile.ApplyOrientation(s_isLandscape);
                }
            }

            ApplyControlsPreview();
        }

        private static void OnStageOpened(PrefabStage stage)
        {
            s_pendingStage = stage;
            EditorApplication.delayCall -= OnStageReady;
            EditorApplication.delayCall += OnStageReady;
        }

        private static void OnStageReady()
        {
            EditorApplication.delayCall -= OnStageReady;
            PrefabStage stage = s_pendingStage;
            s_pendingStage = null;

            if (stage == null || stage != PrefabStageUtility.GetCurrentPrefabStage() || s_stage == stage)
            {
                return;
            }

            ResponsiveCanvasLayout[] profiles = stage.prefabContentsRoot.GetComponentsInChildren<ResponsiveCanvasLayout>(true);

            if (profiles.Length > 0)
            {
                Configure(profiles[0], PreviewSource.GameView, false);
            }
        }

        private static void OnEditorUpdate()
        {
            if (s_stage == null || s_layout == null || s_isSaving || s_source != PreviewSource.GameView ||
                EditorApplication.isPlayingOrWillChangePlaymode || s_stage != PrefabStageUtility.GetCurrentPrefabStage())
            {
                return;
            }

            Configure(s_layout, PreviewSource.GameView, s_isWithoutControls);
        }

        private static void OnUndoRedo()
        {
            if (s_stage != null)
            {
                RefreshSurfaces();
            }
        }

        private static void RefreshSurfaces()
        {
            foreach (PrefabCanvasSurface surface in s_surfaces)
            {
                surface.Show(s_size);
            }

            Canvas.ForceUpdateCanvases();
            SceneView.RepaintAll();
        }

        private static void FrameSurface()
        {
            SceneView view = SceneView.lastActiveSceneView;

            if (view == null || s_surfaces.Count == 0)
            {
                return;
            }

            view.in2DMode = true;
            Vector3 size = new Vector3(s_size.x, s_size.y, 1f);
            view.Frame(new Bounds(s_surfaces[0].Canvas.transform.position, size), false);
        }

        private static void RememberControls(RoulettePopupLayout roulette)
        {
            SerializedObject serialized = new SerializedObject(roulette);
            RememberControl(serialized.FindProperty(SpinButtonProperty));
            RememberControl(serialized.FindProperty(AdButtonProperty));
        }

        private static void RememberControl(SerializedProperty property)
        {
            RectTransform rect = property.objectReferenceValue as RectTransform;

            if (rect == null || s_rouletteControls.ContainsKey(rect.gameObject))
            {
                return;
            }

            s_rouletteControls.Add(rect.gameObject, rect.gameObject.activeSelf);
        }

        private static void RestoreControls()
        {
            foreach (KeyValuePair<GameObject, bool> entry in s_rouletteControls)
            {
                if (entry.Key != null)
                {
                    entry.Key.SetActive(entry.Value);
                }
            }
        }

        private static void ApplyControlsPreview()
        {
            foreach (KeyValuePair<GameObject, bool> entry in s_rouletteControls)
            {
                if (entry.Key != null)
                {
                    entry.Key.SetActive(s_isWithoutControls == false && entry.Value);
                }
            }
        }

        private static void OnBeforeSave(GameObject root)
        {
            if (s_stage == null || s_stage.prefabContentsRoot != root)
            {
                return;
            }

            s_isSaving = true;
            RestoreControls();
            RestoreSurfaces();
        }

        private static void OnAfterSave(GameObject root)
        {
            if (s_stage == null || s_stage.prefabContentsRoot != root)
            {
                return;
            }

            s_isSaving = false;
            ApplyControlsPreview();
            RefreshSurfaces();
        }

        private static void OnStageClosing(PrefabStage stage)
        {
            if (s_stage == stage)
            {
                Clear();
            }
        }

        private static void RestoreSurfaces()
        {
            foreach (PrefabCanvasSurface surface in s_surfaces)
            {
                surface.Restore();
            }
        }

        private static void OnBeforeAssemblyReload()
        {
            PrefabStage.prefabStageOpened -= OnStageOpened;
            PrefabStage.prefabSaving -= OnBeforeSave;
            PrefabStage.prefabSaved -= OnAfterSave;
            PrefabStage.prefabStageClosing -= OnStageClosing;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.delayCall -= OnStageReady;
            Undo.undoRedoPerformed -= OnUndoRedo;
            s_pendingStage = null;
            Clear();
        }

        private static void Clear()
        {
            RestoreControls();
            s_rouletteControls.Clear();
            RestoreSurfaces();
            s_surfaces.Clear();
            s_stage = null;
            s_layout = null;
            s_isSaving = false;
        }
    }
}
