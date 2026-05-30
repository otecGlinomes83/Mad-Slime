using UnityEngine;

public interface IAttractable
{
    public int Mass { get; }

    public void Attract(Vector3 direction, float attractForce);
}