using System;
using Collectables;
using Items;
using Saves;

namespace Game
{
    public class CollectedItemsCounter : IDisposable
    {
        private ItemCollector _collector;
        private ICollectedItemsStorage _storage;

        public CollectedItemsCounter(ItemCollector collector, ICollectedItemsStorage storage)
        {
            _collector = collector;
            _storage = storage;
            _collector.ItemCollected += OnItemCollected;
        }

        public void Dispose()
        {
            _collector.ItemCollected -= OnItemCollected;
            _storage.CommitCollectedItems();
        }

        private void OnItemCollected(Item item)
        {
            _storage.RegisterCollectedItem();
        }
    }
}
