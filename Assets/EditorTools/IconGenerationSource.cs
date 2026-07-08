using UnityEngine;

public sealed class IconGenerationSource : MonoBehaviour
{
    [SerializeField] private GameObject[] _iconSources;
    public GameObject[] IconSources => _iconSources;
}