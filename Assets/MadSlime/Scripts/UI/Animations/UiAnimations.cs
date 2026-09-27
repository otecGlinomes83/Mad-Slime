using System;
using DG.Tweening;
using UnityEngine;

namespace UI.Animations
{
    public static class UiAnimations
    {
        public const float WindowScaleInDuration = 0.25f;
        public const float WindowScaleOutDuration = 0.2f;

        public static void ScaleIn(RectTransform target, float duration)
        {
            if (target == null)
            {
                return;
            }

            Vector3 baseScale = target.localScale;

            target.DOKill();
            target.localScale = baseScale * 0.6f;

            target.DOScale(baseScale, duration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetLink(target.gameObject);
        }

        public static void ScaleOut(RectTransform target, float duration, Action onCompleted)
        {
            if (target == null)
            {
                onCompleted?.Invoke();
                return;
            }

            target.DOKill();

            target.DOScale(Vector3.zero, duration)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .SetLink(target.gameObject)
                .OnComplete(() => onCompleted?.Invoke());
        }
    }
}
