using Audio;
using Game;
using Skins;
using System;
using System.Collections.Generic;
using Scriptables;
using TMPro;
using UI.Animations;
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

        private static readonly Color CoinEntryColor = new Color(1f, 0.8f, 0.3f);

        [SerializeField] private RouletteReel _reel;
        [SerializeField] private Button _spinButton;
        [SerializeField] private TMP_Text _spinPriceText;
        [SerializeField] private Button _adButton;
        [SerializeField] private TMP_Text _adButtonText;
        [SerializeField] private TMP_Text _resultText;

        [SerializeField] private GameObject _screenRoot;
        [SerializeField] private Button _closeButton;

        private RouletteService _service;
        private AdScheduler _adScheduler;
        private SfxPlayer _sfxPlayer;
        private Mode _mode;
        private List<SkinItem> _skinSource;
        private List<SkinItem> _sectorPool;
        private bool _allSkinsCollected;
        private bool _isInitialized;
        private bool _lastFreeReady;
        private int _lastAdSpinsLeft = -1;
        private int _pendingIndex;

        public bool IsSpinning => _reel.IsSpinning;

        [Inject]
        public void Construct(RouletteService service, AdScheduler adScheduler, SfxPlayer sfxPlayer)
        {
            _service = service;
            _adScheduler = adScheduler;
            _sfxPlayer = sfxPlayer;
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

            if (_sfxPlayer == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SfxPlayer was not injected. Check that ProjectLifetimeScope registers SfxPlayer.");
            }

            if (_reel == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Reel is not assigned. Drag a RouletteReel into the _reel field.");
            }

            if (_spinButton == null || _adButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a button is not assigned. Drag the Spin and Ad buttons into the fields.");
            }

            if (_spinPriceText == null || _adButtonText == null || _resultText == null)
            {
                throw new InvalidOperationException(
                    $"{name}: a text is not assigned. Drag the SpinPrice, AdText and Result texts into the fields.");
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

            _reel.Setup(_service.Config, _sfxPlayer);
            _resultText.gameObject.SetActive(false);

            if (_mode == Mode.Skins)
            {
                _adButton.gameObject.SetActive(false);
            }

            _spinButton.onClick.AddListener(OnSpinClicked);
            _adButton.onClick.AddListener(OnAdClicked);

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }

            BuildReel();
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

        private void OnDestroy()
        {
            if (_isInitialized == false)
            {
                return;
            }

            _spinButton.onClick.RemoveListener(OnSpinClicked);
            _adButton.onClick.RemoveListener(OnAdClicked);

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
            }
        }

        private void Update()
        {
            if (_isInitialized == true && _mode == Mode.Main)
            {
                RefreshButtons();
            }
        }

        private void OnCloseClicked()
        {
            if (_reel.IsSpinning == true)
            {
                return;
            }

            UiAnimations.ScaleOut((RectTransform)_screenRoot.transform, UiAnimations.WindowScaleOutDuration, HideScreenRoot);
        }

        private void HideScreenRoot()
        {
            _screenRoot.SetActive(false);
        }

        private void BuildReel()
        {
            List<RouletteSectorView> views = new List<RouletteSectorView>();

            if (_mode == Mode.Main)
            {
                foreach (RouletteSector sector in _service.Config.Sectors)
                {
                    if (sector.RewardType == RouletteSector.RewardKind.Coins)
                    {
                        views.Add(new RouletteSectorView($"×{sector.Coins}", null, CoinEntryColor));
                    }
                    else
                    {
                        views.Add(new RouletteSectorView(
                            Localization.Get(GetRarityKey(sector.Skin.Rarity)),
                            sector.Skin.Icon,
                            _service.GetRarityColor(sector.Skin.Rarity)));
                    }
                }
            }
            else
            {
                List<SkinItem> pool = _service.CollectAvailableSkinPool(_skinSource);
                _service.SortByRarityAscending(pool);
                _allSkinsCollected = pool.Count == 0;
                _sectorPool = new List<SkinItem>(pool);

                if (_allSkinsCollected == true)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        views.Add(new RouletteSectorView(
                            $"×{_service.Config.DuplicateCoinsCompensation}",
                            null,
                            CoinEntryColor));
                    }
                }
                else
                {
                    for (int i = 0; i < pool.Count; i++)
                    {
                        views.Add(new RouletteSectorView(
                            Localization.Get(GetRarityKey(pool[i].Rarity)),
                            pool[i].Icon,
                            _service.GetRarityColor(pool[i].Rarity)));
                    }
                }
            }

            _reel.Build(views);
        }

        private void OnSpinClicked()
        {
            if (_reel.IsSpinning == true)
            {
                return;
            }

            if (_mode == Mode.Main)
            {
                long now = GetNowUnixTime();

                if (_service.CanSpinFree(now) == true)
                {
                    _service.RegisterFreeSpin(now);
                    SpinReel();
                    return;
                }

                if (_service.CanSpinForCoins() == true)
                {
                    _service.PayMainSpin();
                    SpinReel();
                }

                return;
            }

            if (_service.CanSpinSkinsForCoins() == true)
            {
                _service.PaySkinSpin();
                SpinReel();
            }
        }

        private void OnAdClicked()
        {
            if (_reel.IsSpinning == true)
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
            SpinReel();
        }

        private void SpinReel()
        {
            _pendingIndex = PickTargetIndex();

            _resultText.gameObject.SetActive(false);
            RefreshButtons();

            _reel.Spin(_pendingIndex, _reel.EntryCount, OnReelSpinCompleted);
        }

        private int PickTargetIndex()
        {
            if (_mode == Mode.Skins)
            {
                return _service.PickSkinIndex(_sectorPool);
            }

            return _service.PickMainSectorIndex();
        }

        private void OnReelSpinCompleted()
        {
            OnReelStopped(_pendingIndex);
        }

        private void OnReelStopped(int targetIndex)
        {
            PlayWinSound();

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

            BuildReel();
            RefreshButtons();
        }

        private void PlayWinSound()
        {
            SfxClip winClip = _service.Config.WinClip;

            if (winClip == null)
            {
                return;
            }

            _sfxPlayer.PlayUi(winClip);
        }

        private void ShowResult(string message)
        {
            _resultText.text = message;
            _resultText.gameObject.SetActive(true);
        }

        private void RefreshButtons()
        {
            bool spinning = _reel.IsSpinning;
            long now = GetNowUnixTime();

            if (_mode == Mode.Main)
            {
                bool freeReady = _service.CanSpinFree(now);
                int adSpinsLeft = _service.GetAdSpinsLeft(now);

                if (freeReady != _lastFreeReady || adSpinsLeft != _lastAdSpinsLeft)
                {
                    _lastFreeReady = freeReady;
                    _lastAdSpinsLeft = adSpinsLeft;

                    if (freeReady == true)
                    {
                        _spinPriceText.text = Localization.Get("roulette_free_ready");
                    }
                    else
                    {
                        _spinPriceText.text = $"{_service.MainSpinCost}";
                    }

                    _adButtonText.text = string.Format(
                        Localization.Get("roulette_ad_spins"),
                        adSpinsLeft);
                }

                _spinButton.interactable = spinning == false
                    && (freeReady == true || _service.CanSpinForCoins() == true);
                _adButton.interactable = spinning == false && _service.CanSpinForAd(now);

                return;
            }

            _spinPriceText.text = $"{_service.GetSkinSpinCost()}";
            _spinButton.interactable = spinning == false && _service.CanSpinSkinsForCoins();
        }

        private static string GetRarityKey(SkinRarity rarity)
        {
            switch (rarity)
            {
                case SkinRarity.Rare:
                    return "rarity_rare";

                case SkinRarity.Epic:
                    return "rarity_epic";

                case SkinRarity.Legendary:
                    return "rarity_legendary";

                default:
                    return "rarity_common";
            }
        }

        private static long GetNowUnixTime()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
