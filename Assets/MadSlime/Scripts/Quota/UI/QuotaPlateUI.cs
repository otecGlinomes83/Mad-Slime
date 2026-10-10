using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Quota
{
    [RequireComponent(typeof(QuotaPlateAnimator))]
    public class QuotaPlateUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _text;

        public void Setup(Sprite icon, int remaining)
        {
            _icon.sprite = icon;
            UpdateCount(remaining);
        }

        public void UpdateCount(int remaining)
        {
            _text.text = remaining.ToString();
        }
    }
}
