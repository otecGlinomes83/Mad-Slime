using System;
using System.Collections.Generic;
using System.Threading;
using Audio;
using Cysharp.Threading.Tasks;
using Game;
using Scriptables;
using Saves;
using UnityEngine;

namespace Roulette
{
    public class RoulettePresenter : MonoBehaviour
    {
        private const float ButtonRefreshInterval = 1f;

        [SerializeField] private RouletteView _view;

        private RouletteModel _model;
        private ISaveConfirmation _saveConfirmation;
        private AdScheduler _adScheduler;
        private RouletteSoundFeedback _sound;
        private IUISoundPlayer _soundPlayer;
        private RouletteOutcome _outcome;
        private CancellationTokenSource _presentationCancellation;
        private bool _isShown;
        private bool _isBusy;
        private bool _isWaitingForAd;
        private bool _isSubscribed;
        private bool _hasSaveFailed;
        private Action<RoulettePrize> _onComplete;
        private float _nextRefreshTime;

        public event Action CloseRequested;

        public event Action Closed;

        public RouletteView View => _view;

        public void Setup(RouletteModel model, AdScheduler adScheduler, IUISoundPlayer soundPlayer,
            ISaveConfirmation saveConfirmation)
        {
            Unsubscribe();
            _model = model;
            _saveConfirmation = saveConfirmation;
            _adScheduler = adScheduler;
            _soundPlayer = soundPlayer;
            _sound = new RouletteSoundFeedback();
            _view.Wheel.Setup(model.Config);
            _model.RefreshSlots();

            if (isActiveAndEnabled == true)
            {
                Subscribe();
            }
        }

        public int GetSpinPrice()
        {
            return _model.GetPrice();
        }

        public bool CanSpin()
        {
            return _isBusy == false && _model.CanSpin(false);
        }

        public bool HasAvailableSkin()
        {
            return _model.HasAvailableSkin();
        }

        public bool ShowAndSpin(Action<RoulettePrize> onComplete)
        {
            if (_isShown == true || CanSpin() == false)
            {
                return false;
            }

            Show(onComplete);
            Spin(false);
            return true;
        }

        public void Show(Action<RoulettePrize> onComplete)
        {
            if (_isShown == true)
            {
                return;
            }

            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }

            _onComplete = onComplete;
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

            OnViewDisabled();
            _view.Hide();
        }

        public void ResumeAfterPrize()
        {
            if (_isShown == false)
            {
                return;
            }

            _view.Wheel.Stop();
            _outcome = null;
            _isBusy = false;
            BuildWheel();
            RefreshButtons();
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
            OnViewDisabled();
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
            _view.CloseRequested += OnCloseRequested;
            _view.Closed += OnViewClosed;
            _view.Disabled += OnViewDisabled;
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
            _view.CloseRequested -= OnCloseRequested;
            _view.Closed -= OnViewClosed;
            _view.Disabled -= OnViewDisabled;
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

            _nextRefreshTime = Time.unscaledTime + ButtonRefreshInterval;
            RefreshButtons();
        }

        private void OnCloseRequested()
        {
            if (_isBusy == false)
            {
                CloseRequested?.Invoke();
            }
        }

        private void BuildWheel()
        {
            _model.RefreshSlots();
            List<RouletteSectorIcon> icons = new List<RouletteSectorIcon>();

            for (int i = 0; i < _model.Slots.Count; i++)
            {
                RouletteSlot slot = _model.Slots[i];
                Sprite icon;
                string label = null;

                if (slot.IsHidden == true)
                {
                    icon = _view.SecretIcon;
                }
                else if (slot.Skin != null)
                {
                    icon = slot.Skin.Icon;
                }
                else
                {
                    icon = _view.GetCoinIcon(slot.Coins);
                    label = $"×{slot.Coins}";
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

            if (_hasSaveFailed == true)
            {
                _hasSaveFailed = false;
                _isBusy = true;
                RefreshButtons();
                ConfirmAndSpinAsync(_presentationCancellation.Token).Forget();
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
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception)
            {
                _hasSaveFailed = true;
                _isBusy = false;
                RefreshButtons();
                return;
            }

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
            RoulettePrize prize = _outcome.Prize;
            Action<RoulettePrize> onComplete = _onComplete;
            onComplete?.Invoke(prize);
        }

        private void OnViewDisabled()
        {
            _isShown = false;
            _isBusy = false;
            _isWaitingForAd = false;
            _hasSaveFailed = false;
            _onComplete = null;
            _outcome = null;

            if (_presentationCancellation != null)
            {
                _presentationCancellation.Cancel();
                _presentationCancellation.Dispose();
                _presentationCancellation = null;
            }

            _view.Wheel.Stop();
        }

        private void OnViewClosed()
        {
            Closed?.Invoke();
        }

        private void RefreshButtons()
        {
            string title = Localization.Get("skin_roulette_title");
            string spinText = _model.GetPrice().ToString();

            if (_model.HasAdvertising == true)
            {
                title = Localization.Get("daily_roulette_title");
                int remaining = _model.GetFreeRemainSeconds();
                spinText = Localization.Get("roulette_free_ready");

                if (remaining > 0)
                {
                    spinText = $"{remaining / 60}:{remaining % 60:00}";
                }
            }

            if (_hasSaveFailed == true)
            {
                title = Localization.Get("roulette_save_error");
                spinText = Localization.Get("leaderboard_retry");
            }

            string adText = string.Format(Localization.Get("roulette_ad_spins"), _model.GetAdSpinsLeft());
            bool canSpin = _isBusy == false && (_hasSaveFailed == true || _model.CanSpin(false));
            bool showAd = _model.HasAdvertising == true && _hasSaveFailed == false;
            _view.Display(title, _model.HasAdvertising == true || _hasSaveFailed == true, spinText, canSpin,
                showAd, adText, _isBusy == false && _model.CanSpin(true), _model.HasAdvertising,
                _isBusy == false);
        }
    }
}
