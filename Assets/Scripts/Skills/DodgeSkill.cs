using System;
using UnityEngine;

namespace Skills
{
    public sealed class DodgeSkill : BaseSkill
    {
        [SerializeField] private DodgeConfig _config;

        public event Action Dodged;

        private void Awake()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: DodgeConfig is not assigned. Drag a DodgeConfig asset into the _config field.");
            }
        }

        public override bool TryActivate()
        {
            bool dodged = UnityEngine.Random.value < _config.DodgeChance;

            if (dodged == true)
            {
                Dodged?.Invoke();
            }

            return dodged;
        }
    }
}
