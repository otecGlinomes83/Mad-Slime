using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class WinMenu : BaseWindow
    {
        [SerializeField] private TMP_Text _moneyCount;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _doubleRewardButton;
        [SerializeField] private Button _menuButton;

        [Tooltip("Длительность отсчёта показанной награды от предыдущей суммы до итоговой (с).")]
        [SerializeField, Min(0.01f)] private float _countUpDuration = 0.8f;

        public event Action NextLevelRequested;

        public event Action DoubleRewardRequested;

        public event Action MenuRequested;

        private int _moneyCountShown;
        private CancellationTokenSource _countUpCancellation;
        private bool _isNavigationRequested;

        protected override void Awake()
        {
            base.Awake();

            if (_moneyCount == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MoneyCount is not assigned. Drag a TMP_Text into the _moneyCount field.");
            }

            if (_nextLevelButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: NextLevelButton is not assigned. Drag a Button into the _nextLevelButton field.");
            }

            if (_menuButton == null)
            {
                throw new InvalidOperationException(
                    $"{name}: MenuButton is not assigned. Drag a Button into the _menuButton field.");
            }
        }

        public void Initialize(int moneyCount, int previousMoneyCount, bool canDoubleReward)
        {
            CancelCountUp();
            _isNavigationRequested = false;
            _nextLevelButton.interactable = true;
            _menuButton.interactable = true;
            _doubleRewardButton.interactable = canDoubleReward;
            _moneyCountShown = previousMoneyCount;
            _moneyCount.text = $"{previousMoneyCount}";

            _doubleRewardButton.gameObject.SetActive(canDoubleReward);
        }

        public void PlayRewardCountUp(int targetAmount)
        {
            CancelCountUp();

            if (IsClosing == true)
            {
                return;
            }
            if (targetAmount <= _moneyCountShown)
            {
                _moneyCountShown = targetAmount;
                _moneyCount.text = $"{targetAmount}";
                return;
            }

            _countUpCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            CountUpMoneyAsync(_moneyCountShown, targetAmount, _countUpCancellation.Token).Forget();
        }

        private async UniTaskVoid CountUpMoneyAsync(int fromAmount, int toAmount, CancellationToken cancellationToken)
        {
            float elapsedTime = 0f;

            try
            {
                while (elapsedTime < _countUpDuration)
                {
                    elapsedTime += Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(elapsedTime / _countUpDuration);
                    int shownAmount = Mathf.RoundToInt(Mathf.Lerp(fromAmount, toAmount, progress));
                    _moneyCountShown = shownAmount;
                    _moneyCount.text = $"{shownAmount}";
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _moneyCountShown = toAmount;
            _moneyCount.text = $"{toAmount}";
        }

        private void OnEnable()
        {
            _nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            _doubleRewardButton.onClick.AddListener(OnDoubleRewardClicked);
            _menuButton.onClick.AddListener(OnMenuClicked);
        }

        protected override void OnDisable()
        {
            CancelCountUp();
            base.OnDisable();
            _nextLevelButton.onClick.RemoveListener(OnNextLevelClicked);
            _doubleRewardButton.onClick.RemoveListener(OnDoubleRewardClicked);
            _menuButton.onClick.RemoveListener(OnMenuClicked);
        }

        protected override void OnClosing()
        {
            CancelCountUp();
            _nextLevelButton.interactable = false;
            _doubleRewardButton.interactable = false;
            _menuButton.interactable = false;
        }

        private void CancelCountUp()
        {
            if (_countUpCancellation == null)
            {
                return;
            }

            _countUpCancellation.Cancel();
            _countUpCancellation.Dispose();
            _countUpCancellation = null;
        }

        private void OnNextLevelClicked()
        {
            if (_isNavigationRequested == true || IsClosing == true)
            {
                return;
            }

            _isNavigationRequested = true;
            Action nextLevelRequested = NextLevelRequested;
            nextLevelRequested?.Invoke();

        }

        public void SetDoubleRewardAvailable(bool isAvailable)
        {
            _doubleRewardButton.interactable = isAvailable;
        }

        private void OnDoubleRewardClicked()
        {
            if (_isNavigationRequested == true || IsClosing == true)
            {
                return;
            }
            Action doubleRewardRequested = DoubleRewardRequested;
            doubleRewardRequested?.Invoke();
        }

        private void OnMenuClicked()
        {
            if (_isNavigationRequested == true || IsClosing == true)
            {
                return;
            }

            _isNavigationRequested = true;
            Action menuRequested = MenuRequested;
            menuRequested?.Invoke();

        }
    }
}
