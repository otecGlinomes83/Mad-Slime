using System;
using Assets.Scripts.HealthSystem;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class PlayerDeathSound : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private AudioMixerGroup _group;
        [SerializeField] private AudioClip _clip;

        private AudioSource _source;

        private void Awake()
        {
            if (_health == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Health is not assigned.");
            }

            if (_clip == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AudioClip is not assigned.");
            }

            _source = GetComponent<AudioSource>();
            _source.outputAudioMixerGroup = _group;
            _source.playOnAwake = false;
        }

        private void OnEnable()
        {
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Died -= OnDied;
        }

        private void OnDied()
        {
            _source.PlayOneShot(_clip);
        }
    }
}