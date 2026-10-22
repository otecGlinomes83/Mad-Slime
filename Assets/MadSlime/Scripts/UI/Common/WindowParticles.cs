using System;
using UnityEngine;

namespace UI
{
    public class WindowParticles : MonoBehaviour
    {
        [SerializeField] private ParticleSystem[] _systems;

        private void Awake()
        {
            if (_systems == null || _systems.Length == 0)
            {
                throw new InvalidOperationException($"{name}: assign the window particle systems.");
            }

            foreach (ParticleSystem system in _systems)
            {
                if (system == null)
                {
                    throw new InvalidOperationException($"{name}: a window particle system is missing.");
                }
            }
        }

        private void OnEnable()
        {
            foreach (ParticleSystem system in _systems)
            {
                ParticleSystem.MainModule main = system.main;
                main.useUnscaledTime = true;
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                system.Play(true);
            }
        }

        private void OnDisable()
        {
            foreach (ParticleSystem system in _systems)
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
