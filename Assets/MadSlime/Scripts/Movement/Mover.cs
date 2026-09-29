using System;
using Scriptables;
using UnityEngine;
using VContainer;

namespace Movement
{
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class Mover : MonoBehaviour
    {
        [SerializeField] private float _defaultSpeed = 4f;
        [SerializeField] private float _smoothTime = 0.12f;

        private const float MoveThreshold = 0.05f;
        private const float MinDecayVelocity = 0.05f;

        private CapsuleCollider _playerCollider;
        private Bounds _bounds;
        private bool _hasBounds;
        private Vector3 _currentVelocity;
        private Vector3 _velocityRef;
        private float _currentSpeed;
        private float _speedMultiplier = 1f;
        private PlayerConfig _playerConfig;
        private float _crawlPhase;
        private float _crawlStrength;
        private float _crawlStrengthVelocityRef;
        private bool _isCrawlInputActive;

        public Vector3 Velocity => _currentVelocity;

        public float CurrentSpeed => _currentSpeed;

        public float CrawlPhase => _crawlPhase;

        public float CrawlStrength => _crawlStrength;

        private void Awake()
        {
            _playerCollider = GetComponent<CapsuleCollider>();
            _currentSpeed = _defaultSpeed;
        }

        [Inject]
        public void Construct(PlayerConfig playerConfig)
        {
            if (playerConfig == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerConfig was not injected. Check that GameLifetimeScope registers Mover and PlayerConfig.");
            }

            ValidateCrawlConfig(playerConfig);

            _playerConfig = playerConfig;
        }

        private static void ValidateCrawlConfig(PlayerConfig playerConfig)
        {
            if (playerConfig.CrawlStretchCurve == null || playerConfig.CrawlStretchCurve.length == 0)
            {
                throw new InvalidOperationException(
                    "PlayerConfig has an empty CrawlStretchCurve. Add keyframes to the Crawl section.");
            }

            if (playerConfig.CrawlThrustCurve == null || playerConfig.CrawlThrustCurve.length == 0)
            {
                throw new InvalidOperationException(
                    "PlayerConfig has an empty CrawlThrustCurve. Add keyframes to the Crawl section.");
            }

            if (playerConfig.CrawlStride <= 0f && playerConfig.StridePerSpeed <= 0f)
            {
                throw new InvalidOperationException(
                    "PlayerConfig: CrawlStride and StridePerSpeed are both zero, the crawl cycle can never advance.");
            }
        }

        public void SetDefaultSpeed(float speed)
        {
            if (speed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speed),
                    "Mover.SetDefaultSpeed requires a positive speed.");
            }

            _defaultSpeed = speed;
            _currentSpeed = speed * _speedMultiplier;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            if (multiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(multiplier),
                    "Mover.SetSpeedMultiplier requires a positive multiplier.");
            }

            _speedMultiplier = multiplier;
            _currentSpeed = _defaultSpeed * _speedMultiplier;
        }

        public void SetSmoothTime(float smoothTime)
        {
            if (smoothTime <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(smoothTime),
                    "Mover.SetSmoothTime requires a positive smooth time.");
            }

            _smoothTime = smoothTime;
        }

        public void SetBounds(Bounds bounds)
        {
            if (bounds.size.x <= 0f || bounds.size.z <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(bounds),
                    "Mover.SetBounds requires positive XZ size.");
            }

            _bounds = bounds;
            _hasBounds = true;
        }

        public void Move(Vector3 direction)
        {
            if (_hasBounds == false)
            {
                throw new InvalidOperationException(
                    $"{name}: Move is called before SetBounds. Drag the Mover into the _mover field of LevelGenerator.");
            }

            bool hasInput = direction.sqrMagnitude >= MoveThreshold * MoveThreshold;

            UpdateCrawlStrength(hasInput, Time.deltaTime);

            if (hasInput == false)
            {
                DecayVelocity();
                return;
            }

            direction = direction.normalized;

            float crawlVelocityScale = GetCrawlVelocityScale();
            Vector3 targetVelocity = direction * (_currentSpeed * crawlVelocityScale);

            _currentVelocity = Vector3.SmoothDamp
            (
                _currentVelocity,
                targetVelocity,
                ref _velocityRef,
                _smoothTime
            );

            transform.position += _currentVelocity * Time.deltaTime;

            AdvanceCrawlPhase(_currentVelocity.magnitude * Time.deltaTime);

            ClampToBounds();
        }

        private void DecayVelocity()
        {
            _currentVelocity = Vector3.SmoothDamp
            (
                _currentVelocity,
                Vector3.zero,
                ref _velocityRef,
                _smoothTime
            );

            if (_currentVelocity.sqrMagnitude < MinDecayVelocity * MinDecayVelocity)
            {
                _currentVelocity = Vector3.zero;
                return;
            }

            transform.position += _currentVelocity * Time.deltaTime;

            AdvanceCrawlPhase(_currentVelocity.magnitude * Time.deltaTime);

            ClampToBounds();
        }

        private void UpdateCrawlStrength(bool hasInput, float deltaTime)
        {
            if (hasInput != _isCrawlInputActive)
            {
                _isCrawlInputActive = hasInput;

                if (hasInput == true)
                {
                    _crawlPhase = 0f;
                    _crawlStrengthVelocityRef = 0f;
                }
            }

            float targetStrength;

            if (hasInput == true)
            {
                targetStrength = 1f;
            }
            else
            {
                targetStrength = 0f;
            }

            _crawlStrength = Mathf.Clamp01
            (
                Mathf.SmoothDamp
                (
                    _crawlStrength,
                    targetStrength,
                    ref _crawlStrengthVelocityRef,
                    _playerConfig.CrawlRampTime,
                    Mathf.Infinity,
                    deltaTime
                )
            );
        }

        private float GetCrawlVelocityScale()
        {
            float thrust = _playerConfig.CrawlThrustCurve.Evaluate(_crawlPhase);

            return 1f + _playerConfig.CrawlThrustDepth * (thrust - 1f) * _crawlStrength;
        }

        private void AdvanceCrawlPhase(float distance)
        {
            if (distance <= 0f)
            {
                return;
            }

            float stride = _playerConfig.CrawlStride + _currentSpeed * _playerConfig.StridePerSpeed;

            if (stride <= 0f)
            {
                return;
            }

            _crawlPhase = (_crawlPhase + distance / stride) % 1f;
        }

        private void ClampToBounds()
        {
            float radius = _playerCollider.radius;
            Vector3 position = transform.position;

            position.x = Mathf.Clamp(position.x, _bounds.min.x + radius, _bounds.max.x - radius);
            position.z = Mathf.Clamp(position.z, _bounds.min.z + radius, _bounds.max.z - radius);

            transform.position = position;
        }
    }
}
