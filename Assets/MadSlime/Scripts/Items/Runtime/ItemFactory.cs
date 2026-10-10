using UnityEngine;
using Object = UnityEngine.Object;

namespace Items
{
    public class ItemFactory
    {
        public Item Create(Item prefab, ItemDefinition definition, Vector3 position, float scale, Transform parent)
        {
            Item item = Object.Instantiate(prefab, position, Quaternion.identity, parent);

            item.SetDefinition(definition);
            if (item.TryGetComponent(out ItemBody body) == false)
            {
                throw new System.InvalidOperationException($"{prefab.name}: ItemBody is missing.");
            }

            body.Initialize(position, scale);

            return item;
        }
    }
}
