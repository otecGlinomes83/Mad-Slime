using System;
using System.Collections.Generic;
using Scriptables;
using UnityEngine;
using UnityEngine.UI;

namespace Levels
{
    public class LevelProgressPanel : MonoBehaviour
    {
        [SerializeField] private LevelNodeViewFactory _factory;
        [SerializeField] private RectTransform _content;
        [SerializeField] private ScrollRect _scrollRect;
        
        private readonly List<LevelNodeView> _nodes = new List<LevelNodeView>();

        private SkillsConfig _skillsConfig;
        
        public event Action<int> LevelClicked;

        public void Populate(int totalLevels, int currentLevel)
        {
            Clear();

            for (int level = 1; level <= totalLevels; level++)
            {
                LevelNodeView node = _factory.Get(level, _content);
                node.Click += OnNodeClicked;

                if (level < currentLevel)
                {
                    node.Unlock();
                }
                else if (level == currentLevel)
                {
                    node.Unlock();
                    node.Select();
                }
                else
                {
                    node.Lock();
                }

                _nodes.Add(node);
            }
            
            _scrollRect.verticalNormalizedPosition = 0f;
            
        }

        private void OnDisable()
        {
            Clear();
        }

        private void OnNodeClicked(int level)
        {
            LevelClicked?.Invoke(level);
        }

        private void Clear()
        {
            foreach (LevelNodeView node in _nodes)
            {
                node.Click -= OnNodeClicked;
                Destroy(node.gameObject);
            }

            _nodes.Clear();
        }
    }
}