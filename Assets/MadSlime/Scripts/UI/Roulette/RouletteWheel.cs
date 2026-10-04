using System;
using System.Collections.Generic;
using System.Threading;
using Audio;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Scriptables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Roulette
{
    public sealed class RouletteWheel : MonoBehaviour
    {
        private const float FullCircleDegrees = 360f;
        private const float MinTickIntervalSeconds = 0.06f;
        private const float SettleSwingSeconds = 0.2f;
        private const float WinPunchStrength = 0.12f;
        private const float MaxWinPunchDurationSeconds = 0.5f;
        private const int WinPunchVibrato = 4;
        private const float WinPunchElasticity = 0.5f;

        [SerializeField, Tooltip("Вращающийся контейнер колеса: диск и слоты иконок — его дети.")]
        private RectTransform _spinContainer;
        [SerializeField, Tooltip("Стрелка-указатель: её направление задаёт выигрышную позицию колеса.")]
        private RectTransform _arrow;
        [SerializeField, Tooltip("Слоты иконок — прямые дети SpinContainer, по часовой стрелке. Порядок списка = порядок секторов.")]
        private List<Image> _sectorIcons = new List<Image>();
        [SerializeField, Tooltip("Лейблы секторов — по одному на слот иконки, порядок совпадает со слотами.")]
        private List<TMP_Text> _sectorLabels = new List<TMP_Text>();

        private RouletteConfig _config;
        private SfxPlayer _sfxPlayer;
        private float[] _sectorAngles;
        private float _arrowAngle;
        private float _averageSectorAngle;
        private float _rotation;
        private int _lastTickIndex;
        private float _lastTickTime;
        private int _pendingTargetIndex;
        private bool _isSpinning;
        private bool _isBuilt;
        private Action _spinCompleted;

        public bool IsSpinning => _isSpinning;

        public int SectorCount => _sectorIcons.Count;

        public void Setup(RouletteConfig config, SfxPlayer sfxPlayer)
        {
            _config = config;
            _sfxPlayer = sfxPlayer;
        }

        public void Build(IReadOnlyList<RouletteSectorIcon> sectors)
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteWheel.Setup was not called. Pass the RouletteConfig before building.");
            }

            if (_spinContainer == null || _arrow == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SpinContainer or Arrow is not assigned. Wire them in the wheel prefab.");
            }

            if (sectors == null || sectors.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteWheel.Build requires at least one sector.");
            }

            if (_sectorIcons.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{name}: the wheel has no icon slots. Add sector icon slots to the prefab.");
            }

            if (_sectorLabels.Count != _sectorIcons.Count)
            {
                throw new InvalidOperationException(
                    $"{name}: label count {_sectorLabels.Count} must match icon slot count {_sectorIcons.Count}.");
            }

            _sectorAngles = new float[_sectorIcons.Count];

            for (int i = 0; i < _sectorIcons.Count; i++)
            {
                Image sectorIcon = _sectorIcons[i];
                TMP_Text sectorLabel = _sectorLabels[i];

                if (sectorIcon == null)
                {
                    throw new InvalidOperationException(
                        $"{name}: icon slot {i} is not assigned. Wire all sector icon slots in the prefab.");
                }

                if (sectorLabel == null)
                {
                    throw new InvalidOperationException(
                        $"{name}: sector label {i} is not assigned. Wire all sector labels in the prefab.");
                }

                Vector2 slotOffset = sectorIcon.rectTransform.anchoredPosition;

                if (slotOffset.sqrMagnitude <= 0f)
                {
                    throw new InvalidOperationException(
                        $"{name}: icon slot {i} sits at the container center. Offset it from the center.");
                }

                _sectorAngles[i] = Mathf.Atan2(slotOffset.y, slotOffset.x) * Mathf.Rad2Deg;
            }

            Vector2 arrowOffset = _arrow.anchoredPosition;

            if (arrowOffset.sqrMagnitude <= 0f)
            {
                throw new InvalidOperationException(
                    $"{name}: the Arrow sits at the center. Offset it from the wheel center.");
            }

            _arrowAngle = Mathf.Atan2(arrowOffset.y, arrowOffset.x) * Mathf.Rad2Deg;
            _averageSectorAngle = FullCircleDegrees / _sectorAngles.Length;

            for (int i = 0; i < _sectorIcons.Count; i++)
            {
                RouletteSectorIcon sector = sectors[i % sectors.Count];

                if (sector.Icon == null)
                {
                    throw new InvalidOperationException(
                        $"{name}: sector icon at index {i % sectors.Count} is null.");
                }

                _sectorIcons[i].sprite = sector.Icon;
                ApplySectorLabel(_sectorLabels[i], sector.Label);
            }

            ApplyRotation(0f);
            _lastTickIndex = 0;
            _isBuilt = true;
        }

        public void Spin(int targetIndex, Action onComplete)
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteWheel.Setup was not called. Pass the RouletteConfig before spinning.");
            }

            if (_isBuilt == false)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteWheel.Spin was called before Build.");
            }

            if (_isSpinning == true)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteWheel.Spin was called while already spinning.");
            }

            if (targetIndex < 0 || targetIndex >= _sectorAngles.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetIndex),
                    targetIndex,
                    $"RouletteWheel.Spin received a target index outside the 0..{_sectorAngles.Length - 1} sector range.");
            }

            _isSpinning = true;
            _pendingTargetIndex = targetIndex;
            _spinCompleted = onComplete;

            DOTween.Kill(this);

            PlayClip(_config.SpinStartClip);

            int turns = Random.Range(_config.MinTurns, _config.MaxTurns + 1);
            float targetAngle = Mod(_arrowAngle - _sectorAngles[targetIndex], FullCircleDegrees);
            float windBackRotation = _rotation + _config.WindBackDegrees;
            float delta = Mod(windBackRotation - targetAngle, FullCircleDegrees);
            float targetRotation = windBackRotation - (turns * FullCircleDegrees + delta);

            Sequence sequence = DOTween.Sequence();
            sequence.SetTarget(this);
            sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);

            sequence.Append(DOTween.To(ReadRotation, ApplyRotation, windBackRotation, _config.WindBackDuration)
                .SetEase(Ease.InOutQuad));

            float maxDeviation = _config.SettleMaxDeviationDegrees;

            if (maxDeviation > 0f)
            {
                float deviation = Random.Range(0f, maxDeviation);
                float settleRotation = targetRotation - deviation;

                if (Random.Range(0, 2) == 0)
                {
                    settleRotation = targetRotation + deviation;
                }

                sequence.Append(DOTween.To(ReadRotation, ApplyRotation, settleRotation, _config.SpinDuration)
                    .SetEase(EvaluateSpinEase));
                sequence.Append(DOTween.To(ReadRotation, ApplyRotation, targetRotation, SettleSwingSeconds)
                    .SetEase(Ease.OutQuad));
            }
            else
            {
                sequence.Append(DOTween.To(ReadRotation, ApplyRotation, targetRotation, _config.SpinDuration)
                    .SetEase(EvaluateSpinEase));
            }

            sequence.OnComplete(OnSpinCompleted);
        }

        public void ReleaseHold()
        {
            if (_isSpinning == false)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteWheel.ReleaseHold was called while the wheel holds no win.");
            }

            _isSpinning = false;
        }

        private void Update()
        {
            if (_isBuilt == false || _isSpinning == true)
            {
                return;
            }

            if (_rotation <= -FullCircleDegrees)
            {
                _rotation %= FullCircleDegrees;
                _lastTickIndex = Mathf.FloorToInt((_arrowAngle - _rotation) / _averageSectorAngle);
            }

            ApplyRotation(_rotation - _config.IdleRotationSpeed * Time.unscaledDeltaTime);
        }

        private void ApplySectorLabel(TMP_Text label, string labelText)
        {
            if (string.IsNullOrEmpty(labelText) == false)
            {
                label.text = labelText;
                label.gameObject.SetActive(true);
            }
            else
            {
                label.gameObject.SetActive(false);
            }
        }

        private void ApplyRotation(float rotation)
        {
            _rotation = rotation;
            _spinContainer.localRotation = Quaternion.Euler(0f, 0f, _rotation);
            TryPlayStepSound();
        }

        private float ReadRotation()
        {
            return _rotation;
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

            int tickIndex = Mathf.FloorToInt((_arrowAngle - _rotation) / _averageSectorAngle);

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
            float targetAngle = Mod(_arrowAngle - _sectorAngles[_pendingTargetIndex], FullCircleDegrees);

            _rotation = targetAngle + FullCircleDegrees * Mathf.RoundToInt((_rotation - targetAngle) / FullCircleDegrees);

            ApplyRotation(_rotation);
            PunchWinningIcon();
            HoldWinAsync().Forget();
        }

        private void PunchWinningIcon()
        {
            float punchDuration = Mathf.Min(_config.WinDwellSeconds, MaxWinPunchDurationSeconds);

            _sectorIcons[_pendingTargetIndex].rectTransform
                .DOPunchScale(new Vector3(WinPunchStrength, WinPunchStrength, 0f), punchDuration, WinPunchVibrato, WinPunchElasticity)
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

        private static float Mod(float value, float modulus)
        {
            float remainder = value % modulus;

            if (remainder < 0f)
            {
                remainder += modulus;
            }

            return remainder;
        }
    }
}
