using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class PlayerPickupSound : MonoBehaviour
    {
        [SerializeField] private Collector _collector;
        [SerializeField] private AudioMixerGroup _group;
        [SerializeField] private AudioClip _clip;

        private AudioSource _source;

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

        private void OnItemCollected(Item.Item item)
        {
            _source.PlayOneShot(_clip);
        }
    }
}