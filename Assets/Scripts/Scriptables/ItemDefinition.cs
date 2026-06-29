using Skills;
using UnityEngine;

namespace Item
{
    [CreateAssetMenu(menuName = "Mad Slime/Item Definition", fileName = "NewItemDefinition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private GameObject _previewModel;
        [SerializeField] private int _baseMass = 1;
        [SerializeField] private ItemTier _tier = ItemTier.Small;

        public string DisplayName => _displayName;
        public GameObject PreviewModel => _previewModel;
        public int BaseMass => _baseMass;
        public ItemTier Tier => _tier;
    }
}