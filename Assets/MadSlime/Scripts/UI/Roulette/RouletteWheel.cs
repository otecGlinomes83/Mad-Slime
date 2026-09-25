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

        private readonly List<RouletteSectorCard> _cards = new List<RouletteSectorCard>();

        private RouletteConfig _config;
        private bool _isSpinning;
        private Action _spinCompleted;

        public bool IsSpinning => _isSpinning;

        public int SectorCount => _cards.Count;

        public void Setup(RouletteConfig config)
        {
            _config = config;
        }

        private void Update()
        {
            if (_isSpinning == true || _config == null)
            {
                return;
            }

            _wheelContainer.Rotate(0f, 0f, _config.IdleDegreesPerSecond * Time.unscaledDeltaTime);
        }

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
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteWheel.Setup was not called. Pass the RouletteConfig before spinning.");
            }

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
            _spinCompleted = onComplete;

            float step = 360f / sectorCount;
            int turns = Random.Range(_config.MinTurns, _config.MaxTurns + 1);
            float targetZ = 360f * turns + targetIndex * step;

            Sequence sequence = DOTween.Sequence();
            sequence.SetTarget(this);
            sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);

            float windBackZ = ReadWheelZ() - _config.WindBackDegrees;

            sequence.Append(DOTween.To(ReadWheelZ, ApplyWheelZ, windBackZ, _config.WindBackDuration)
                .SetEase(Ease.InOutQuad));
            sequence.Append(DOTween.To(ReadWheelZ, ApplyWheelZ, targetZ, _config.SpinDuration)
                .SetEase(Ease.OutQuart));
            sequence.OnComplete(OnSpinCompleted);
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

        private void OnDisable()
        {
            _isSpinning = false;
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
