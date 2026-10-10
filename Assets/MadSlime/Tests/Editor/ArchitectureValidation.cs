using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MadSlime.Tests
{
    public class ArchitectureValidation
    {
        public static void ValidateAssets()
        {
            string[] sceneNames = { "Menu", "Game", "Fill", "Shop" };
            int checkedObjects = 0;

            for (int i = 0; i < sceneNames.Length; i++)
            {
                Scene scene = EditorSceneManager.OpenScene("Assets/MadSlime/Scenes/" + sceneNames[i] + ".unity");
                GameObject[] roots = scene.GetRootGameObjects();

                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    Transform[] transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);

                    for (int objectIndex = 0; objectIndex < transforms.Length; objectIndex++)
                    {
                        GameObject target = transforms[objectIndex].gameObject;
                        int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(target);

                        if (missingCount > 0)
                        {
                            throw new InvalidOperationException(scene.name + "/" + target.name + ": missing scripts " + missingCount);
                        }

                        checkedObjects++;
                    }
                }
            }

            Debug.Log("ARCHITECTURE ASSETS PASS: " + checkedObjects + " objects");
        }
    }
}
