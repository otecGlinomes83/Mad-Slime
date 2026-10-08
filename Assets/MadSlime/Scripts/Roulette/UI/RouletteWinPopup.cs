using System;
using Skins;
using TMPro;
using UI;
using UI.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Roulette
{
    public sealed class RouletteWinPopup : MonoBehaviour, IShowable
    {
        private const float FadeAlpha = 0.85f;

        [SerializeField] private RectTransform _panel;
        [SerializeField] private Image _fade;
        [SerializeField, Tooltip("Иконка скина: спрайт подставляется из скина при выигрыше.")]
        private Image _skinIcon;
        [SerializeField, Tooltip("Иконка монет: спрайт назначается в префабе, включается при денежном выигрыше.")]
        private Image _coinIcon;
        [SerializeField, Tooltip("Фон-подложка под призом: спрайт переключается по редкости.")]
        private Image _prizeFrame;
        [SerializeField, Tooltip("Голубой фон: монеты и обычные скины.")]
        private Sprite _normalFrame;
        [SerializeField, Tooltip("Зелёный фон редких скинов.")]
        private Sprite _rareFrame;
        [SerializeField, Tooltip("Фиолетовый фон эпических скинов.")]
        private Sprite _epicFrame;
        [SerializeField, Tooltip("Оранжевый фон легендарных скинов.")]
        private Sprite _legendaryFrame;
        [SerializeField] private Image _plate;
        [SerializeField, Tooltip("Голубая плашка: денежные призы и скины обычной редкости.")]
        private Sprite _normalPlate;
        [SerializeField, Tooltip("Зелёная плашка редких скинов.")]
        private Sprite _rarePlate;
        [SerializeField, Tooltip("Фиолетовая плашка эпических скинов.")]
        private Sprite _epicPlate;
        [SerializeField, Tooltip("Оранжевая плашка легендарных скинов.")]
        private Sprite _legendaryPlate;
        [SerializeField] private TMP_Text _plateText;
        [SerializeField] private Button _takeButton;

        public event Action Closed;

        private void Awake()
        {
            if (_panel == null || _fade == null || _skinIcon == null || _coinIcon == null || _prizeFrame == null
                || _plate == null || _plateText == null || _takeButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a win popup part is not assigned. Drag the Panel, Fade, SkinIcon, CoinIcon, PrizeFrame, " +
                    "Plate, PlateText and TakeButton into the fields.");
            }

            if (_normalFrame == null || _rareFrame == null || _epicFrame == null || _legendaryFrame == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a prize frame sprite is not assigned. Drag the four frame sprites into the fields.");
            }

            if (_normalPlate == null || _rarePlate == null || _epicPlate == null || _legendaryPlate == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a plate sprite is not assigned. Drag the GUI Kit grade label sprites (blue, green, purple, orange) into the fields.");
            }

            if (_coinIcon.sprite == null)
            {
                throw new InvalidOperationException(
                    $"{name}: CoinIcon has no sprite. Assign the coin sprite on the CoinIcon image in the prefab.");
            }
        }

        private void OnEnable()
        {
            _takeButton.onClick.AddListener(OnTakeClicked);
        }

        private void OnDisable()
        {
            _takeButton.onClick.RemoveListener(OnTakeClicked);
        }

        public void Show()
        {
            gameObject.SetActive(true);

            _takeButton.interactable = true;
            _fade.color = new Color(0f, 0f, 0f, FadeAlpha);

            UiAnimations.ScaleIn(_panel, UiAnimations.WindowScaleInDuration);
        }

        public void Hide()
        {
            _takeButton.interactable = false;

            UiAnimations.ScaleOut(_panel, UiAnimations.WindowScaleOutDuration, HideAndDestroy);
        }

        public void ShowSkin(SkinItem skin, SkinRarity rarity, string rarityName)
        {
            if (gameObject.activeSelf == false)
            {
                throw new InvalidOperationException(
                    $"{name}: ShowSkin requires a shown popup. Spawn the popup through UiSpawner first.");
            }

            if (skin == null)
            {
                throw new ArgumentNullException(nameof(skin),
                    $"{name}: ShowSkin requires a skin.");
            }

            if (skin.Icon == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SkinItem '{skin.name}' has no icon. Assign the Icon in the skin asset.");
            }

            _skinIcon.sprite = skin.Icon;
            _skinIcon.gameObject.SetActive(true);
            _coinIcon.gameObject.SetActive(false);

            ShowRarity(rarity, rarityName);
        }

        public void ShowCoins(int amount)
        {
            if (gameObject.activeSelf == false)
            {
                throw new InvalidOperationException(
                    $"{name}: ShowCoins requires a shown popup. Spawn the popup through UiSpawner first.");
            }

            _skinIcon.gameObject.SetActive(false);
            _coinIcon.gameObject.SetActive(true);

            ShowRarity(SkinRarity.Common, $"×{amount}");
        }

        private void ShowRarity(SkinRarity rarity, string label)
        {
            _plate.sprite = GetPlateSprite(rarity);
            _prizeFrame.sprite = GetFrameSprite(rarity);
            _plateText.text = label;
        }

        private Sprite GetPlateSprite(SkinRarity rarity)
        {
            switch (rarity)
            {
                case SkinRarity.Rare:
                    return _rarePlate;

                case SkinRarity.Epic:
                    return _epicPlate;

                case SkinRarity.Legendary:
                    return _legendaryPlate;

                default:
                    return _normalPlate;
            }
        }

        private Sprite GetFrameSprite(SkinRarity rarity)
        {
            switch (rarity)
            {
                case SkinRarity.Rare:
                    return _rareFrame;

                case SkinRarity.Epic:
                    return _epicFrame;

                case SkinRarity.Legendary:
                    return _legendaryFrame;

                default:
                    return _normalFrame;
            }
        }

        private void OnTakeClicked()
        {
            Hide();
        }

        private void HideAndDestroy()
        {
            Action closed = Closed;
            closed?.Invoke();

            Destroy(gameObject);
        }
    }
}
