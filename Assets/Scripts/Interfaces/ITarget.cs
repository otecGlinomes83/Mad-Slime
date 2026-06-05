using Health;
using UnityEngine;

namespace Interfaces
{
    public interface ITarget
    {
        Transform Transform { get; }
        Health.Health Health { get; }
    }
}
