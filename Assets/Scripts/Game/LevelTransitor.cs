using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    public sealed class LevelTransitor : MonoBehaviour
    {
        [SerializeField] private string _previousScene;
        [SerializeField] private string _nextScene;

        public bool IsHasPrevious => string.IsNullOrEmpty(_previousScene) == false;
        public bool IsHasNext => string.IsNullOrEmpty(_nextScene) == false;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void LoadPrevious()
        {
            if (string.IsNullOrEmpty(_previousScene))
            {
                return;
            }

            SceneManager.LoadScene(_previousScene);
        }

        public void LoadNext()
        {
            if (string.IsNullOrEmpty(_nextScene))
            {
                return;
            }

            SceneManager.LoadScene(_nextScene);
        }
    }
}