using Game;
using Skins;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Roulette
{
    public sealed class RouletteView : MonoBehaviour
    {
        public enum Mode
        {
            Main = 0,
            Skins
        }

        [SerializeField] private RouletteWheel _wheel;
        [SerializeField] private Button _freeButton;
        [SerializeField] private Button _adButton;
        [SerializeField] private Button _coinsButton;
        [SerializeField] private TMP_Text _freeTimerText;
        [SerializeField] private TMP_Text _adSpinsText;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private TMP_Text _resultText;

        [SerializeField] private GameObject _screenRoot;
        [SerializeField] private Button _closeButton;

        private RouletteService _service;
        private AdScheduler _adScheduler;
        private Mode _mode;
        private List<SkinItem> _skinSource;
        private List<SkinItem> _sectorPool;
        private bool _allSkinsCollected;
        private bool _isInitialized;
        private int _lastFreeTimerKey = int.MinValue;
        private int _pendingIndex;

        public bool IsSpinning => _wheel.IsSpinning;

        [Inject]
        public void Construct(RouletteService service, AdScheduler adScheduler)
        {
            _service = service;
            _adScheduler = adScheduler;
        }

        public void Initialize(Mode mode, IEnumerable<SkinItem> skinSource)
        {
            if (_isInitialized == true)
            {
                return;
            }

            if (_service == null)
            {
                throw new InvalidOperationException(
                    $"{name}: RouletteService was not injected. Check that the scene LifetimeScope registers RouletteView.");
            }

            if (_adScheduler == null)
            {
                throw new InvalidOperationException(
                    $"{name}: AdScheduler was not injected. Check that the scene LifetimeScope registers RouletteView.");
            }

            if (_wheel == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Wheel is not assigned. Drag a RouletteWheel into the _wheel field.");
            }

            if (_freeButton == null || _adButton == null || _coinsButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a button is not assigned. Drag the Free, Ad and Coins buttons into the fields.");
            }

            if (_freeTimerText == null || _adSpinsText == null || _costText == null || _resultText == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a text is not assigned. Drag the FreeTimer, AdSpins, Cost and Result texts into the fields.");
            }

            _mode = mode;
            _skinSource = new List<SkinItem>();

            if (skinSource != null)
            {
                foreach (SkinItem item in skinSource)
                {
                    _skinSource.Add(item);
                }
            }

            _wheel.Setup(_service.Config);
            _resultText.gameObject.SetActive(false);
            _lastFreeTimerKey = int.MinValue;

            BuildWheel();
            RefreshButtons();

            _isInitialized = true;
        }

        public void Open()
        {
            if (_screenRoot == null)
            {
                throw new InvalidOperationException(
                    $"{name}: this RouletteView is not a screen. Assign the _screenRoot object to open it.");
            }

            Initialize(Mode.Main, null);
            _screenRoot.SetActive(true);
        }

        private void OnEnable()
        {
            _freeButton.onClick.AddListener(OnFreeClicked);
            _adButton.onClick.AddListener(OnAdClicked);
            _coinsButton.onClick.AddListener(OnCoinsClicked);

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }

            if (_isInitialized == true)
            {
                _resultText.gameObject.SetActive(false);

                if (_mode == Mode.Main)
                {
                    TryAutoSpinFree();
                }
            }
        }

        private void OnDisable()
        {
            _freeButton.onClick.RemoveListener(OnFreeClicked);
            _adButton.onClick.RemoveListener(OnAdClicked);
            _coinsButton.onClick.RemoveListener(OnCoinsClicked);

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
            }
        }

        private void Update()
        {
            if (_isInitialized == true && _mode == Mode.Main)
            {
                RefreshFreeTimerText();
            }
        }

        private void OnCloseClicked()
        {
            if (_wheel.IsSpinning == true)
            {
                return;
            }

            _screenRoot.SetActive(false);
        }

        private void TryAutoSpinFree()
        {
            if (_wheel.IsSpinning == true)
            {
                return;
            }

            long now = GetNowUnixTime();

            if (_service.CanSpinFree(now) == false)
            {
                return;
            }

            _service.RegisterFreeSpin(now);
            SpinWheel();
        }

        private void BuildWheel()
        {
            List<RouletteSectorView> views = new List<RouletteSectorView>();

            if (_mode == Mode.Main)
            {
                foreach (RouletteSector sector in _service.Config.Sectors)
                {
                    if (sector.RewardType == RouletteSector.RewardKind.Coins)
                    {
                        views.Add(new RouletteSectorView($"×{sector.Coins}", null, new Color(1f, 0.8f, 0.3f)));
                    }
                    else
                    {
                        views.Add(new RouletteSectorView(string.Empty, sector.Skin.Icon, new Color(0.7f, 0.4f, 1f)));
                    }
                }
            }
            else
            {
                List<SkinItem> pool = _service.CollectAvailableSkinPool(_skinSource);
                _allSkinsCollected = pool.Count == 0;
                _sectorPool = new List<SkinItem>(pool);

                if (_allSkinsCollected == true)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        views.Add(new RouletteSectorView(
                            $"×{_service.Config.DuplicateCoinsCompensation}",
                            null,
                            new Color(1f, 0.8f, 0.3f)));
                    }
                }
                else
                {
                    for (int i = 0; i < pool.Count; i++)
                    {
                        views.Add(new RouletteSectorView(string.Empty, pool[i].Icon, Color.white));
                    }
                }
            }

            _wheel.Build(views);
        }

        private void OnFreeClicked()
        {
            if (CanSpin() == false)
            {
                return;
            }

            long now = GetNowUnixTime();

            if (_service.CanSpinFree(now) == false)
            {
                return;
            }

            _service.RegisterFreeSpin(now);
            SpinWheel();
        }

        private void OnAdClicked()
        {
            if (CanSpin() == false)
            {
                return;
            }

            long now = GetNowUnixTime();

            if (_service.CanSpinForAd(now) == false)
            {
                return;
            }

            _adScheduler.ShowRewarded(
                _adScheduler.RouletteRewardId,
                OnAdSpinGranted,
                null);
        }

        private void OnAdSpinGranted()
        {
            _service.RegisterAdSpin(GetNowUnixTime());
            RefreshButtons();
            SpinWheel();
        }

        private void OnCoinsClicked()
        {
            if (CanSpin() == false)
            {
                return;
            }

            if (_mode == Mode.Main)
            {
                if (_service.CanSpinForCoins() == false)
                {
                    return;
                }

                _service.PayMainSpin();
            }
            else
            {
                if (_service.CanSpinSkinsForCoins() == false)
                {
                    return;
                }

                _service.PaySkinSpin();
            }

            SpinWheel();
        }

        private bool CanSpin()
        {
            return _isInitialized && _wheel.IsSpinning == false;
        }

        private void SpinWheel()
        {
            int sectorCount = _wheel.SectorCount;

            _pendingIndex = PickWeightedIndex(sectorCount);

            _resultText.gameObject.SetActive(false);
            RefreshButtons();

            _wheel.Spin(_pendingIndex, sectorCount, OnWheelSpinCompleted);
        }

        private void OnWheelSpinCompleted()
        {
            OnWheelStopped(_pendingIndex);
        }

        private int PickWeightedIndex(int sectorCount)
        {
            if (_mode == Mode.Skins)
            {
                return UnityEngine.Random.Range(0, sectorCount);
            }

            IReadOnlyList<RouletteSector> sectors = _service.Config.Sectors;
            float totalWeight = 0f;

            for (int i = 0; i < sectors.Count; i++)
            {
                totalWeight += sectors[i].Weight;
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);

            for (int i = 0; i < sectors.Count; i++)
            {
                roll -= sectors[i].Weight;

                if (roll <= 0f)
                {
                    return i;
                }
            }

            return sectors.Count - 1;
        }

        private void OnWheelStopped(int targetIndex)
        {
            if (_mode == Mode.Main)
            {
                RouletteSector sector = _service.Config.Sectors[targetIndex];

                if (sector.RewardType == RouletteSector.RewardKind.Coins)
                {
                    _service.GrantCoins(sector.Coins);
                    ShowResult(string.Format(Localization.Get("roulette_won_coins"), sector.Coins));
                }
                else if (_service.IsSkinOpen(sector.Skin) == true)
                {
                    _service.GrantCoins(_service.Config.DuplicateCoinsCompensation);
                    ShowResult(string.Format(
                        Localization.Get("roulette_won_coins"),
                        _service.Config.DuplicateCoinsCompensation));
                }
                else
                {
                    _service.GrantSkin(sector.Skin);
                    ShowResult(Localization.Get("roulette_won_skin"));
                }

                RefreshButtons();
                return;
            }

            if (_allSkinsCollected == true)
            {
                _service.GrantCoins(_service.Config.DuplicateCoinsCompensation);
                ShowResult(string.Format(
                    Localization.Get("roulette_won_coins"),
                    _service.Config.DuplicateCoinsCompensation));
                RefreshButtons();
                return;
            }

            _service.GrantSkin(_sectorPool[targetIndex]);
            ShowResult(Localization.Get("roulette_won_skin"));

            BuildWheel();
            RefreshButtons();
        }

        private void ShowResult(string message)
        {
            _resultText.text = message;
            _resultText.gameObject.SetActive(true);
        }

        private void RefreshButtons()
        {
            bool spinning = _wheel.IsSpinning;
            long now = GetNowUnixTime();

            if (_mode == Mode.Main)
            {
                _freeButton.interactable = spinning == false && _service.CanSpinFree(now);
                _adButton.interactable = spinning == false && _service.CanSpinForAd(now);

                _adSpinsText.text = string.Format(
                    Localization.Get("roulette_ad_spins"),
                    _service.GetAdSpinsLeft(now));

                _costText.text = $"{_service.MainSpinCost}";
                _coinsButton.interactable = spinning == false && _service.CanSpinForCoins();

                RefreshFreeTimerText();

                return;
            }

            _freeButton.gameObject.SetActive(false);
            _adButton.gameObject.SetActive(false);
            _freeTimerText.gameObject.SetActive(false);
            _adSpinsText.gameObject.SetActive(false);

            _costText.text = $"{_service.GetSkinSpinCost()}";
            _coinsButton.interactable = spinning == false && _service.CanSpinSkinsForCoins();
        }

        private void RefreshFreeTimerText()
        {
            long now = GetNowUnixTime();
            bool canSpinFree = _service.CanSpinFree(now);
            int remainSeconds = canSpinFree == true ? -1 : _service.GetFreeSpinRemainSeconds(now);

            if (remainSeconds == _lastFreeTimerKey)
            {
                return;
            }

            _lastFreeTimerKey = remainSeconds;

            if (canSpinFree == true)
            {
                _freeTimerText.text = Localization.Get("roulette_free_ready");
                return;
            }

            TimeSpan remain = TimeSpan.FromSeconds(remainSeconds);

            _freeTimerText.text = string.Format(
                Localization.Get("roulette_free_timer"),
                $"{(int)remain.TotalMinutes}:{remain.Seconds:00}");
        }

        private static long GetNowUnixTime()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
