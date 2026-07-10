using UnityEngine;

[CreateAssetMenu(menuName = "Mad Slime/Shop Item", fileName = "NewShopItem")]
public class SkinItem : ScriptableObject
{
    [field: SerializeField] public GameObject Model { get; private set; }
    [field: SerializeField] public Sprite Icon { get; private set; }

    [field: SerializeField, Range(0, 10000)]  public int Price { get; private set; }

    [field: SerializeField] public PlayerSkins SkinType { get; private set; }
}