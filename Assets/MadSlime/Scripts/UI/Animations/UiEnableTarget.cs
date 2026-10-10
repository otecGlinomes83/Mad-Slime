using System;
using UnityEngine;

namespace UI.Animations
{
    [Serializable]
    public class UiEnableTarget
    {
        [SerializeField] private GameObject _target;

        [Tooltip("Пауза перед включением после предыдущего элемента списка (с, реальное время).")]
        [SerializeField, Min(0f)] private float _delay;

        public GameObject Target => _target;
        public float Delay => _delay;
    }
}
