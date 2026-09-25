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
        [SerializeField] private string _menuScene;

        public bool IsHasShop => string.IsNullOrEmpty(_shopScene) == false;
        public bool IsHasMenu => string.IsNullOrEmpty(_menuScene) == false;

        public void LoadGame()
        {
            Load(_gameScene);
        }

        public void LoadFill()
        {
            Load(_fillScene);
        }

        public void LoadShop()
        {
            if (IsHasShop == false)
            {
                return;
            }

            YG2.saves.PreviousScene = SceneManager.GetActiveScene().name;

            if (YG2.isSDKEnabled == true)
            {
                YG2.SaveProgress();
            }

            Load(_shopScene);
        }

        public void LoadMenu()
        {
            if (IsHasMenu == false)
            {
                return;
            }

            Load(_menuScene);
        }

        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            Load(sceneName);
        }

        private void Load(string targetScene)
        {
            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogError(
                    $"[Scene] {SceneManager.GetActiveScene().name} tried to load EMPTY scene name. Fill the scene name fields on the LevelTransitor component.");
                return;
            }

            SceneManager.LoadScene(targetScene);
        }
    }
}