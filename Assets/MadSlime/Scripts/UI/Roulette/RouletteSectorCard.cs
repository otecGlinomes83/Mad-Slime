using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Roulette
{
    public sealed class RouletteSectorCard : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _label;

        public void Initialize(RouletteSectorView view)
        {
            _background.color = view.Color;

            if (string.IsNullOrEmpty(view.Label) == false)
            {
                _label.text = view.Label;
                _label.gameObject.SetActive(true);
            }
            else
            {
                _label.gameObject.SetActive(false);
            }

            if (view.Icon != null)
            {
                _icon.sprite = view.Icon;
                _icon.gameObject.SetActive(true);
            }
            else
            {
                _icon.gameObject.SetActive(false);
            }
        }
    }
}
