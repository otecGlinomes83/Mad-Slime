using System;
using UnityEngine;

namespace Items
{
    [RequireComponent(typeof(ItemBody))]
    public class Item : MonoBehaviour
    {
        [SerializeField] private ItemDefinition _definition;
        public ItemDefinition Definition => _definition;

        public void SetDefinition(ItemDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition),
                    $"{name}: SetDefinition requires a non-null definition.");
            }

            _definition = definition;
        }

    }
}
