namespace Saves
{
    public interface ICollectedItemsStorage
    {
        int CollectedItemsCount { get; }

        void RegisterCollectedItem();

        void CommitCollectedItems();
    }
}
