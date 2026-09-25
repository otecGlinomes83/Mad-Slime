using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Roulette
{
    public sealed class RouletteWheel : MonoBehaviour
    {
        [SerializeField] private RectTransform _wheelContainer;
        [SerializeField] private RouletteSectorCard _sectorCardPrefab;
        [SerializeField, Min(10f)] private float _radius = 220f;
        [SerializeField, Min(1)] private int _minTurns = 3;
        [SerializeField, Min(1)] private int _maxTurns = 5;
        [SerializeField, Min(0.5f)] private float _spinDuration = 3f;

        private readonly List<RouletteSectorCard> _cards = new List<RouletteSectorCard>();

        private bool _isSpinning;
        private int _lastSpinIndex;
        private float _spinStep;
        private Action _spinCompleted;

        public bool IsSpinning => _isSpinning;

        public int SectorCount => _cards.Count;

        public void Build(List<RouletteSectorView> sectors)
        {
            if (sectors == null || sectors.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteWheel.Build requires at least one sector.");
            }

            Clear();

            float step = 360f / sectors.Count;

            for (int i = 0; i < sectors.Count; i++)
            {
                RouletteSectorCard card = Instantiate(_sectorCardPrefab, _wheelContainer);
                card.Initialize(sectors[i]);

                RectTransform cardTransform = (RectTransform)card.transform;
                float angle = -i * step;

                cardTransform.anchoredPosition = Quaternion.Euler(0f, 0f, angle) * Vector3.up * _radius;
                cardTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                cardTransform.localScale = Vector3.one;

                _cards.Add(card);
            }

            _wheelContainer.localRotation = Quaternion.identity;
        }

        public void Spin(int targetIndex, int sectorCount, Action onComplete)
        {
            if (_isSpinning == true)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteWheel.Spin called while already spinning.");
            }

            if (targetIndex < 0 || targetIndex >= sectorCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetIndex),
                    targetIndex,
                    "RouletteWheel.Spin received a target index outside the sector range.");
            }

            _isSpinning = true;

            float step = 360f / sectorCount;
            int turns = Random.Range(_minTurns, _maxTurns + 1);
            float startZ = _wheelContainer.localEulerAngles.z;
            float targetZ = startZ + 360f * turns + targetIndex * step;

            _lastSpinIndex = targetIndex;
            _spinStep = step;
            _spinCompleted = onComplete;

            DOTween.To(ReadWheelZ, ApplyWheelZ, targetZ, _spinDuration)
                .SetEase(Ease.OutQuart)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(OnSpinCompleted);
        }

        private float ReadWheelZ()
        {
            return _wheelContainer.localEulerAngles.z;
        }

        private void ApplyWheelZ(float z)
        {
            _wheelContainer.localRotation = Quaternion.Euler(0f, 0f, z);
        }

        private void OnSpinCompleted()
        {
            _isSpinning = false;

            Action completed = _spinCompleted;
            _spinCompleted = null;
            completed?.Invoke();
        }

        private void Clear()
        {
            for (int i = _cards.Count - 1; i >= 0; i--)
            {
                Destroy(_cards[i].gameObject);
            }

            _cards.Clear();
        }
    }
}
