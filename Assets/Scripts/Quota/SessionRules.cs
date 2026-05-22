using System.Collections.Generic;
using UnityEngine;

namespace Quota
{
    [CreateAssetMenu(menuName = "Mad Slime/Session Rules", fileName = "NewSessionRules")]
    public sealed class SessionRules : ScriptableObject
    {
        [SerializeField] private QuotaEntry[] _quota;
        [SerializeField] private float _duration = 60f;

        public IReadOnlyList<QuotaEntry> Quota => _quota;
        public float Duration => _duration;
    }
}
