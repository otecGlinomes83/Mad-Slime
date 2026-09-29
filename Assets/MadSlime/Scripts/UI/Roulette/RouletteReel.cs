using System;
using System.Collections.Generic;
using System.Threading;
using Audio;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Scriptables;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Roulette
{
    public sealed class RouletteReel : MonoBehaviour
    {
        private const float MinTickIntervalSeconds = 0.06f;
        private const int LoopNormalizationTurns = 64;
        private const float SnapZone = 0.3f;
        private const float WinPunchStrength = 0.06f;

        [SerializeField] private RectTransform _viewport;
        [SerializeField] private RectTransform _content;
        [SerializeField, Tooltip("Центральная зона ленты: её высота задаёт размер главной ячейки и линии-разделители.")]
        private RectTransform _centerZone;
        [SerializeField] private RouletteSectorCard _cardPrefab;
        [SerializeField, Min(2), Tooltip("Сколько ячеек видно помимо центральной. Чётное: половина сверху, половина снизу.")]
        private int _visibleRowCount = 2;

        private readonly List<RouletteSectorCard> _cards = new List<RouletteSectorCard>();
        private readonly List<RouletteSectorView> _entries = new List<RouletteSectorView>();
        private readonly List<int> _cardEntryIndices = new List<int>();

        private RouletteConfig _config;
        private SfxPlayer _sfxPlayer;
        private float _cardWidth;
        private float _centerHeight;
        private float _sideHeight;
        private float _baseCardHeight;
        private float _position;
        private int _lastTickIndex;
        private float _lastTickTime;
        private int _pendingTargetIndex;
        private bool _isSpinning;
        private bool _isBuilt;
        private bool _isIdleLoopStarted;
        private Action _spinCompleted;

        public bool IsSpinning => _isSpinning;

        public int EntryCount => _entries.Count;

        public void Setup(RouletteConfig config, SfxPlayer sfxPlayer)
        {
            _config = config;
            _sfxPlayer = sfxPlayer;
        }

        public void Build(List<RouletteSectorView> entries)
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteReel.Setup was not called. Pass the RouletteConfig before building.");
            }

            if (_viewport == null || _content == null || _centerZone == null || _cardPrefab == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a reel part is not assigned. Drag the Viewport, Content, CenterBand and the RouletteSectorCard prefab into the fields.");
            }

            if (entries == null || entries.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteReel.Build requires at least one entry.");
            }

            if (_visibleRowCount % 2 != 0)
            {
                throw new InvalidOperationException(
                    $"{name}: Visible Row Count must be even — it splits in half above and below the center cell. " +
                    $"Current value: {_visibleRowCount}.");
            }

            float viewportHeight = _viewport.rect.height;
            _centerHeight = _centerZone.rect.height;

            if (_centerHeight <= 0f || _centerHeight >= viewportHeight)
            {
                throw new InvalidOperationException(
                    $"{name}: the CenterBand height must be inside the Viewport — it defines the center cell. " +
                    $"CenterBand: {_centerHeight:0}, Viewport: {viewportHeight:0}.");
            }

            _cardWidth = _viewport.rect.width;
            _sideHeight = (viewportHeight - _centerHeight) / _visibleRowCount;

            RectTransform cardPrefabRect = (RectTransform)_cardPrefab.transform;
            _baseCardHeight = cardPrefabRect.rect.height;

            if (_baseCardHeight <= 0f)
            {
                throw new InvalidOperationException(
                    $"{name}: the sector card prefab root has zero height. Author it at the center cell size.");
            }

            _entries.Clear();

            for (int i = 0; i < entries.Count; i++)
            {
                _entries.Add(entries[i]);
            }

            EnsureCards();

            _position = 0f;
            _lastTickIndex = 0;
            _isBuilt = true;

            Reposition();
            StartIdleLoop();
        }

        public void Spin(int targetIndex, int entryCount, Action onComplete)
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteReel.Setup was not called. Pass the RouletteConfig before spinning.");
            }

            if (_isBuilt == false)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteReel.Spin was called before Build.");
            }

            if (_isSpinning == true)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteReel.Spin was called while already spinning.");
            }

            if (targetIndex < 0 || targetIndex >= entryCount || entryCount != _entries.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetIndex),
                    targetIndex,
                    "RouletteReel.Spin received a target index outside the entry range.");
            }

            _isSpinning = true;
            _pendingTargetIndex = targetIndex;
            _spinCompleted = onComplete;

            DOTween.Kill(this);

            PlayClip(_config.SpinStartClip);

            int entryTotal = _entries.Count;
            int turns = Random.Range(_config.MinTurns, _config.MaxTurns + 1);
            float windBackPosition = _position + _config.WindBackCards;
            float delta = windBackPosition - targetIndex;
            delta = Mod(delta, entryTotal);
            float targetPosition = windBackPosition - (turns * entryTotal + delta);

            Sequence sequence = DOTween.Sequence();
            sequence.SetTarget(this);
            sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);

            sequence.Append(DOTween.To(ReadPosition, ApplyPosition, windBackPosition, _config.WindBackDuration)
                .SetEase(Ease.InOutQuad));
            sequence.Append(DOTween.To(ReadPosition, ApplyPosition, targetPosition, _config.SpinDuration)
                .SetEase(EvaluateSpinEase));
            sequence.OnComplete(OnSpinCompleted);
        }

        private void EnsureCards()
        {
            if (_cards.Count > 0)
            {
                return;
            }

            int cardCount = _visibleRowCount + 2;

            for (int i = 0; i < cardCount; i++)
            {
                RouletteSectorCard card = Instantiate(_cardPrefab, _content);
                RectTransform cardTransform = (RectTransform)card.transform;

                cardTransform.anchorMin = new Vector2(0.5f, 0.5f);
                cardTransform.anchorMax = new Vector2(0.5f, 0.5f);
                cardTransform.pivot = new Vector2(0.5f, 0.5f);
                cardTransform.anchoredPosition = Vector2.zero;
                cardTransform.localScale = Vector3.one;

                ApplyCardPhase(cardTransform, 0f);

                _cards.Add(card);
                _cardEntryIndices.Add(-1);
            }
        }

        private void StartIdleLoop()
        {
            if (_isIdleLoopStarted == true)
            {
                return;
            }

            _isIdleLoopStarted = true;
            RunIdleLoopAsync().Forget();
        }

        private async UniTaskVoid RunIdleLoopAsync()
        {
            CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();

            try
            {
                while (true)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_config.IdleStepInterval),
                        DelayType.Realtime,
                        PlayerLoopTiming.Update,
                        cancellationToken);

                    if (_isBuilt == false || _isSpinning == true || isActiveAndEnabled == false)
                    {
                        continue;
                    }

                    IdleStep();
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private void IdleStep()
        {
            int entryTotal = _entries.Count;
            float normalizationLimit = entryTotal * LoopNormalizationTurns;

            if (_position < -normalizationLimit)
            {
                _position = Mod(_position, entryTotal);
                _lastTickIndex = Mathf.FloorToInt(_position);
            }

            float targetPosition = _position - 1f;

            DOTween.To(ReadPosition, ApplyPosition, targetPosition, _config.IdleStepDuration)
                .SetEase(Ease.OutBack)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void Reposition()
        {
            int entryTotal = _entries.Count;
            int baseIndex = Mathf.FloorToInt(_position) - 1;

            for (int k = 0; k < _cards.Count; k++)
            {
                int entryIndex = Mod(baseIndex + k, entryTotal);
                RouletteSectorCard card = _cards[k];

                if (_cardEntryIndices[k] != entryIndex)
                {
                    card.Initialize(_entries[entryIndex]);
                    _cardEntryIndices[k] = entryIndex;
                }

                ApplyCardPhase((RectTransform)card.transform, baseIndex + k - _position);
            }
        }

        // Cells morph along the reel: a full-size cell inside the center zone,
        // tucked cells filling the remaining strips above and below. The whole
        // card scales uniformly — icon and text shrink and grow with the plate —
        // while the rect is stretched by 1/scale horizontally, so the plate keeps
        // rendering at the full window width. Distance is measured in cell steps
        // from the center slot; at whole distances every card sits flush inside
        // its strip, so the viewport never clips anything. Growth is compressed
        // into the last SnapZone of a step, so a cell travels at strip size and
        // pops into the center slot instead of easing under a lens.
        private void ApplyCardPhase(RectTransform cardTransform, float delta)
        {
            float direction = Mathf.Approximately(delta, 0f) ? 0f : Mathf.Sign(delta);
            float distance = Mathf.Abs(delta);
            float scale = CellHeight(distance) / _baseCardHeight;

            Vector2 anchoredPosition = cardTransform.anchoredPosition;
            anchoredPosition.y = direction * SlotY(distance);
            cardTransform.anchoredPosition = anchoredPosition;

            cardTransform.sizeDelta = new Vector2(_cardWidth / scale, _baseCardHeight);
            cardTransform.localScale = new Vector3(scale, scale, 1f);
        }

        private float SlotY(float distance)
        {
            float firstStripOffset = (_centerHeight + _sideHeight) * 0.5f;

            if (distance <= 1f)
            {
                return distance * firstStripOffset;
            }

            return firstStripOffset + (distance - 1f) * _sideHeight;
        }

        private float CellHeight(float distance)
        {
            float blendInput = Mathf.Clamp01(1f - distance / SnapZone);
            float blend = blendInput * blendInput * (3f - 2f * blendInput);

            return _sideHeight + (_centerHeight - _sideHeight) * blend;
        }

        private void ApplyPosition(float value)
        {
            _position = value;
            Reposition();
            TryPlayStepSound();
        }

        private float ReadPosition()
        {
            return _position;
        }

        private float EvaluateSpinEase(float time, float duration, float overshootOrAmplitude, float period)
        {
            float normalized = time / duration;

            return 1f - Mathf.Pow(1f - normalized, _config.SpinEasePower);
        }

        private void TryPlayStepSound()
        {
            if (_config.StepClip == null || _sfxPlayer == null)
            {
                return;
            }

            int tickIndex = Mathf.FloorToInt(_position);

            if (tickIndex == _lastTickIndex || Time.unscaledTime - _lastTickTime < MinTickIntervalSeconds)
            {
                return;
            }

            _lastTickIndex = tickIndex;
            _lastTickTime = Time.unscaledTime;
            _sfxPlayer.PlayUi(_config.StepClip);
        }

        private void PlayClip(SfxClip clip)
        {
            if (clip == null || _sfxPlayer == null)
            {
                return;
            }

            _sfxPlayer.PlayUi(clip);
        }

        private void OnSpinCompleted()
        {
            int entryTotal = _entries.Count;
            _position = _pendingTargetIndex + entryTotal * Mathf.RoundToInt((_position - _pendingTargetIndex) / entryTotal);

            Reposition();
            PunchCenterCard();
            HoldWinAsync().Forget();
        }

        public void ReleaseHold()
        {
            if (_isSpinning == false)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteReel.ReleaseHold was called while the reel holds no win.");
            }

            _isSpinning = false;
        }

        private void PunchCenterCard()
        {
            float punchDuration = Mathf.Min(_config.WinDwellSeconds, 0.5f);

            ((RectTransform)_cards[1].transform)
                .DOPunchScale(new Vector3(WinPunchStrength, WinPunchStrength, 0f), punchDuration, 4, 0.5f)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private async UniTaskVoid HoldWinAsync()
        {
            CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();

            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_config.WinDwellSeconds),
                    DelayType.Realtime,
                    PlayerLoopTiming.Update,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            Action completed = _spinCompleted;
            _spinCompleted = null;
            completed?.Invoke();
        }

        private void OnDisable()
        {
            _isSpinning = false;
        }

        private static float Mod(float value, int modulus)
        {
            float remainder = value % modulus;

            if (remainder < 0f)
            {
                remainder += modulus;
            }

            return remainder;
        }

        private static int Mod(int value, int modulus)
        {
            int remainder = value % modulus;

            if (remainder < 0)
            {
                remainder += modulus;
            }

            return remainder;
        }
    }
}
