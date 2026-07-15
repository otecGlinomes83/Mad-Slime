using UnityEngine;

namespace Levels
{
    public sealed class LevelNodeViewFactory : MonoBehaviour
    {
        [SerializeField] private LevelNodeView _levelNodeViewPrefab;

        public LevelNodeView Get(int level, Transform parent)
        {
            LevelNodeView instance = Instantiate(_levelNodeViewPrefab, parent);
            instance.Initialize(level);

            return instance;
        }
    }
}