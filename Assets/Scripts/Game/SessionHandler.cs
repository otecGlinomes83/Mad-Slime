using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public sealed class SessionHandler : MonoBehaviour
    {
        [SerializeField] private Timer _timer;
        [SerializeField] private float _timerDuration;
        [SerializeField] private List<ISpawner> _spawners;

        public void Setup()
        {
            _timer.Setup(_timerDuration);
        }

        public void Start()
        {
            _timer.Start();

            StartSpawners();
        }

        public void Pause()
        {
            _timer.Stop();
            Time.timeScale = 0;
        }

        public void Resume()
        {
            _timer.Start();
            Time.timeScale = 1;
        }
        
        public void Finish()
        {   
            _timer.Stop();
            StopSpawners();
        }

        private void StopSpawners()
        {
            foreach (ISpawner spawner in _spawners)
            {
                spawner.StopSpawn();
            }
        }

        private void StartSpawners()
        {
            foreach (ISpawner spawner in _spawners)
            {
                spawner.StartSpawn();
            }
        }
    }
}