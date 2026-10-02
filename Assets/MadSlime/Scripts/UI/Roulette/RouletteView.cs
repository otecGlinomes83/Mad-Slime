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

namespace Roulette
{
    public sealed class RouletteView : MonoBehaviour, IShowable
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
        [SerializeField, Tooltip("Иконка секретного сектора ежедневной рулетки: какой скин выпадет, игрок не видит до попапа.")]
        private Sprite _secretIcon;
        [SerializeField, Tooltip("Цвет плашки секретного сектора (одинаков для всех секторов со скином).")]
        private Color _secretColor = new Color(0.24f, 0.22f, 0.31f, 1f);
        [SerializeField] private RouletteWinPopup _winPopupPrefab;

        [SerializeField] private GameObject _screenRoot;
        [SerializeField] private Button _closeButton;

        private RouletteService _service;
        private AdScheduler _adScheduler;
        private SfxPlayer _sfxPlayer;
        private UiSpawner _uiSpawner;
        private Mode _mode;
        private List<SkinItem> _skinSource;
        private List<SkinItem> _sectorPool;
        private readonly List<SkinItem> _entrySkins = new List<SkinItem>();
        private readonly List<int> _entrySectorIndices = new List<int>();
        private bool _allSkinsCollected;
        private bool _isInitialized;
        private bool _lastFreeReady;
        private int _lastAdSpinsLeft = -1;
        private int _lastFreeRemainSeconds = -1;
        private int _lastSkinSpinCost = -1;
        private int _pendingIndex;

        public bool IsSpinning => _reel.IsSpinning;

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

            if (_reel == null)
            {
                throw new InvalidOperationException(
                    $"{name}: Reel is not assigned. Drag a RouletteReel into the _reel field.");
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

            _reel.Setup(_service.Config, _sfxPlayer);

            _service.PruneAdSpins(GetNowUnixTime());

            if (_mode == Mode.Skins && _adButton != null)
            {
                _adButton.gameObject.SetActive(false);
            }

            _spinButton.onClick.AddListener(OnSpinClicked);

            // В режиме Skins кнопка рекламы необязательна: гвард симметричен
            // проверке в OnDestroy.
            if (_adButton != null)
            {
                _adButton.onClick.AddListener(OnAdClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }

            BuildReel();
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
            if (_reel.IsSpinning == true)
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

        // Лента не должна работать на коротком списке: соседние карточки
        // повторяются и наезжают друг на друга. Поэтому список растягивается
        // до MinimumEntryCount, повторяя существующие записи по кругу, а
        // параллельные списки хранят, какая запись (сектор или скин) стоит
        // за каждой ячейкой ленты.
        private void BuildReel()
        {
            List<RouletteSectorView> views = new List<RouletteSectorView>();
            _entrySkins.Clear();
            _entrySectorIndices.Clear();

            if (_mode == Mode.Main)
            {
                IReadOnlyList<RouletteSector> sectors = _service.Config.Sectors;

                if (sectors.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"{name}: RouletteConfig '{_service.Config.name}' has no sectors.");
                }

                int entryCount = Mathf.Max(sectors.Count, _reel.MinimumEntryCount);

                for (int i = 0; i < entryCount; i++)
                {
                    RouletteSector sector = sectors[i % sectors.Count];
                    _entrySectorIndices.Add(i % sectors.Count);

                    if (sector.RewardType == RouletteSector.RewardKind.Coins)
                    {
                        views.Add(new RouletteSectorView($"×{sector.Coins}", null, CoinEntryColor));
                    }
                    else
                    {
                        // Скин-сектор показывается «секретом»: иконка и плашка
                        // одинаковые, выпавший скин раскрывается только в попапе.
                        views.Add(new RouletteSectorView(
                            Localization.Get("roulette_secret"),
                            _secretIcon,
                            _secretColor));
                    }
                }
            }
            else
            {
                List<SkinItem> pool = _service.CollectAvailableSkinPool(_skinSource);

                // Эксклюзивные (сверхредкие) скины выпадают только из денежной
                // рулетки в главном меню (дейли-ревард) — из скин-рулетки исключены.
                for (int i = pool.Count - 1; i >= 0; i--)
                {
                    if (IsExclusive(pool[i]) == true)
                    {
                        pool.RemoveAt(i);
                    }
                }

                _service.SortByRarityAscending(pool);
                _allSkinsCollected = pool.Count == 0;
                _sectorPool = new List<SkinItem>(pool);

                // Пустой пул: крутим вхолостую нельзя — показываем все скины,
                // но покупка (крутка за монеты) запрещена в RefreshButtons.
                List<SkinItem> source = pool.Count > 0 ? pool : AllSkinsSorted();

                if (source.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"{name}: no skins to show — the skin source list is empty. Check ShopContent.");
                }

                int entryCount = Mathf.Max(source.Count, _reel.MinimumEntryCount);

                for (int i = 0; i < entryCount; i++)
                {
                    SkinItem skin = source[i % source.Count];
                    _entrySkins.Add(skin);
                    views.Add(new RouletteSectorView(
                        Localization.Get(GetRarityKey(skin.Rarity)),
                        skin.Icon,
                        _service.GetRarityColor(skin.Rarity)));
                }
            }

            _reel.Build(views);
        }

