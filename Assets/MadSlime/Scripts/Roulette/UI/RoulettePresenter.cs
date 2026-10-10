using System;
using System.Collections.Generic;
using System.Threading;
using Audio;
using Cysharp.Threading.Tasks;
using Game;
using Scriptables;
using Saves;
using Skins;
using UI;
using UnityEngine;

namespace Roulette
{
    public class RoulettePresenter : MonoBehaviour
    {
        [SerializeField] private RouletteView _view;

        private RouletteModel _model;
        private ISaveConfirmation _saveConfirmation;
        private AdScheduler _adScheduler;
        private UiSpawner _spawner;
        private RouletteSoundFeedback _sound;
        private IUISoundPlayer _soundPlayer;
        private RouletteOutcome _outcome;
        private RouletteWinPopup _popup;
        private CancellationTokenSource _presentationCancellation;
        private bool _isShown;
        private bool _isBusy;
        private bool _isWaitingForAd;
        private bool _isSubscribed;
        private float _nextRefreshTime;

        public event Action Closed;

        public RouletteView View => _view;

        public void Setup(RouletteModel model, AdScheduler adScheduler, IUISoundPlayer soundPlayer, UiSpawner spawner,
            ISaveConfirmation saveConfirmation)
        {
            Unsubscribe();
            _model = model;
            _saveConfirmation = saveConfirmation;
            _adScheduler = adScheduler;
            _soundPlayer = soundPlayer;
            _spawner = spawner;
            _sound = new RouletteSoundFeedback();
            _view.Wheel.Setup(model.Config);
            Subscribe();
        }

        public void Show()
        {
            if (_isShown == true)
            {
                return;
            }

            Subscribe();
            _isShown = true;
            _presentationCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            BuildWheel();
            _view.Show();
            RefreshButtons();
        }

        public void Hide()
        {
            if (_isShown == false)
            {
                return;
            }

            StopPresentation();
            _view.Hide();
        }

        private void OnEnable()
        {
            if (_model != null)
            {
                Subscribe();
            }
        }

        private void OnDisable()
        {
            StopPresentation();
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_isSubscribed == true)
            {
                return;
            }

            _view.SpinRequested += OnSpinRequested;
            _view.AdRequested += OnAdRequested;
            _view.CloseRequested += Hide;
            _view.Closed += OnViewClosed;
            _view.Disabled += StopPresentation;
            _view.Wheel.Completed += OnWheelCompleted;
            _sound.Setup(_view.Wheel, _model.Config, _soundPlayer);
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (_isSubscribed == false)
            {
                return;
            }

            _view.SpinRequested -= OnSpinRequested;
            _view.AdRequested -= OnAdRequested;
            _view.CloseRequested -= Hide;
            _view.Closed -= OnViewClosed;
            _view.Disabled -= StopPresentation;
            _view.Wheel.Completed -= OnWheelCompleted;
            _sound.Release();
            _isSubscribed = false;
        }

