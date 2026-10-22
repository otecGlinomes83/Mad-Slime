using System;
using Scriptables;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game
{
    public class LevelEnvironment
    {
        private Transform _parent;
        private GameObject _instance;
        private GameObject _prefab;

        public LevelEnvironment(Transform parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            _parent = parent;
        }

        public void Apply(LevelTheme theme, Bounds floorBounds)
        {
            if (theme == null)
            {
                throw new ArgumentNullException(nameof(theme));
            }

            if (_prefab != theme.EnvironmentPrefab)
            {
                Clear();
            }

            if (theme.EnvironmentPrefab == null)
            {
                return;
            }

            Vector3 position = floorBounds.center;
            position.y = floorBounds.max.y;

            if (_instance == null)
            {
                _instance = Object.Instantiate(theme.EnvironmentPrefab, position, Quaternion.identity, _parent);
                _prefab = theme.EnvironmentPrefab;
            }
            else
            {
                _instance.transform.position = position;
            }
        }

        public void Clear()
        {
            if (_instance != null)
            {
                _instance.SetActive(false);
                Object.Destroy(_instance);
            }

            _instance = null;
            _prefab = null;
        }
    }
}
