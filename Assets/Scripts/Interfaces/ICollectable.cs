using System;

public interface ICollectable
{
    public int Mass { get; }

    public event Action<ICollectable> ReadyToRelease;

    public void Collect();

    public void Release();
}