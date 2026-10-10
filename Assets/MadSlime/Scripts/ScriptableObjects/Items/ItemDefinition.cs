using Skills;
using UnityEngine;

namespace Items
{
    [CreateAssetMenu(menuName = "Mad Slime/Item Definition", fileName = "NewItemDefinition")]
    public class ItemDefinition : ScriptableObject
    {
        [Tooltip("Иконка предмета для тарелок квоты в HUD.")]
        [SerializeField] private Sprite _icon;

        [Tooltip("Тир предмета: определяет массу (TierTable), может ли его съесть игрок и когда он становится призрачным.")]
        [SerializeField] private SizeTier _tier = SizeTier.Small;

        public Sprite Icon => _icon;
        public SizeTier Tier => _tier;
    }
}
