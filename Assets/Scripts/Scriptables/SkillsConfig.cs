using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(menuName = "Mad Slime/Skills Config", fileName = "NewSkillsConfig")]
    public sealed class SkillsConfig : ScriptableObject
    {
        [SerializeField] private List<SkillDefinition> _skills = new List<SkillDefinition>();

        public IReadOnlyList<SkillDefinition> Skills => _skills;
    }
}