        private void Update()
        {
            if (_isShown == false || Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime = Time.unscaledTime + 1f;
            RefreshButtons();
        }

        private void BuildWheel()
        {
            _model.RefreshSlots();
            List<RouletteSectorIcon> icons = new List<RouletteSectorIcon>();

            for (int i = 0; i < _model.Slots.Count; i++)
            {
                RouletteSlot slot = _model.Slots[i];
                Sprite icon = _view.CoinIcon;
                string label = $"×{slot.Coins}";

                if (slot.IsHidden == true)
                {
                    icon = _view.SecretIcon;
                    label = null;
                }
                else if (slot.Skin != null)
                {
                    icon = slot.Skin.Icon;
                    label = null;
                }

                icons.Add(new RouletteSectorIcon(icon, label));
            }

            _view.Wheel.Build(icons);
        }

        private void OnSpinRequested()
        {
            if (_isShown == false || _isBusy == true)
            {
                return;
            }

            Spin(false);
        }

        private void OnAdRequested()
        {
            if (_isShown == false || _isBusy == true || _model.HasAdvertising == false
                || _model.CanSpin(true) == false)
            {
                return;
            }

            _isBusy = true;
            _isWaitingForAd = true;
            RefreshButtons();
            _adScheduler.ShowRewarded(_adScheduler.RouletteRewardId, OnAdGranted,
                OnAdNotGranted, OnAdNotGranted, OnAdNotGranted);
        }

        private void OnAdGranted()
        {
            if (_isShown == false || _isWaitingForAd == false)
            {
                return;
            }

            _isWaitingForAd = false;
            Spin(true);
        }

        private void OnAdNotGranted()
        {
            if (_isWaitingForAd == false)
            {
                return;
            }

            _isWaitingForAd = false;
            _isBusy = false;
            RefreshButtons();
        }

        private void Spin(bool advertisementGranted)
        {
            if (_model.Spin(advertisementGranted, out _outcome) == false)
            {
                _isBusy = false;
                RefreshButtons();
                return;
            }

            _isBusy = true;
            RefreshButtons();
            ConfirmAndSpinAsync(_presentationCancellation.Token).Forget();
        }

        private async UniTaskVoid ConfirmAndSpinAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _saveConfirmation.ConfirmSavedAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            _sound.PlaySpin();
            _view.Wheel.Spin(_outcome.SlotIndex);
        }

        private void OnWheelCompleted()
        {
            ShowPrizeAsync(_presentationCancellation.Token).Forget();
        }

        private async UniTaskVoid ShowPrizeAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_model.Config.WinDwellSeconds),
                    DelayType.Realtime, PlayerLoopTiming.Update, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _sound.PlayWin();
            _popup = _spawner.Spawn(_view.WinPopupPrefab, UiLayer.Popup);
            _popup.CloseRequested += OnPopupCloseRequested;
            _popup.Closed += OnPopupClosed;
            RoulettePrize prize = _outcome.Prize;

            if (prize.Skin != null)
            {
                _popup.InitializeSkin(prize.Skin, prize.Skin.Rarity, Localization.Get(GetRarityKey(prize.Skin.Rarity)));
            }
            else
            {
                _popup.InitializeCoins(prize.Coins);
            }

            _spawner.Show(_popup);
        }

        private void OnPopupCloseRequested()
        {
            _popup.BeginClose();
        }

        private void OnPopupClosed()
        {
            ReleasePopup();
            _view.Wheel.ReleaseHold();
            _outcome = null;
            _isBusy = false;
            BuildWheel();
            RefreshButtons();
        }

        private void ReleasePopup()
        {
            if (_popup == null)
            {
                return;
            }

            _popup.CloseRequested -= OnPopupCloseRequested;
            _popup.Closed -= OnPopupClosed;
            _spawner.Release(_popup);
            _popup = null;
        }

        private void StopPresentation()
        {
            _isShown = false;
            _isBusy = false;
            _isWaitingForAd = false;
            _outcome = null;

            if (_presentationCancellation != null)
            {
                _presentationCancellation.Cancel();
                _presentationCancellation.Dispose();
                _presentationCancellation = null;
            }

            ReleasePopup();
            _view.Wheel.Stop();
        }

        private void OnViewClosed()
        {
            Closed?.Invoke();
        }

        private void RefreshButtons()
        {
            string spinText = _model.GetPrice().ToString();

            if (_model.HasAdvertising == true)
            {
                int remaining = _model.GetFreeRemainSeconds();
                spinText = Localization.Get("roulette_free_ready");

                if (remaining > 0)
                {
                    spinText = $"{remaining / 60}:{remaining % 60:00}";
                }
            }

            string adText = string.Format(Localization.Get("roulette_ad_spins"), _model.GetAdSpinsLeft());
            _view.Display(spinText, _isBusy == false && _model.CanSpin(false), _model.HasAdvertising,
                adText, _isBusy == false && _model.CanSpin(true));
        }

        private string GetRarityKey(SkinRarity rarity)
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
    }
}
