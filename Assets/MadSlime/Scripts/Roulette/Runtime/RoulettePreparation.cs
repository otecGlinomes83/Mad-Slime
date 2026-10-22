using System;
using Audio;
using Game;
using Saves;
using Shop;
using UI;
using UnityEngine;
using VContainer;

namespace Roulette
{
    public class RoulettePreparation : MonoBehaviour
    {
        [SerializeField] private RouletteView _dailyRoulettePrefab;
        [SerializeField] private RouletteView _skinRoulettePrefab;

        private RouletteConfig _config;
        private ShopContent _content;
        private RouletteFactory _factory;
        private AdScheduler _ads;
        private IUISoundPlayer _sound;
        private ISaveConfirmation _saveConfirmation;
        private UiSpawner _spawner;
        private RouletteModel _dailyModel;

        private RouletteModel _skinModel;

        public RouletteModel SkinModel => _skinModel;

        [Inject]
        public void Construct(RouletteConfig config, ShopContent content, RouletteFactory factory,
            AdScheduler ads, IUISoundPlayer sound, ISaveConfirmation saveConfirmation, UiSpawner spawner)
        {
            _config = config;
            _content = content;
            _factory = factory;
            _ads = ads;
            _sound = sound;
            _saveConfirmation = saveConfirmation;
            _spawner = spawner;
        }

        public void Prepare()
        {
            if (_dailyRoulettePrefab == null || _skinRoulettePrefab == null)
            {
                throw new InvalidOperationException($"{name}: assign both roulette prefabs.");
            }

            int dailySectorCount = _dailyRoulettePrefab.Wheel.SectorCount;
            int skinSectorCount = _skinRoulettePrefab.Wheel.SectorCount;
            RouletteConfigValidator validator = new RouletteConfigValidator();
            validator.Validate(_config, _content.SkinItems, dailySectorCount, skinSectorCount);
            _dailyModel = _factory.CreateDaily(dailySectorCount);
            _skinModel = _factory.CreateSkins(_content.SkinItems, skinSectorCount);
            _dailyModel.RefreshSlots();
            SkinModel.RefreshSlots();
        }

        public RoulettePresenter SpawnDaily()
        {
            return Spawn(_dailyRoulettePrefab, _dailyModel);
        }

        public RoulettePresenter SpawnSkins()
        {
            return Spawn(_skinRoulettePrefab, SkinModel);
        }

        private RoulettePresenter Spawn(RouletteView prefab, RouletteModel model)
        {
            if (model == null)
            {
                throw new InvalidOperationException($"{name}: prepare roulette models before opening a popup.");
            }

            RouletteView view = _spawner.Spawn(prefab, UiLayer.Popup);
            if (view.TryGetComponent(out RoulettePresenter presenter) == false)
            {
                throw new InvalidOperationException($"{view.name}: RoulettePresenter is required.");
            }

            presenter.Setup(model, _ads, _sound, _saveConfirmation);
            return presenter;
        }
    }
}
