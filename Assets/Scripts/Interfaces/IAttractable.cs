using Skills;
using UnityEngine;

namespace Interfaces
{
    public interface IAttractable
    {
        ItemTier Tier { get; }
        Transform Self { get; }
    }
}
