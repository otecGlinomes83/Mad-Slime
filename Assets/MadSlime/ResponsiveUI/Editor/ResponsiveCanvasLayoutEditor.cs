using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UI.Editor
{
    [CustomEditor(typeof(ResponsiveCanvasLayout))]
    public class ResponsiveCanvasLayoutEditor : UnityEditor.Editor
    {
        private const string ElementsProperty = "_elements";
        private const string WithoutControlsProperty = "_withoutControls";
        private const string TargetProperty = "Target";
        private const string PortraitProfile = "Portrait";
        private const string LandscapeProfile = "Landscape";
        private const int SkinRouletteMode = 1;

        private static string[] s_rouletteOptions = { "С кнопками (ежедневная)", "Без кнопок (скины)" };
        private static string[] s_surfaceOptions =
        {
            "Из Game View (автоматически)",
            "Portrait · 1080 × 1920",
            "Landscape · 1920 × 1080"
        };

        private ResponsiveCanvasLayout _layout;
        private RoulettePopupLayout _roulette;
        private string _rouletteModeKey;
        private string _messageKey;

        private void OnEnable()
        {
            _layout = (ResponsiveCanvasLayout)target;
            _layout.TryGetComponent(out _roulette);
            string stateKey = "ResponsiveCanvasLayout.Capture." + target.GetInstanceID();
            _rouletteModeKey = stateKey + ".RouletteMode";
            _messageKey = stateKey + ".Message";
        }

        public override void OnInspectorGUI()
        {
            SceneLayoutPreview.ApplyCurrentOrientation();
            serializedObject.Update();

            if (DrawRouletteVariant())
            {
                return;
            }

            SerializedObject storage = GetCaptureStorage();
            storage.Update();
            SerializedProperty elements = storage.FindProperty(GetCaptureProperty());
            int count = CountTargets(elements);
            bool isInPrefabMode = PrefabStageUtility.GetPrefabStage(_layout.gameObject) != null;

            if (isInPrefabMode)
            {
                DrawSurfaceSource();
            }

            DrawInstructions(isInPrefabMode, count);
            DrawCaptureButtons(count);
            string message = SessionState.GetString(_messageKey, "");

            if (string.IsNullOrEmpty(message) == false)
            {
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }

            DrawDefaultInspector();
        }

        private bool DrawRouletteVariant()
        {
            if (_roulette == null)
            {
                return false;
            }

            int mode = SessionState.GetInt(_rouletteModeKey, 0);
            int selected = EditorGUILayout.Popup("Вариант рулетки", mode, s_rouletteOptions);

            if (selected == mode)
            {
                return false;
            }

            SessionState.SetInt(_rouletteModeKey, selected);

            if (PrefabStageUtility.GetPrefabStage(_layout.gameObject) != null)
            {
                PrefabLayoutPreview.PreviewSource source = PrefabLayoutPreview.GetSource(_layout);
                PrefabLayoutPreview.Configure(_layout, source, selected == SkinRouletteMode, true);
            }

            Repaint();
            return true;
        }

        private void DrawSurfaceSource()
        {
            PrefabLayoutPreview.PreviewSource source = PrefabLayoutPreview.GetSource(_layout);
            int selected = EditorGUILayout.Popup("Размер области", (int)source, s_surfaceOptions);
            PrefabLayoutPreview.Configure(_layout, (PrefabLayoutPreview.PreviewSource)selected, IsWithoutControls());
            string profile = PortraitProfile;

            if (PrefabLayoutPreview.IsLandscape)
            {
                profile = LandscapeProfile;
            }

            EditorGUILayout.LabelField("Профиль", profile);
        }

        private void DrawInstructions(bool isInPrefabMode, int count)
        {
            string instructions = "Работает вне Play Mode. Выберите формат Game View, расставьте объекты через Rect Transform и " +
                "запишите их в Portrait или Landscape. Затем сохраните сцену или префаб. " +
                "В игре профиль выбирается автоматически по размеру экрана.";

            if (isInPrefabMode)
            {
                instructions = "Prefab Mode: по умолчанию область в Scene следует за разрешением Game View. " +
                    "При смене ориентации применяется сохранённый профиль. Перед сменой ориентации запишите ручные правки, " +
                    "затем сохраните префаб (Ctrl+S).";
            }

            EditorGUILayout.HelpBox(instructions, MessageType.Info);
            EditorGUILayout.LabelField("Объект", target.name);
            EditorGUILayout.LabelField("Объектов в профиле", count.ToString());

            if (count == 0)
            {
                EditorGUILayout.HelpBox("В этом окне не назначены объекты для записи (Elements). " +
                    "Запись недоступна: профиль окна не настроен.", MessageType.Warning);
            }

            if (_layout.gameObject.activeInHierarchy == false)
            {
                EditorGUILayout.HelpBox("Окно или его родитель выключены. Профиль изменит координаты, " +
                    "но окно не станет видимым. Включите нужный объект в Hierarchy.", MessageType.Warning);
            }

            if (isInPrefabMode == false)
            {
                Vector2 size = Handles.GetMainGameViewSize();
                EditorGUILayout.LabelField("Game View", Mathf.RoundToInt(size.x) + " × " + Mathf.RoundToInt(size.y));
            }
        }

        private void DrawCaptureButtons(int count)
        {
            bool isDisabled = EditorApplication.isPlayingOrWillChangePlaymode || count == 0;

            using (new EditorGUI.DisabledScope(isDisabled))
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Записать в Portrait"))
                {
                    Capture(PortraitProfile);
                }

                if (GUILayout.Button("Записать в Landscape"))
                {
                    Capture(LandscapeProfile);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private bool IsWithoutControls()
        {
            return _roulette != null && SessionState.GetInt(_rouletteModeKey, 0) == SkinRouletteMode;
        }

        private string GetCaptureProperty()
        {
            if (IsWithoutControls())
            {
                return WithoutControlsProperty;
            }

            return ElementsProperty;
        }

        private SerializedObject GetCaptureStorage()
        {
            if (IsWithoutControls())
            {
                return new SerializedObject(_roulette);
            }

            return serializedObject;
        }

        private static int CountTargets(SerializedProperty elements)
        {
            int count = 0;

            for (int i = 0; i < elements.arraySize; i++)
            {
                SerializedProperty element = elements.GetArrayElementAtIndex(i);

                if (element.FindPropertyRelative(TargetProperty).objectReferenceValue != null)
                {
                    count++;
                }
            }

            return count;
        }

        private void Capture(string profile)
        {
            serializedObject.ApplyModifiedProperties();
            SerializedObject storage = GetCaptureStorage();
            Undo.RecordObject(storage.targetObject, "Capture UI layout");
            storage.Update();
            SerializedProperty elements = storage.FindProperty(GetCaptureProperty());
            int count = 0;

            for (int i = 0; i < elements.arraySize; i++)
            {
                SerializedProperty element = elements.GetArrayElementAtIndex(i);
                RectTransform rect = element.FindPropertyRelative(TargetProperty).objectReferenceValue as RectTransform;

                if (rect == null)
                {
                    continue;
                }

                SerializedProperty placement = element.FindPropertyRelative(profile);
                CapturePlacement(placement, rect);
                count++;
            }

            storage.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(storage.targetObject);
            EditorUtility.SetDirty(storage.targetObject);

            if (_layout.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(_layout.gameObject.scene);
            }

            SessionState.SetString(_messageKey, "Записано в " + profile + ": " + count +
                " объектов. Сохраните сцену или префаб (Ctrl+S). Запись не переключает расстановку.");
            RefreshViews();
        }

        private static void CapturePlacement(SerializedProperty placement, RectTransform rect)
        {
            placement.FindPropertyRelative("AnchorMin").vector2Value = rect.anchorMin;
            placement.FindPropertyRelative("AnchorMax").vector2Value = rect.anchorMax;
            placement.FindPropertyRelative("Pivot").vector2Value = rect.pivot;
            placement.FindPropertyRelative("Position").vector2Value = rect.anchoredPosition;
            placement.FindPropertyRelative("Size").vector2Value = rect.sizeDelta;
            placement.FindPropertyRelative("Scale").floatValue = rect.localScale.x;
        }

        private void RefreshViews()
        {
            Canvas.ForceUpdateCanvases();
            SceneView.RepaintAll();
            EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();

            foreach (EditorWindow window in windows)
            {
                window.Repaint();
            }

            EditorApplication.QueuePlayerLoopUpdate();
            Repaint();
        }
    }
}
