using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Sfx Clip", fileName = "NewSfxClip")]
    public sealed class SfxClip : ScriptableObject
    {
        [Tooltip("Аудиофайл, который проигрывает аудиосистема")]
        [SerializeField] private AudioClip _clip;

        [Tooltip("Громкость проигрывания")]
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;

        [Tooltip("Проигрывать ли звук со случайным питчем в диапазоне Min/Max Pitch")]
        [SerializeField] private bool _isRandomPitch;

        [Tooltip("Нижняя граница случайного питча. Работает только при включённом Random Pitch")]
        [SerializeField, Range(0.1f, 3f)] private float _minPitch = 1f;

        [Tooltip("Верхняя граница случайного питча. Работает только при включённом Random Pitch")]
        [SerializeField, Range(0.1f, 3f)] private float _maxPitch = 1f;

        public AudioClip Clip => _clip;
        public float Volume => _volume;
        public bool IsRandomPitch => _isRandomPitch;
        public float MinPitch => _minPitch;
        public float MaxPitch => _maxPitch;
    }
}