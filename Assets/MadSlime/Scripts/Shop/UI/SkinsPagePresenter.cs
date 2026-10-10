using System;
using System.Collections.Generic;
using Skins;
using UnityEngine;

namespace Shop
{
    public class SkinsPagePresenter : MonoBehaviour
    {
        [SerializeField] private Transform _itemsParent;
        [SerializeField] private ShopItemViewFactory _factory;
        [SerializeField, Min(1)] private int _skinsPerRow = 3;

        private SkinInventory _inventory;
        private SkinRarityLookup _rarityLookup;
        private ModelPlacer _preview;
        private List<SkinItem> _skins = new List<SkinItem>();
        private List<SkinItem> _exclusiveSkins = new List<SkinItem>();
        private List<ShopItemView> _views = new List<ShopItemView>();
        private List<GameObject> _fillers = new List<GameObject>();
        private bool _isSetup;

        public void Setup(SkinInventory inventory, IEnumerable<SkinItem> skins, IEnumerable<SkinItem> exclusiveSkins,
            SkinRarityTable rarityTable, ModelPlacer preview)
        {
            Clear();
            _inventory = inventory;
            _rarityLookup = new SkinRarityLookup(rarityTable);
            _preview = preview;
            _skins.Clear();
            _exclusiveSkins.Clear();
            AddSkins(skins, false);
            AddSkins(exclusiveSkins, true);
            _skins.Sort(CompareRarity);
            _isSetup = true;

            if (isActiveAndEnabled == true)
            {
                Show();
            }
        }

        private int CompareRarity(SkinItem left, SkinItem right)
        {
            return left.Rarity.CompareTo(right.Rarity);
        }

        private void AddSkins(IEnumerable<SkinItem> skins, bool isExclusive)
        {
            foreach (SkinItem skin in skins)
            {
                if (skin == null || FindSkin(skin.Id) != null)
                {
                    continue;
                }

                _skins.Add(skin);

                if (isExclusive == true)
                {
                    _exclusiveSkins.Add(skin);
                }
            }
        }

        private void OnEnable()
        {
            if (_isSetup == true)
            {
                Show();
            }
        }

        private void OnDisable()
        {
            Clear();

            if (_preview != null)
            {
                _preview.Clear();
            }
        }

        private void Show()
        {
            if (_views.Count > 0)
            {
                return;
            }

            int index = 0;

            while (index < _skins.Count)
            {
                int end = index;
                SkinRarity rarity = _skins[index].Rarity;

                while (end < _skins.Count && _skins[end].Rarity == rarity)
                {
                    end++;
                }

                SpawnGroup(index, end, true);
                SpawnGroup(index, end, false);
                index = end;
            }

            int remainder = _views.Count % _skinsPerRow;

            if (remainder != 0)
            {
                for (int i = remainder; i < _skinsPerRow; i++)
                {
                    GameObject filler = new GameObject("RowFiller", typeof(RectTransform));
                    filler.transform.SetParent(_itemsParent, false);
                    _fillers.Add(filler);
                }
            }

            SkinItem equipped = FindSkin(_inventory.SelectedSkinId);

            if (equipped != null)
            {
                _preview.SetModel(equipped.Model);
            }
        }

        private void SpawnGroup(int start, int end, bool isOpen)
        {
            for (int i = start; i < end; i++)
            {
                SkinItem skin = _skins[i];

                if (_inventory.IsOpen(skin.Id) != isOpen)
                {
                    continue;
                }

                ShopItemView view = _factory.Get(_itemsParent);
                view.Initialize(skin.Id, skin.Icon, _rarityLookup.Get(skin.Rarity).PlateColor,
                    _exclusiveSkins.Contains(skin), isOpen, _inventory.SelectedSkinId == skin.Id);
                view.Click += OnClicked;
                _views.Add(view);
            }
        }

        private void OnClicked(ShopItemView view)
        {
            SkinItem skin = FindSkin(view.SkinId);
            _preview.SetModel(skin.Model);

            if (_inventory.Select(skin.Id) == false)
            {
                return;
            }

            foreach (ShopItemView item in _views)
            {
                item.SetSelected(item.SkinId == _inventory.SelectedSkinId);
            }
        }

        private SkinItem FindSkin(string skinId)
        {
            foreach (SkinItem skin in _skins)
            {
                if (skin.Id == skinId)
                {
                    return skin;
                }
            }

            return null;
        }

        private void Clear()
        {
            foreach (ShopItemView view in _views)
            {
                view.Click -= OnClicked;
                Destroy(view.gameObject);
            }

            _views.Clear();

            foreach (GameObject filler in _fillers)
            {
                Destroy(filler);
            }

            _fillers.Clear();
        }
    }
}
