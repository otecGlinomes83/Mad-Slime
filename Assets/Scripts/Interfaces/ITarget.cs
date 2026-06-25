using Assets.Scripts.HealthSystem;
using UnityEngine;

namespace Interfaces
{
    public interface ITarget
    {
        Transform Transform { get; }
        Health Health { get; }
    }
}
