using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Playlist", fileName = "NewPlaylist")]
    public class Playlist : ScriptableObject
    {
        [Tooltip("Треки плейлиста сцены: играют по порядку с плавными переходами, последний переходит в первый.")]
        [SerializeField] private List<AudioClip> _tracks = new List<AudioClip>();

        public IReadOnlyList<AudioClip> Tracks => _tracks;
    }
}
