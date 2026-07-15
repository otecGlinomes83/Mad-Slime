using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

namespace Game
{
    public sealed class LevelTransitor : MonoBehaviour
    {
        [SerializeField] private string _previousScene;
        [SerializeField] private string _nextScene;
        [SerializeField] private string _shopScene;

        public bool IsHasPrevious => string.IsNullOrEmpty(_previousScene) == false;
        public bool IsHasNext => string.IsNullOrEmpty(_nextScene) == false;
        public bool IsHasShop => string.IsNullOrEmpty(_shopScene) == false;

        public void Restart()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            Load(currentSceneName);
        }

        public void LoadPrevious()
        {
            if (IsHasPrevious == false)
            {
                return;
            }

            Load(_previousScene);
        }

        public void LoadNext()
        {
            if (IsHasNext == false)
            {
                return;
            }

            Load(_nextScene);
        }

        public void LoadShop()
        {
            if (IsHasShop == false)
            {
                return;
            }

            Load(_shopScene);
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
            YG2.saves.PreviousScene = SceneManager.GetActiveScene().name;
            YG2.saves.NextScene = targetScene;
            YG2.SaveProgress();
            SceneManager.LoadScene(targetScene);
        }
    }
}