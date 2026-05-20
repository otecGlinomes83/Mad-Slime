public interface ICollectable
{
    public int Mass { get; }

    public void Collect();

    public void Release();
}