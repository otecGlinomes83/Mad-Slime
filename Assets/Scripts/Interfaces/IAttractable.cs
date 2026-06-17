using UnityEngine;

public interface IAttractable : IMassHolder
{
    Transform Self { get; }
}
