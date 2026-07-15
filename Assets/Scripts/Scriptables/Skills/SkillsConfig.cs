using System.Collections.Generic;
using Skills;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(fileName = "NewSkillsConfig", menuName = "Mad Slime/Skills Config")]
    public sealed class SkillsConfig : ScriptableObject
    {
        [SerializeField] private List<SkillConfig> _skills = new List<SkillConfig>();

        public IReadOnlyList<SkillConfig> Skills => _skills;
    }
}
