using Audio;
using Saves;
using System;
using Game;
using Roulette;
using Scriptables;
using Skins;
using UI;
using UnityEngine;
using Upgrades;
using VContainer;

namespace Shop
{
    public class ShopScenePresentation : MonoBehaviour
    {
        [SerializeField] private ShopPanel _panel;
        [SerializeField] private SkinsPagePresenter _skinsPage;
        [SerializeField] private UpgradesPagePresenter _upgradesPage;
        [SerializeField] private ShopBalanceView _balance;
        [SerializeField] private RoulettePresenter _roulette;
        [SerializeField] private ModelPlacer _preview;

        private ISaveConfirmation _saveConfirmation;
        private Action<Action> _transaction;
        private ShopSessionHandler _session;
        private SkinInventory _inventory;
        private ShopContent _content;
        private PlayerUpgrades _upgrades;
        private Wallet _wallet;
        private RouletteConfig _config;
        private RouletteFactory _factory;
        private AdScheduler _ads;
        private IUISoundPlayer _sound;
        private UiSpawner _spawner;

        [Inject]
        public void Construct(ShopSessionHandler session, SkinInventory inventory, ShopContent content,
            PlayerUpgrades upgrades, Wallet wallet, RouletteConfig config, RouletteFactory factory,
            AdScheduler ads, IUISoundPlayer sound, UiSpawner spawner, ISaveConfirmation saveConfirmation, Action<Action> transaction)
        {
            _session = session;
            _inventory = inventory;
            _content = content;
            _upgrades = upgrades;
            _wallet = wallet;
            _config = config;
            _factory = factory;
            _ads = ads;
            _sound = sound;
            _spawner = spawner;
            _saveConfirmation = saveConfirmation;
            _transaction = transaction;
        }

        private void OnEnable()
        {
            _session.Prepared += OnPrepared;
            _session.Started += OnStarted;
            _session.Finished += OnFinished;
            _panel.PageChanged += OnPageChanged;
        }

        private void OnDisable()
        {
            _session.Prepared -= OnPrepared;
            _session.Started -= OnStarted;
            _session.Finished -= OnFinished;
            _panel.PageChanged -= OnPageChanged;
            _roulette.Hide();
        }

        private void OnPrepared()
        {
            int sectorCount = _roulette.View.Wheel.SectorCount;
            new RouletteConfigValidator().Validate(_config, _content.SkinItems, sectorCount, sectorCount);
            _roulette.Setup(_factory.CreateSkins(_content.SkinItems, sectorCount), _ads, _sound, _spawner, _saveConfirmation);
            _skinsPage.Setup(_inventory, _content.SkinItems, _config.ExclusiveSkins, _config.RarityTable, _preview);
            _upgradesPage.Setup(_upgrades, _wallet, _transaction);
            _balance.Setup(_wallet);
            _panel.Setup();
        }

        private void OnStarted()
        {
            _panel.ShowRouletteTab();
        }

        private void OnFinished()
        {
            _roulette.Hide();
            _skinsPage.gameObject.SetActive(false);
            _upgradesPage.gameObject.SetActive(false);
            _panel.enabled = false;
            _balance.enabled = false;
            _preview.Clear();
        }

        private void OnPageChanged(int pageIndex)
        {
            if (pageIndex == 1)
            {
                _roulette.Show();
                return;
            }

            _roulette.Hide();
        }
    }
}
