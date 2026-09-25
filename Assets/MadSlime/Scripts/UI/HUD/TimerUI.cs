using System;
using Game;
using TMPro;
using UnityEngine;
using VContainer;

namespace UI
{
    public sealed class TimerUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _timerText;

        private Timer _timer;

        [Inject]
        public void Construct(Timer timer)
        {
            _timer = timer;
        }

        private void Awake()
        {
            if (_timer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Timer was not injected. Check that GameLifetimeScope registers Timer and TimerUI.");
            }

            if (_timerText == null)
            {
                throw new InvalidOperationException(
                    $"{name}: TMP_Text is not assigned. Drag a TMP_Text component into the _timerText field.");
            }
        }

        private void OnEnable()
        {
            _timer.Ticked += OnTimerTicked;
        }

        private void OnDisable()
        {
            _timer.Ticked -= OnTimerTicked;
        }

        private void OnTimerTicked(float remaining)
        {
            float display = Mathf.Max(0f, remaining);
            _timerText.text = $"{display:0.0}";
        }
    }
}