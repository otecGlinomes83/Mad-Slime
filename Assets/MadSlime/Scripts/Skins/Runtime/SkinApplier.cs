using System;
using Game;
using Saves;
using Shop;
using Skins;
using UnityEngine;
using VContainer;

namespace Player
{
    public class SkinApplier : MonoBehaviour
    {
        [SerializeField] private ShopContent _shopContent;
        [SerializeField] private Transform _skinsContainer;

        private ISkinStorage _skinStorage;
        private ISavesReadiness _savesReadiness;
        private GameObject _currentModel;

        [Inject]
        public void Construct(ISkinStorage skinStorage, ISavesReadiness savesReadiness)
        {
            _skinStorage = skinStorage;
            _savesReadiness = savesReadiness;
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
            _savesReadiness.Ready += OnSavesLoaded;
        }

        private void Start()
        {
            ApplySelectedSkin();
        }

        private void OnDisable()
        {
            _savesReadiness.Ready -= OnSavesLoaded;
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
            if (_savesReadiness.IsReady == false)
            {
                return;
            }

            string selectedId = _skinStorage.SelectedSkinId;

            SkinItem matchingItem = FindItem(selectedId);

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

        private SkinItem FindItem(string skinId)
        {
            foreach (SkinItem item in _shopContent.SkinItems)
            {
                if (item == null)
                {
                    continue;
                }

                if (item.Id == skinId)
                {
                    return item;
                }
            }

            return null;
        }
    }
}
