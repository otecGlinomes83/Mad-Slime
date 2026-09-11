using Scriptables;
using UnityEditor;
using UnityEngine;

namespace Game
{
    [CustomEditor(typeof(LayoutPreviewDrawer))]
    public sealed class LayoutPreviewDrawerEditor : Editor
    {
        private int _handleLayoutIndex;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            LayoutPreviewDrawer drawer = (LayoutPreviewDrawer)target;

            if (drawer.CustomLayout != null)
            {
                EditorGUILayout.HelpBox(
                    $"Scene handles edit Custom Layout ('{drawer.CustomLayout.name}'). Clear the Custom Layout field to edit catalog layouts by index.",
                    MessageType.Info);
                return;
            }

            if (drawer.Catalog == null || drawer.Resolver == null)
            {
                return;
            }

            LevelConfig config = drawer.Resolver.GetConfigFor(drawer.PreviewLevel);

            if (config == null || config.Layouts.Count == 0)
            {
                return;
            }

            _handleLayoutIndex = Mathf.Clamp(
                EditorGUILayout.IntField("Handle Layout Index", _handleLayoutIndex),
                0,
                config.Layouts.Count - 1);

            LayoutSet selected = config.Layouts[_handleLayoutIndex];

            if (selected != null)
            {
                EditorGUILayout.HelpBox(
                    $"Scene handles edit layout #{_handleLayoutIndex} ('{selected.name}'). Assign a Custom Layout to edit it directly.",
                    MessageType.Info);
            }
        }

        private void OnSceneGUI()
        {
            LayoutPreviewDrawer drawer = (LayoutPreviewDrawer)target;

            if (drawer.LevelGenerator == null)
            {
                return;
            }

            LayoutSet layout = drawer.CustomLayout;

            if (layout == null)
            {
                if (drawer.Catalog == null || drawer.Catalog.Ranges.Count == 0)
                {
                    return;
                }

                LevelConfig config = drawer.Resolver.GetConfigFor(drawer.PreviewLevel);

                if (config == null || config.Layouts.Count == 0)
                {
                    return;
                }

                int layoutIndex = Mathf.Clamp(_handleLayoutIndex, 0, config.Layouts.Count - 1);
                layout = config.Layouts[layoutIndex];

                if (layout == null)
                {
                    return;
                }
            }

            SerializedObject layoutSetObject = new SerializedObject(layout);
            SerializedProperty zonesProperty = layoutSetObject.FindProperty("_zones");

            if (zonesProperty == null)
            {
                return;
            }

            for (int i = 0; i < zonesProperty.arraySize; i++)
            {
                SerializedProperty centerProperty =
                    zonesProperty.GetArrayElementAtIndex(i).FindPropertyRelative("_center");

                if (centerProperty == null)
                {
                    continue;
                }

                DrawZoneCenterHandle(drawer, layoutSetObject, centerProperty);
            }
        }

        private void DrawZoneCenterHandle(LayoutPreviewDrawer drawer, SerializedObject layoutSetObject,
            SerializedProperty centerProperty)
        {
            Vector2 displayCenter = ApplyMirror(drawer, centerProperty.vector2Value);
            Vector3 worldCenter =
                drawer.transform.TransformPoint(new Vector3(displayCenter.x, 0f, displayCenter.y));

            float handleSize = HandleUtility.GetHandleSize(worldCenter) * 0.2f;

            EditorGUI.BeginChangeCheck();

            Vector3 newWorldCenter = Handles.FreeMoveHandle(
                worldCenter,
                handleSize,
                Vector3.one * 0.5f,
                Handles.SphereHandleCap);

            if (EditorGUI.EndChangeCheck() == false)
            {
                return;
            }

            Vector3 newLocalPoint = drawer.transform.InverseTransformPoint(newWorldCenter);

            Vector2 newCenter = ApplyMirror(drawer, new Vector2(newLocalPoint.x, newLocalPoint.z));

            centerProperty.vector2Value = newCenter;
            layoutSetObject.ApplyModifiedProperties();
        }

        private static Vector2 ApplyMirror(LayoutPreviewDrawer drawer, Vector2 center)
        {
            Vector2 mirrored = center;

            if (drawer.MirrorX == true)
            {
                mirrored.x = -mirrored.x;
            }

            if (drawer.MirrorZ == true)
            {
                mirrored.y = -mirrored.y;
            }

            return mirrored;
        }
    }
}