        private List<SkinItem> AllSkinsSorted()
        {
            List<SkinItem> all = new List<SkinItem>(_skinSource);
            _service.SortByRarityAscending(all);

            return all;
        }

        private bool IsExclusive(SkinItem item)
        {
            IReadOnlyList<SkinItem> exclusives = _service.Config.ExclusiveSkins;

            for (int i = 0; i < exclusives.Count; i++)
            {
                if (exclusives[i] == item)
                {
                    return true;
                }
            }

            return false;
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

                // Дейли-рулетка крутится только бесплатно по кулдауну или за рекламу.
                if (_service.CanSpinFree(now) == true)
                {
                    _service.RegisterFreeSpin(now);
                    SpinReel();
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

            RefreshButtons();

            _reel.Spin(_pendingIndex, _reel.EntryCount, OnReelSpinCompleted);
        }

        private int PickTargetIndex()
        {
            if (_mode == Mode.Skins)
            {
                int poolIndex = _service.PickSkinIndex(_sectorPool);

                return _entrySkins.IndexOf(_sectorPool[poolIndex]);
            }

            int sectorIndex = _service.PickMainSectorIndex();

            return _entrySectorIndices.IndexOf(sectorIndex);
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
                    GrantCoinsAndShow(sector.Coins);
                    return;
                }

                if (_service.IsSkinOpen(sector.Skin) == true)
                {
                    GrantCoinsAndShow(_service.Config.DuplicateCoinsCompensation);
                    return;
                }

                _service.GrantSkin(sector.Skin);
                ShowSkinPopup(sector.Skin);
                return;
            }

            if (_allSkinsCollected == true)
            {
                GrantCoinsAndShow(_service.Config.DuplicateCoinsCompensation);
                return;
            }

            SkinItem skin = _entrySkins[targetIndex];
            _service.GrantSkin(skin);
            ShowSkinPopup(skin);
        }

        private void ShowSkinPopup(SkinItem skin)
        {
            RouletteWinPopup winPopup = CreateWinPopup();
            winPopup.ShowSkin(
                skin,
                skin.Rarity,
                _service.GetRarityColor(skin.Rarity));
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
            _reel.ReleaseHold();

            if (_mode == Mode.Skins && _allSkinsCollected == false)
            {
                BuildReel();
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
            bool spinning = _reel.IsSpinning;
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
                        // Пока бесплатная крутка на кулдауне — показываем таймер до неё.
                        _spinPriceText.text = $"{freeRemainSeconds / 60}:{freeRemainSeconds % 60:00}";
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
