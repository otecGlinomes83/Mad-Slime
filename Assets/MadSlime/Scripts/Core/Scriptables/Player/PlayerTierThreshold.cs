using Skills;
using System;
using UnityEngine;

namespace Scriptables
{
    [Serializable]
    public sealed class PlayerTierThreshold
    {
        [Tooltip("Какой тир открывается при достижении порога.")]
        [SerializeField] private ItemTier _tier;

        [Tooltip("Масса, необходимая для открытия этого тира.")]
        [SerializeField] private int _requiredMass;

        [Tooltip("Во сколько раз растут модель и коллайдер слайма на этом тире.")]
        [SerializeField] private float _scaleMultiplier = 1f;

        [Tooltip("Скорость движения слайма на этом тире (юнитов/с), перекрывает базовую.")]
        [SerializeField] private float _speed = 1f;

        [Tooltip("Во сколько раз отъезжает камера на этом тире.")]
        [SerializeField] private float _cameraOffsetMultiplier = 1f;

        public ItemTier Tier => _tier;
        public int RequiredMass => _requiredMass;
        public float ScaleMultiplier => _scaleMultiplier;
        public float CameraOffsetMultiplier => _cameraOffsetMultiplier;
        public float Speed => _speed;
    }
}
