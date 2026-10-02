using System;
using System.Collections.Generic;
using Items;
using Player;
using Skills;
using UnityEngine;
using Upgrades;
using VContainer;

namespace Game
{
    public sealed class QuotaItemHighlighter : MonoBehaviour
    {
        [Tooltip("Базовый радиус подсветки квотовых предметов. Растёт с тиром как у детекторов (база * TierResolver), от магнита не зависит.")]
        [SerializeField, Min(0f)] private float _radius = 3f;

        [SerializeField] private Color _gizmoColor = new Color(1f, 0.85f, 0.2f, 1f);

        private LevelGenerator _levelGenerator;
        private LevelProgress _levelProgress;
        private PlayerUpgrades _upgrades;
        private PlayerTier _tierSource;
        private TierResolver _tierResolver;
        private Movement.Mover _mover;
        private float _baseRadius;

        public float Radius => _radius;

        [Inject]
        public void Construct(LevelGenerator levelGenerator, LevelProgress levelProgress,
            PlayerUpgrades upgrades, PlayerTier tierSource, TierResolver tierResolver,
            Movement.Mover mover)
        {
            _levelGenerator = levelGenerator;
            _levelProgress = levelProgress;
            _upgrades = upgrades;
            _tierSource = tierSource;
            _tierResolver = tierResolver;
            _mover = mover;
        }

        private void Awake()
        {
            if (_levelGenerator == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LevelGenerator was not injected. Check that GameLifetimeScope registers LevelGenerator and QuotaItemHighlighter.");
            }

            if (_levelProgress == null)
            {
                throw new InvalidOperationException(
                    $"{name}: LevelProgress was not injected. Check that GameLifetimeScope registers LevelProgress and QuotaItemHighlighter.");
            }

            if (_upgrades == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerUpgrades was not injected. Check that GameLifetimeScope registers PlayerUpgrades and QuotaItemHighlighter.");
            }

            if (_tierSource == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierSource was not injected. Check that GameLifetimeScope registers PlayerTier and QuotaItemHighlighter.");
            }

            if (_tierResolver == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TierResolver was not injected. Check that GameLifetimeScope registers TierResolver and QuotaItemHighlighter.");
            }

            if (_mover == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Mover was not injected. Check that GameLifetimeScope registers Mover and QuotaItemHighlighter.");
            }

            if (_radius < 0f)
            {
                throw new InvalidOperationException(
                    $"{name}: Radius cannot be negative. Set a non-negative value in the _radius field.");
            }

            _baseRadius = _radius;
        }

        private void Update()
        {
            if (Time.timeScale == 0f || _upgrades.HasSmell == false)
            {
                return;
            }

            Vector3 playerPosition = _mover.transform.position;
            float sqrRadius = _radius * _radius;
            Color highlightColor = _upgrades.HighlightColor;
            float outlineWidth = _upgrades.OutlineWidth;
            IReadOnlyList<Item> items = _levelGenerator.SpawnedItems;

            for (int i = 0; i < items.Count; i++)
            {
                Item item = items[i];
                bool inRadius = (item.transform.position - playerPosition).sqrMagnitude <= sqrRadius;

                item.SetHighlighted(
                    inRadius == true && _levelProgress.IsQuotaItem(item.Definition),
                    highlightColor,
                    outlineWidth);
            }
        }

        private void OnEnable()
        {
            _tierSource.TierChanged += OnTierSourceChanged;
            SetRadius(_baseRadius * _tierResolver.GetScaleFor(_tierSource.CurrentTier));
        }

        private void OnDisable()
        {
            _tierSource.TierChanged -= OnTierSourceChanged;
        }

        private void OnTierSourceChanged(ItemTier previousTier, ItemTier currentTier)
        {
            SetRadius(_baseRadius * _tierResolver.GetScaleFor(currentTier));
        }

        private void SetRadius(float newRadius)
        {
            if (newRadius < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(newRadius), "new radius cannot be negative");
            }

            _radius = newRadius;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
