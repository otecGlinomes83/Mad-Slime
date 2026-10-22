using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    internal class AudioVoicePool
    {
        private List<AudioSource> _sources = new List<AudioSource>();
        private List<float> _sourceEndTimes = new List<float>();
        private AudioMixerGroup _mixerGroup;
        private Transform _ownerTransform;

        public AudioVoicePool(int sourceCount, AudioMixerGroup mixerGroup, Transform ownerTransform)
        {
            _mixerGroup = mixerGroup;
            _ownerTransform = ownerTransform;

            for (int i = 0; i < sourceCount; i++)
            {
                AddSource();
            }
        }

        public void Play(AudioClip clip, float volume, float pitch)
        {
            int freeIndex = FindFreeSourceIndex();

            if (freeIndex < 0)
            {
                freeIndex = AddSource();
            }

            AudioSource source = _sources[freeIndex];
            source.pitch = pitch;
            source.volume = volume;
            source.PlayOneShot(clip);
            _sourceEndTimes[freeIndex] = Time.unscaledTime + clip.length / pitch;
        }

        private int AddSource()
        {
            AudioSource source = _ownerTransform.gameObject.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = _mixerGroup;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;

            _sources.Add(source);
            _sourceEndTimes.Add(0f);

            return _sources.Count - 1;
        }

        private int FindFreeSourceIndex()
        {
            for (int index = 0; index < _sources.Count; index++)
            {
                if (Time.unscaledTime >= _sourceEndTimes[index])
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
