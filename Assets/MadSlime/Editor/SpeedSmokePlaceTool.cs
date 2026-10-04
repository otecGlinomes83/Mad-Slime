using Movement;
using Player;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EditorTools
{
    public static class SpeedSmokePlaceTool
    {
        private const string ScenePath = "Assets/MadSlime/Scenes/Game.unity";
        private const string SmokePrefabPath =
            "Assets/Epic Toon FX/Prefabs/Environment/Smoke/White/SmokeWhiteTrail.prefab";
        private const string FallbackObjectName = "SmokeTrail";

        public static void Run()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Scene scene = EditorSceneManager.GetActiveScene();
            GameObject[] rootObjects = scene.GetRootGameObjects();

            GameObject playerObject = null;

            for (int i = 0; i < rootObjects.Length && playerObject == null; i++)
            {
                playerObject = rootObjects[i].GetComponentInChildren<Mover>(true).gameObject;
            }

            if (playerObject == null)
            {
                throw new InvalidOperationException($"SpeedSmokePlaceTool: no Player in '{scene.name}'.");
            }

            ParticleSystem[] childSystems = playerObject.GetComponentsInChildren<ParticleSystem>(true);
            GameObject smokeObject;

            if (childSystems.Length > 0)
            {
                smokeObject = childSystems[0].gameObject;
            }
            else
            {
                GameObject smokePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmokePrefabPath);

                if (smokePrefab == null)
                {
                    throw new InvalidOperationException(
                        $"SpeedSmokePlaceTool: no smoke under Player and no prefab at '{SmokePrefabPath}'.");
                }

                smokeObject = (GameObject)PrefabUtility.InstantiatePrefab(smokePrefab, playerObject.transform);
                smokeObject.name = FallbackObjectName;
                smokeObject.transform.localPosition = Vector3.zero;
                smokeObject.transform.localRotation = Quaternion.identity;
                smokeObject.transform.localScale = Vector3.one;
            }

            ParticleSystem[] systems = smokeObject.GetComponentsInChildren<ParticleSystem>(true);

            if (systems.Length == 0)
            {
                throw new InvalidOperationException(
                    $"SpeedSmokePlaceTool: '{smokeObject.name}' has no ParticleSystem inside.");
            }

            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem.MainModule main = systems[i].main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
            }

            SpeedSmoke speedSmoke = playerObject.GetComponent<SpeedSmoke>();

            if (speedSmoke == null)
            {
                throw new InvalidOperationException($"SpeedSmokePlaceTool: no SpeedSmoke on '{playerObject.name}'.");
            }

            SerializedObject smokeSerialized = new SerializedObject(speedSmoke);
            smokeSerialized.FindProperty("_smoke").objectReferenceValue = systems[0];
            smokeSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[SpeedSmokePlaceTool] Wired '{smokeObject.name}' ({systems.Length} systems, world space) " +
                "into SpeedSmoke._smoke.");
        }
    }
}
