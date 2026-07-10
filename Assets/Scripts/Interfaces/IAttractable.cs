using Skills;
using UnityEngine;

namespace Interfaces
{
    public interface IAttractable : IMassHolder
    {
        ItemTier Tier { get; }
        Transform Self { get; }
    }
}
