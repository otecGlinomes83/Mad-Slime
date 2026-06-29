using Skills;
using System;
using UnityEngine;

namespace Scriptables
{
    [Serializable]
    public sealed class SkillDefinition
    {
        [SerializeField] private SkillId _id;
        [SerializeField] private int _requiredLevel = 1;

        public SkillId Id => _id;
        public int RequiredLevel => _requiredLevel;
    }
}