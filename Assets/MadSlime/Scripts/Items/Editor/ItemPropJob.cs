using System.Collections.Generic;
using Items;
using UnityEngine;

namespace EditorTools
{
    internal class ItemPropJob
    {
        private GameObject _model;
        private string _prefabName;
        private string _prefabPath;
        private string _iconPath;
        private ItemDefinition _fallbackDefinition;
        private Item _prefab;
        private List<ItemDefinition> _definitions = new List<ItemDefinition>();

        public GameObject Model => _model;
        public string PrefabName => _prefabName;
        public string PrefabPath => _prefabPath;
        public string IconPath => _iconPath;
        public ItemDefinition FallbackDefinition => _fallbackDefinition;
        public Item Prefab => _prefab;
        public IReadOnlyList<ItemDefinition> Definitions => _definitions;

        public ItemPropJob(GameObject model, string prefabsPath, string iconsPath)
        {
            _model = model;
            _prefabName = $"Item_{model.name}";
            _prefabPath = $"{prefabsPath}/{_prefabName}.prefab";
            _iconPath = $"{iconsPath}/{_prefabName}.png";
        }

        public void SetPrefab(Item prefab)
        {
            _prefab = prefab;
        }

        public void AddDefinition(ItemDefinition definition)
        {
            _definitions.Add(definition);
        }

        public void SetFallbackDefinition(ItemDefinition definition)
        {
            _fallbackDefinition = definition;
        }
    }
}
