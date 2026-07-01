using Skills;
using UnityEngine;

public interface IAttractable : IMassHolder
{
    ItemTier Tier { get; }
    Transform Self { get; }
}
