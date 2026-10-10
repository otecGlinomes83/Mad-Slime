using Collectables;
using DG.Tweening;
using Items;
using Player;
using Scriptables;
using Skills;
using System;
using UnityEngine;
using VContainer;

namespace CameraSystem
{
    public class CameraPullAnimator : MonoBehaviour
    {
        [SerializeField] private CameraImpulseConfig _config;

        private ItemCollector _itemCollector;
        private PlayerTier _playerTier;
        private TierTable _tierTable;
        private float _pull;
        private Tween _recoverTween;

        public float Pull => _pull;

        [Inject]
        public void Construct(ItemCollector itemCollector, PlayerTier playerTier, TierTable tierTable)
        {
            _itemCollector = itemCollector;
            _playerTier = playerTier;
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
            _playerTier.TierChanged += OnTierChanged;
        }

        private void OnDisable()
        {
            _itemCollector.ItemCollected -= OnItemCollected;
            _playerTier.TierChanged -= OnTierChanged;

            KillRecoverTween();
        }

        private void OnItemCollected(Item item)
        {
            float mass = _tierTable.Get(item.Definition.Tier).Mass;
            float strength = _config.MassToPullStrength.Evaluate(mass);
            float newPull = Mathf.Min(_pull + strength, _config.MaxPull);

            PlayRecoverFrom(newPull);
        }

        private void OnTierChanged(SizeTier previousTier, SizeTier currentTier)
        {
            if (currentTier <= previousTier)
            {
                return;
            }

            float newPull = Mathf.Max(_pull - _config.TierPushStrength, -_config.MaxPush);

            PlayRecoverFrom(newPull);
        }

        private void PlayRecoverFrom(float newPull)
        {
            KillRecoverTween();

            _pull = newPull;

            float recoverDuration = 1f / _config.RecoverSpeed;

            _recoverTween = DOTween.To(ReadPull, ApplyPull, 0f, recoverDuration)
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

        private float ReadPull()
        {
            return _pull;
        }

        private void ApplyPull(float pull)
        {
            _pull = pull;
        }
    }
}
