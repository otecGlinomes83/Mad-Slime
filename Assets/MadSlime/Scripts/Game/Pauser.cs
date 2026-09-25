using Core;
using System;
using UnityEngine;
using VContainer;

namespace Game
{
    [DisallowMultipleComponent]
    public sealed class Pauser : MonoBehaviour
    {
        private IAdsService _adsService;
        private int _pauseRequestCount;

        public bool IsPaused => _pauseRequestCount > 0;

        [Inject]
        public void Construct(IAdsService adsService)
        {
            _adsService = adsService;
        }

        private void Awake()
        {
            if (_adsService == null)
            {
                throw new InvalidOperationException(
                    $"{name}: IAdsService was not injected. Check that the scene LifetimeScope registers the Pauser component.");
            }
        }

        public void RequestPause()
        {
            _pauseRequestCount++;
            Time.timeScale = 0f;
        }

        public void RequestResume()
        {
            if (_pauseRequestCount <= 0)
            {
                return;
            }

            _pauseRequestCount--;

            if (_pauseRequestCount <= 0 && _adsService.IsPauseGame == false)
            {
                Time.timeScale = 1f;
            }
        }
    }
}
