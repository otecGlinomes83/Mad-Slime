using Collectables;
using Items;
using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class PlayerPickupSound : MonoBehaviour
    {
        [SerializeField] private Collector _collector;
        [SerializeField] private AudioMixerGroup _group;
        [SerializeField] private AudioClip _clip;
        [SerializeField] private float _minInterval = 0.1f;
        
        private AudioSource _source;

        private float _lastPlayedTime;
        
        private void Awake()
        {
            if (_collector == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Collector is not assigned. Drag a Collector component into the _collector field.");
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
            _collector.ItemCollected += OnItemCollected;
        }

        private void OnDisable()
        {
            _collector.ItemCollected -= OnItemCollected;
        }

        private void OnItemCollected(Items.Item item)
        {
            if (Time.time - _lastPlayedTime < _minInterval)
            {
                return;
            }
            
            _lastPlayedTime = Time.time;
            _source.pitch = Random.Range(0.96f, 1.1f);
            _source.PlayOneShot(_clip);
        }
    }
}