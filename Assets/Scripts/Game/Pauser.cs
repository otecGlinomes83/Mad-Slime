using System;
using UnityEngine;

namespace Game
{
    [DisallowMultipleComponent]
    public sealed class Pauser : MonoBehaviour
    {
        private int _pauseRequestCount;

        public bool IsPaused => _pauseRequestCount > 0;

        public void RequestPause()
        {
            _pauseRequestCount++;

            if (_pauseRequestCount >= 1)
            {
                Time.timeScale = 0f;
            }
        }

        public void RequestResume()
        {
            if (_pauseRequestCount <= 0)
            {
                return;
            }

            _pauseRequestCount--;

            if (_pauseRequestCount <= 0)
            {
                Time.timeScale = 1f;
            }
        }
    }
}