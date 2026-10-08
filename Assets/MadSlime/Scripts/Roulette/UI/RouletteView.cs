using Audio;
using Game;
using Scriptables;
using Skins;
using System;
using System.Collections.Generic;
using TMPro;
using UI;
using UI.Animations;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Random = UnityEngine.Random;

namespace Roulette
{
    public sealed class RouletteView : MonoBehaviour, IShowable
    {
        public enum Mode
        {
            Main = 0,
            Skins
        }

        [SerializeField] private RouletteWheel _wheel;
        [SerializeField] private Button _spinButton;
        [SerializeField] private TMP_Text _spinPriceText;
        [SerializeField] private Button _adButton;
        [SerializeField] private TMP_Text _adButtonText;
        [SerializeField, Tooltip("Иконка секретного сектора ежедневной рулетки: какой скин выпадет, игрок не видит до попапа.")]
        private Sprite _secretIcon;
        [SerializeField, Tooltip("Иконка денежных секторов ежедневной рулетки: на секторе колеса и в попапе выигрыша.")]
        private Sprite _coinIcon;
        [SerializeField] private RouletteWinPopup _winPopupPrefab;

        [SerializeField] private GameObject _screenRoot;
        [SerializeField] private Button _closeButton;

        private RouletteService _service;
        private AdScheduler _adScheduler;
        private SfxPlayer _sfxPlayer;
        private UiSpawner _uiSpawner;
        private Mode _mode;
        private List<SkinItem> _skinSource;
        private readonly List<SkinItem> _entrySkins = new List<SkinItem>();
        private readonly List<int> _entrySectorIndices = new List<int>();
        private int _uniqueEntryCount;
        private bool _allSkinsCollected;
        private bool _isInitialized;
        private bool _lastFreeReady;
        private int _lastAdSpinsLeft = -1;
        private int _lastFreeRemainSeconds = -1;
        private int _lastSkinSpinCost = -1;
        private int _pendingIndex;

        public bool IsSpinning => _wheel.IsSpinning;

        public event Action Closed;

