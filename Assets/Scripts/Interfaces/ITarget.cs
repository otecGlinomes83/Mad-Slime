using Assets.Scripts.HealthSystem;
using Skills;
using UnityEngine;

namespace Interfaces
{
    public interface ITarget
    {
        public ItemTier Tier { get; }
        public Transform Transform { get; }
        public Health Health { get; }
    }
}
