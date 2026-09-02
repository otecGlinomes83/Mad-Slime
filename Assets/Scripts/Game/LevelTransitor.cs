using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

namespace Game
{
    public sealed class LevelTransitor : MonoBehaviour
    {
        [SerializeField] private string _gameScene;
        [SerializeField] private string _fillScene;
        [SerializeField] private string _shopScene;

        public bool IsHasShop => string.IsNullOrEmpty(_shopScene) == false;

        public void Restart()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            Load(currentSceneName, false);
        }

        public void LoadGame()
        {
            Load(_gameScene, false);
        }

        public void LoadFill()
        {
            Load(_fillScene, false);
        }

        public void LoadShop()
        {
            if (IsHasShop == false)
            {
                return;
            }

            Load(_shopScene, true);
        }

        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            Load(sceneName, false);
        }

        private void Load(string targetScene, bool savePreviousScene)
        {
            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogError(
                    $"[Scene] {SceneManager.GetActiveScene().name} tried to load EMPTY scene name. Fill the scene name fields on the LevelTransitor component.");
                return;
            }

            Debug.Log($"[Scene] {SceneManager.GetActiveScene().name} -> {targetScene}");

            if (savePreviousScene == true)
            {
                YG2.saves.PreviousScene = SceneManager.GetActiveScene().name;

                if (YG2.isSDKEnabled == true)
                {
                    YG2.SaveProgress();
                }
            }

            SceneManager.LoadScene(targetScene);
        }
    }
}
