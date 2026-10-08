using System;
using UnityEngine;

namespace Scriptables
{
    [Serializable]
    public sealed class LevelRange
    {
        [Tooltip("Первый уровень диапазона (нумерация с 1).")]
        [SerializeField] private int _fromLevel = 1;

        [Tooltip("Последний уровень диапазона (включительно).")]
        [SerializeField] private int _toLevel = 3;

        [Tooltip("Конфиг уровня, действующий в этом диапазоне.")]
        [SerializeField] private LevelConfig _config;

        public int FromLevel => _fromLevel;
        public int ToLevel => _toLevel;
        public LevelConfig Config => _config;
    }
}
