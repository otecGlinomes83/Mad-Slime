using Skills;
using UnityEngine;

namespace Items
{
    [CreateAssetMenu(menuName = "Mad Slime/Item Definition", fileName = "NewItemDefinition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private Sprite _icon;
        [SerializeField] private ItemTier _tier = ItemTier.Small;

        public Sprite Icon => _icon;
        public ItemTier Tier => _tier;
    }
}
