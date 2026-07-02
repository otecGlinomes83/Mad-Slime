using System;
using System.Collections.Generic;
using Scriptables;
using UnityEngine;
using YG;

namespace Skills
{
    public sealed class SkillTracker : MonoBehaviour
    {
        [SerializeField] private SkillsConfig _config;

        private readonly HashSet<SkillId> _unlocked = new HashSet<SkillId>();

        public event Action<SkillId> SkillUnlocked;

        public bool IsUnlocked(SkillId id)
        {
            return _unlocked.Contains(id);
        }

        private void Start()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SkillsConfig is not assigned. Drag a SkillsConfig asset into the _config field.");
            }

            int currentLevel = YG2.saves.CurrentLevel;
            IReadOnlyList<SkillDefinition> skills = _config.Skills;

            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition skill = skills[i];

                if (skill.RequiredLevel <= currentLevel)
                {
                    _unlocked.Add(skill.Id);
                }
            }

            foreach (SkillId id in _unlocked)
            {
                SkillUnlocked?.Invoke(id);
            }
        }
    }
}