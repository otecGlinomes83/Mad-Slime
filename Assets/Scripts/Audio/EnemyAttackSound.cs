using System;
using NPC.Enemy;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class EnemyAttackSound : MonoBehaviour
    {
        [SerializeField] private Attacker _attacker;
        [SerializeField] private AudioMixerGroup _group;
        [SerializeField] private AudioClip _clip;

        private AudioSource _source;

        private void Awake()
        {
            if (_attacker == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Attacker is not assigned.");
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
            _attacker.AttackPerformed += OnAttackPerformed;
        }

        private void OnDisable()
        {
            _attacker.AttackPerformed -= OnAttackPerformed;
        }

        private void OnAttackPerformed()
        {
            _source.PlayOneShot(_clip);
        }
    }
}