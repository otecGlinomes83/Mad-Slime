using System;
using Game;
using Skins;
using UnityEngine;
using VContainer;
using YG;

namespace Player
{
    public sealed class SkinApplier : MonoBehaviour
    {
        [SerializeField] private ShopContent _shopContent;
        [SerializeField] private Transform _skinsContainer;

        private PlayerProgress _progress;
        private GameObject _currentModel;

        [Inject]
        public void Construct(PlayerProgress progress)
        {
            _progress = progress;
        }

        private void Awake()
        {
            if (_shopContent == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SkinApplier requires _shopContent to be assigned in the inspector.");
            }

            if (_skinsContainer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SkinApplier requires _skinsContainer to be assigned in the inspector.");
            }
        }

        private void OnEnable()
        {
            YG2.onGetSDKData += OnSavesLoaded;
        }

        private void Start()
        {
            ApplySelectedSkin();
        }

        private void OnDisable()
        {
            YG2.onGetSDKData -= OnSavesLoaded;
        }

        private void OnDestroy()
        {
            if (_currentModel != null)
            {
                Destroy(_currentModel);
            }
        }

        private void OnSavesLoaded()
        {
            ApplySelectedSkin();
        }

        private void ApplySelectedSkin()
        {
            if (_progress == null)
            {
                return;
            }

            PlayerSkins selectedType = _progress.SelectedSkin;

            SkinItem matchingItem = FindItem(selectedType);

            if (matchingItem == null)
            {
                return;
            }

            if (_currentModel != null)
            {
                Destroy(_currentModel);
            }

            _currentModel = Instantiate(matchingItem.Model, _skinsContainer);
        }

        private SkinItem FindItem(PlayerSkins skinType)
        {
            foreach (SkinItem item in _shopContent.SkinItems)
            {
                if (item == null)
                {
                    continue;
                }

                if (item.SkinType == skinType)
                {
                    return item;
                }
            }

            return null;
        }
    }
}