        [Inject]
        public void Construct(RouletteService service, AdScheduler adScheduler, SfxPlayer sfxPlayer,
            UiSpawner uiSpawner)
        {
            _service = service;
            _adScheduler = adScheduler;
            _sfxPlayer = sfxPlayer;
            _uiSpawner = uiSpawner;
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

            if (_uiSpawner == null)
            {
                throw new InvalidOperationException(
                    $"{name}: UiSpawner was not injected. Check that the scene LifetimeScope registers UiSpawner and RouletteView.");
            }

            if (_wheel == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Wheel is not assigned. Drag a RouletteWheel into the _wheel field.");
            }

            if (_spinButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SpinButton is not assigned. Drag the spin Button into the _spinButton field.");
            }

            if (_spinPriceText == null)
            {
                throw new InvalidOperationException(
                    $"{name}: SpinPrice is not assigned. Drag the price TMP_Text into the _spinPriceText field.");
            }

            if (_winPopupPrefab == null)
            {
                throw new InvalidOperationException(
                    $"{name}: WinPopup prefab is not assigned. Drag the RouletteWinPopup prefab into the _winPopupPrefab field.");
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

            if (_mode == Mode.Main && (_adButton == null || _adButtonText == null))
            {
                throw new InvalidOperationException(
                    $"{name}: the Main roulette needs the Ad button. Drag it and its text into _adButton/_adButtonText.");
            }

            if (_mode == Mode.Main && _coinIcon == null)
            {
                throw new InvalidOperationException(
                    $"{name}: the Main roulette needs the coin icon. Drag a coin sprite into _coinIcon.");
            }

            if (_mode == Mode.Main && _secretIcon == null)
            {
                throw new InvalidOperationException(
                    $"{name}: the Main roulette needs the secret icon. Drag a secret sprite into _secretIcon.");
            }

            _wheel.Setup(_service.Config, _sfxPlayer);

            _service.PruneAdSpins(GetNowUnixTime());

            if (_mode == Mode.Skins && _adButton != null)
            {
                _adButton.gameObject.SetActive(false);
            }

            _spinButton.onClick.AddListener(OnSpinClicked);

            if (_adButton != null)
            {
                _adButton.onClick.AddListener(OnAdClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }

            BuildWheel();
            RefreshButtons();

            _isInitialized = true;
        }

        public void Show()
        {
            if (_screenRoot == null)
            {
                throw new InvalidOperationException(
                    $"{name}: this RouletteView is not a screen. Assign the _screenRoot object to show it.");
            }

            Initialize(Mode.Main, null);
            _screenRoot.SetActive(true);
        }

        public void Hide()
        {
            if (_wheel.IsSpinning == true)
            {
                return;
            }

            UiAnimations.ScaleOut((RectTransform)_screenRoot.transform, UiAnimations.WindowScaleOutDuration, HideScreenRoot);
        }

        private void OnDestroy()
        {
            if (_isInitialized == false)
            {
                return;
            }

            _spinButton.onClick.RemoveListener(OnSpinClicked);

            if (_adButton != null)
            {
                _adButton.onClick.RemoveListener(OnAdClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
            }
        }

        private void Update()
        {
            if (_isInitialized == true)
            {
                RefreshButtons();
            }
        }

        private void OnCloseClicked()
        {
            Hide();
        }

        private void HideScreenRoot()
        {
            _screenRoot.SetActive(false);

            Action closed = Closed;
            closed?.Invoke();
        }

        private void BuildWheel()
        {
            List<RouletteSectorIcon> sectors = new List<RouletteSectorIcon>();
            _entrySkins.Clear();
            _entrySectorIndices.Clear();

            if (_mode == Mode.Main)
            {
                IReadOnlyList<RouletteSector> configSectors = _service.Config.Sectors;

                if (configSectors.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"{name}: RouletteConfig '{_service.Config.name}' has no sectors.");
                }

                int entryCount = Mathf.Max(configSectors.Count, _wheel.SectorCount);

                for (int i = 0; i < entryCount; i++)
                {
                    RouletteSector sector = configSectors[i % configSectors.Count];
                    _entrySectorIndices.Add(i % configSectors.Count);

                    if (sector.RewardType == RouletteSector.RewardKind.Coins)
                    {
                        sectors.Add(new RouletteSectorIcon(_coinIcon, $"×{sector.Coins}"));
                    }
                    else
                    {
                        sectors.Add(new RouletteSectorIcon(_secretIcon, null));
                    }
                }
            }
            else
            {
                _service.EnsureShowcaseFormed(_skinSource);

                List<SkinItem> showcase = _service.GetShowcaseSkins(_skinSource);

                _allSkinsCollected = showcase.Count == 0;

                List<SkinItem> source;

                if (_allSkinsCollected == true)
                {
                    source = AllSkinsSorted();
                }
                else
                {
                    if (showcase.Count > _wheel.SectorCount)
                    {
                        throw new InvalidOperationException(
                            $"{name}: showcase size {showcase.Count} exceeds the wheel slot count {_wheel.SectorCount}. " +
                            "Lower the showcase counts in RouletteConfig.");
                    }

                    _service.SortByRarityAscending(showcase);
                    source = showcase;
                }

                if (source.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"{name}: no skins to show — the skin source list is empty. Check ShopContent.");
                }

                FillSectorSkins(source);

                for (int i = 0; i < _entrySkins.Count; i++)
                {
                    sectors.Add(new RouletteSectorIcon(_entrySkins[i].Icon, null));
                }
            }

            _wheel.Build(sectors);
        }

        private void FillSectorSkins(List<SkinItem> source)
        {
            List<SkinItem> remaining = new List<SkinItem>(source);
            _uniqueEntryCount = Mathf.Min(source.Count, _wheel.SectorCount);

            while (_entrySkins.Count < _uniqueEntryCount)
            {
                int pickIndex = Random.Range(0, remaining.Count);

                _entrySkins.Add(remaining[pickIndex]);
                remaining.RemoveAt(pickIndex);
            }

            remaining.AddRange(_entrySkins);

            while (_entrySkins.Count < _wheel.SectorCount)
            {
                int pickIndex = Random.Range(0, remaining.Count);

                _entrySkins.Add(remaining[pickIndex]);
                remaining.RemoveAt(pickIndex);

                if (remaining.Count == 0)
                {
                    remaining.AddRange(source);
                }
            }
        }

        private List<SkinItem> AllSkinsSorted()
        {
            List<SkinItem> all = new List<SkinItem>(_skinSource);
            _service.SortByRarityAscending(all);

            return all;
        }

        private void OnSpinClicked()
        {
            if (_wheel.IsSpinning == true)
            {
                return;
            }

            if (_mode == Mode.Main)
            {
                long now = GetNowUnixTime();

                if (_service.CanSpinFree(now) == true)
                {
                    _service.RegisterFreeSpin(now);
                    SpinWheel();
                }

                return;
            }

            if (_allSkinsCollected == true)
            {
                return;
            }

            if (_service.CanSpinSkinsForCoins() == true)
            {
                _service.PaySkinSpin();
                SpinWheel();
            }
        }

        private void OnAdClicked()
        {
            if (_wheel.IsSpinning == true)
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

        private void SpinWheel()
        {
            _pendingIndex = PickTargetIndex();

            RefreshButtons();

            _wheel.Spin(_pendingIndex, OnWheelSpinCompleted);
        }

        private int PickTargetIndex()
        {
            if (_mode == Mode.Skins)
            {
                List<SkinItem> uniquePool = _entrySkins.GetRange(0, _uniqueEntryCount);

                return _service.PickSkinIndex(uniquePool);
            }

            int sectorIndex = _service.PickMainSectorIndex();

            return _entrySectorIndices.IndexOf(sectorIndex);
        }

        private void OnWheelSpinCompleted()
        {
            OnWheelStopped(_pendingIndex);
        }

        private void OnWheelStopped(int targetIndex)
        {
            PlayWinSound();

            if (_mode == Mode.Main)
            {
                RouletteSector sector = _service.Config.Sectors[_entrySectorIndices[targetIndex]];

                if (sector.RewardType == RouletteSector.RewardKind.Coins)
                {
                    GrantCoinsAndShow(sector.Coins);
                    return;
                }

                SkinItem hiddenSkin = _service.PickHiddenSkin();

                if (_service.IsSkinOpen(hiddenSkin) == true)
                {
                    GrantCoinsAndShow(_service.Config.DuplicateCoinsCompensation);
                    return;
                }

                _service.GrantSkin(hiddenSkin, _skinSource);
                ShowSkinPopup(hiddenSkin);
                return;
            }

            if (_allSkinsCollected == true)
            {
                GrantCoinsAndShow(_service.Config.DuplicateCoinsCompensation);
                return;
            }

            SkinItem skin = _entrySkins[targetIndex];
            _service.GrantSkin(skin, _skinSource);
            ShowSkinPopup(skin);
        }

        private void ShowSkinPopup(SkinItem skin)
        {
            RouletteWinPopup winPopup = CreateWinPopup();
            winPopup.ShowSkin(
                skin,
                skin.Rarity,
                Localization.Get(GetRarityKey(skin.Rarity)));
        }

        private RouletteWinPopup CreateWinPopup()
        {
            return _uiSpawner.Spawn(_winPopupPrefab, UiLayer.Popup, OnWinPopupClosed);
        }

        private void GrantCoinsAndShow(int amount)
        {
            _service.GrantCoins(amount);

            RouletteWinPopup winPopup = CreateWinPopup();
            winPopup.ShowCoins(amount);
        }

        private void OnWinPopupClosed()
        {
            _wheel.ReleaseHold();

            if (_mode == Mode.Skins && _allSkinsCollected == false)
            {
                BuildWheel();
            }

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

        private void RefreshButtons()
        {
            bool spinning = _wheel.IsSpinning;
            long now = GetNowUnixTime();

            if (_mode == Mode.Main)
            {
                bool freeReady = _service.CanSpinFree(now);
                int adSpinsLeft = _service.GetAdSpinsLeft(now);
                int freeRemainSeconds = _service.GetFreeSpinRemainSeconds(now);

                if (freeReady != _lastFreeReady
                    || adSpinsLeft != _lastAdSpinsLeft
                    || freeRemainSeconds != _lastFreeRemainSeconds)
                {
                    _lastFreeReady = freeReady;
                    _lastAdSpinsLeft = adSpinsLeft;
                    _lastFreeRemainSeconds = freeRemainSeconds;

                    if (freeReady == true)
                    {
                        _spinPriceText.text = Localization.Get("roulette_free_ready");
                    }
                    else
                    {
                        int minutes = freeRemainSeconds / 60;
                        int seconds = freeRemainSeconds % 60;
                        _spinPriceText.text = $"{minutes}:{seconds:00}";
                    }

                    _adButtonText.text = string.Format(
                        Localization.Get("roulette_ad_spins"),
                        adSpinsLeft);
                }

                _spinButton.interactable = spinning == false && freeReady == true;
                _adButton.interactable = spinning == false && _service.CanSpinForAd(now);

                return;
            }

            int spinCost = _service.GetSkinSpinCost();

            if (spinCost != _lastSkinSpinCost)
            {
                _lastSkinSpinCost = spinCost;
                _spinPriceText.text = $"{spinCost}";
            }

            _spinButton.interactable = spinning == false
                && _allSkinsCollected == false
                && _service.CanSpinSkinsForCoins();
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
