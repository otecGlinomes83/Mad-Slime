using DG.Tweening;
using UnityEngine;

namespace Items
{
    [CreateAssetMenu(fileName = "NewGhostFadeConfig", menuName = "Mad Slime/Ghost Fade Config")]
    public sealed class GhostFadeConfig : ScriptableObject
    {
        [Tooltip("Длительность плавного перехода предмета между обычным видом и призрачной сеткой (с).")]
        [SerializeField] private float _fadeDuration = 0.25f;

        [Tooltip("Кривая затухания перехода (ease из DOTween).")]
        [SerializeField] private Ease _ease = Ease.InOutSine;

        public float FadeDuration => _fadeDuration;
        public Ease Ease => _ease;
    }
}
