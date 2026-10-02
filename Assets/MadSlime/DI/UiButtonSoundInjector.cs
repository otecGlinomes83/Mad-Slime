using Audio;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace DI
{
    public static class UiButtonSoundInjector
    {
        public static void InjectInScene(IObjectResolver container, Scene scene)
        {
            GameObject[] sceneRoots = scene.GetRootGameObjects();

            for (int rootIndex = 0; rootIndex < sceneRoots.Length; rootIndex++)
            {
                UIButtonSound[] buttonSounds = sceneRoots[rootIndex].GetComponentsInChildren<UIButtonSound>(true);

                for (int soundIndex = 0; soundIndex < buttonSounds.Length; soundIndex++)
                {
                    container.Inject(buttonSounds[soundIndex]);
                }
            }
        }
    }
}
