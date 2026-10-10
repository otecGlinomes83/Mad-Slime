using Collectables;
using DG.Tweening;
using Items;
using Scriptables;
using System;
using UnityEngine;
using VContainer;

namespace CameraSystem
{
    public class CameraShakeAnimator : MonoBehaviour
    {
        [SerializeField] private CameraImpulseConfig _config;

        private ItemCollector _itemCollector;
        private TierTable _tierTable;
        private float _shake;
        private Tween _recoverTween;

        public float Shake => _shake;

        [Inject]
        public void Construct(ItemCollector itemCollector, TierTable tierTable)
        {
            _itemCollector = itemCollector;
            _tierTable = tierTable;
        }

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: CameraImpulseConfig is not assigned. Drag the CameraImpulseConfig asset into the _config field.");
            }

            if (_tierTable == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierTable was not injected. Check that ProjectLifetimeScope has the TierTable asset assigned.");
            }
        }

        private void OnEnable()
        {
            _itemCollector.ItemCollected += OnItemCollected;
        }

        private void OnDisable()
        {
            _itemCollector.ItemCollected -= OnItemCollected;

            KillRecoverTween();
        }

        private void OnItemCollected(Item item)
        {
            float mass = _tierTable.Get(item.Definition.Tier).Mass;

            if (mass < _config.ShakeMassThreshold)
            {
                return;
            }

            KillRecoverTween();

            _shake = Mathf.Min(_shake + _config.ShakeStrength, _config.MaxShake);

            float recoverDuration = 1f / _config.ShakeRecoverSpeed;

            _recoverTween = DOTween.To(ReadShake, ApplyShake, 0f, recoverDuration)
                .SetEase(Ease.OutQuad)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void KillRecoverTween()
        {
            if (_recoverTween == null)
            {
                return;
            }

            _recoverTween.Kill();
            _recoverTween = null;
        }

        private float ReadShake()
        {
            return _shake;
        }

        private void ApplyShake(float shake)
        {
            _shake = shake;
        }
    }
}
