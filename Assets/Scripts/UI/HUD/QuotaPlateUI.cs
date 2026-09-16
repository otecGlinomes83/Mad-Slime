using Quota;
using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class QuotaPlateUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _text;
        [SerializeField, Range(0f, 0.5f)] private float _popStrength = 0.12f;
        [SerializeField, Min(0.01f)] private float _popDuration = 0.18f;

        private QuotaEntry _entry;
        private Vector3 _baseScale;
        private Tween _popTween;
        private float _popProgress;

        public QuotaEntry Entry => _entry;

        private void Awake()
        {
            if (_icon == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Icon is not assigned. Drag an Image into the _icon field.");
            }

            if (_text == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Text is not assigned. Drag a TMP_Text into the _text field.");
            }

            _baseScale = transform.localScale;
        }

        private void OnDisable()
        {
            if (_popTween != null)
            {
                _popTween.Kill();
                _popTween = null;
            }

            transform.localScale = _baseScale;
        }

        public void Setup(QuotaEntry entry)
        {
            _entry = entry;
            _icon.sprite = entry.Definition.Icon;
        }

        public void UpdateCount(int remaining)
        {
            _text.text = remaining.ToString();
            PlayPop();
        }

        private void PlayPop()
        {
            if (_popTween != null)
            {
                _popTween.Kill();
            }

            _popProgress = 0f;
            transform.localScale = _baseScale;

            _popTween = DOTween.To(ReadPopProgress, ApplyPopProgress, 1f, _popDuration)
                .SetEase(Ease.OutQuad)
                .SetTarget(this);
        }

        private float ReadPopProgress()
        {
            return _popProgress;
        }

        private void ApplyPopProgress(float progress)
        {
            _popProgress = progress;
            transform.localScale = _baseScale * (1f + _popStrength * (1f - progress));
        }
    }
}
