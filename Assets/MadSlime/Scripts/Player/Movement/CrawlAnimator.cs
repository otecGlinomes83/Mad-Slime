using Scriptables;
using System;
using PlayerInput;
using UnityEngine;
using VContainer;

namespace Movement
{
    public class CrawlAnimator : MonoBehaviour
    {
        private const float RestStrengthEpsilon = 0.001f;

        [SerializeField] private Mover _mover;

        private PlayerConfig _config;
        private PlayerInputReader _inputReader;
        private Vector3 _baseScale;
        private Vector3 _lastPosition;
        private float _crawlPhase;
        private float _crawlStrength;
        private float _crawlStrengthVelocityRef;
        private bool _isAtRestScale;

        [Inject]
        public void Construct(PlayerConfig config, PlayerInputReader inputReader)
        {
            _config = config;
            _inputReader = inputReader;
        }

        private void Awake()
        {
            if (_mover == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Mover is not assigned. Drag the Mover component into the _mover field.");
            }

            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerConfig was not injected. Check that GameLifetimeScope registers PlayerConfig and CrawlAnimator.");
            }

            if (_inputReader == null)
            {
                throw new InvalidOperationException(
                    $"{name}: PlayerInputReader was not injected. Check that GameLifetimeScope registers PlayerInputReader and CrawlAnimator.");
            }

            if (_config.CrawlStretchCurve == null || _config.CrawlStretchCurve.length == 0)
            {
                throw new InvalidOperationException(
                    "PlayerConfig has an empty CrawlStretchCurve. Add keyframes to the Crawl section.");
            }

            _baseScale = transform.localScale;
            _lastPosition = _mover.transform.position;
            _isAtRestScale = true;
        }

        private void Update()
        {
            UpdateCrawlStrength(Time.deltaTime);
            AdvanceCrawlPhase();
            ApplyDeform();
        }

        private void UpdateCrawlStrength(float deltaTime)
        {
            bool hasInput = _inputReader.MoveInput.sqrMagnitude > 0f;
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
                    _config.CrawlRampTime,
                    Mathf.Infinity,
                    deltaTime
                )
            );
        }

        private void AdvanceCrawlPhase()
        {
            Vector3 currentPosition = _mover.transform.position;
            float traveledDistance = Vector3.Distance(currentPosition, _lastPosition);

            _lastPosition = currentPosition;

            if (traveledDistance <= 0f)
            {
                return;
            }

            float stride = _config.CrawlStride + _mover.Velocity.magnitude * _config.StridePerSpeed;

            if (stride <= 0f)
            {
                return;
            }

            _crawlPhase = (_crawlPhase + traveledDistance / stride) % 1f;
        }

        private void ApplyDeform()
        {
            if (_crawlStrength <= RestStrengthEpsilon)
            {
                if (_isAtRestScale == false)
                {
                    _isAtRestScale = true;
                    transform.localScale = _baseScale;
                }

                return;
            }

            _isAtRestScale = false;

            float stretch = _crawlStrength * _config.DeformMaxStretch * _config.CrawlStretchCurve.Evaluate(_crawlPhase);
            float squeeze = 1f - stretch * _config.DeformSqueeze;

            transform.localScale = new Vector3(
                _baseScale.x * squeeze,
                _baseScale.y * squeeze,
                _baseScale.z * (1f + stretch));
        }
    }
}
