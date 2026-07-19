using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Levels
{
    public sealed class LevelCounter : MonoBehaviour
    {
        [SerializeField] private string _scenePrefix = "Level";

        private readonly List<int> _availableLevels = new List<int>();
        private bool _isComputed;

        public IReadOnlyList<int> AvailableLevels
        {
            get
            {
                if (_isComputed == false)
                {
                    Compute();
                }
                return _availableLevels;
            }
        }

        public int TotalLevels => AvailableLevels.Count;

        public bool IsLevelAvailable(int level)
        {
            return AvailableLevels.Contains(level);
        }

        public string GetSceneName(int level)
        {
            return _scenePrefix + level;
        }

        private void Compute()
        {
            _availableLevels.Clear();

            int totalScenes = SceneManager.sceneCountInBuildSettings;

            for (int i = 0; i < totalScenes; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = Path.GetFileNameWithoutExtension(path);

                if (TryParseLevelNumber(sceneName, out int level) == true)
                {
                    _availableLevels.Add(level);
                }
            }

            _availableLevels.Sort();
            _isComputed = true;
        }

        private bool TryParseLevelNumber(string sceneName, out int level)
        {
            level = 0;

            if (sceneName.StartsWith(_scenePrefix, StringComparison.Ordinal) == false)
            {
                return false;
            }

            if (sceneName.Length <= _scenePrefix.Length)
            {
                return false;
            }

            string numberPart = sceneName.Substring(_scenePrefix.Length);
            if (int.TryParse(numberPart, out level) == false)
            {
                return false;
            }

            if (level <= 0)
            {
                return false;
            }

            return true;
        }
    }
}