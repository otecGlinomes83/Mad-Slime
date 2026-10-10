using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Quota
{
    public class QuotaIntroSequence : MonoBehaviour
    {
        private Sequence _sequence;

        public void Play(IReadOnlyList<QuotaPlateAnimator> plates, float spacing, float slideOffset,
            float duration, float delayStep)
        {
            Cancel();
            _sequence = DOTween.Sequence();
            _sequence.Pause();
            _sequence.SetUpdate(true).SetLink(gameObject);

            for (int i = 0; i < plates.Count; i++)
            {
                Vector3 finalPosition = new Vector3(0f, -i * spacing, 0f);
                Vector3 startPosition = finalPosition + Vector3.left * slideOffset;
                Tween intro = plates[i].CreateIntro(startPosition, finalPosition, duration);
                _sequence.Insert(i * delayStep, intro);
            }

            _sequence.Play();
        }

        public void Cancel()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        private void OnDisable()
        {
            Cancel();
        }

        private void OnDestroy()
        {
            Cancel();
        }
    }
}
