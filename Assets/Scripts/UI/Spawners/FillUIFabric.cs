using Audio;
using Game;
using Scriptables;
using Skills;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace UI
{
    public class FillUIFabric : MonoBehaviour
    {
        [SerializeField] private FillSessionHandler _sessionHandler;
        [SerializeField] private AudioMixerController _mixerController;

        [SerializeField] private Button _pauseButton;

        [SerializeField] private PauseMenu _pauseMenuPrefab;
        [SerializeField] private WinMenu _winMenuPrefab;
        [SerializeField] private FailMenu _failMenuPrefab;
        [SerializeField] private LevelRewardPopup _levelRewardPopupPrefab;

        [SerializeField] private SkillsConfig _skillsConfig;
        [SerializeField] private Wallet _wallet;
        [SerializeField] private AdScheduler _adScheduler;

        [SerializeField] private Pauser _pauser;

        private PlayerProgress _progress;
        private int _lastRewardAmount;

        [Inject]
        public void Construct(PlayerProgress progress)
        {
            _progress = progress;
        }

        private void OnEnable()
        {
            _sessionHandler.Failed += OnGameFailed;
            _sessionHandler.Win += OnGameWin;

            _pauseButton.onClick.AddListener(OnPauseButtonClick);
        }

        private void OnDisable()
        {
            _sessionHandler.Failed -= OnGameFailed;
            _sessionHandler.Win -= OnGameWin;

            _pauseButton.onClick.RemoveListener(OnPauseButtonClick);
        }

        private void OnGameWin(int rewardAmount)
        {
            _lastRewardAmount = rewardAmount;

            WinMenu winMenu = Instantiate(_winMenuPrefab);
            winMenu.Initialize(
                rewardAmount,
                _pauser,
                _sessionHandler.LoadNextLevel,
                RequestDoubleReward);

            ShowLevelRewardPopup(_progress.CurrentLevel + 1);
        }

        private void OnGameFailed(int rewardAmount)
        {
            FailMenu failMenu = Instantiate(_failMenuPrefab);
            failMenu.Initialize(
                rewardAmount,
                _pauser,
                _sessionHandler.LoadNextLevel,
                _sessionHandler.RestartLevel);
        }

        private void RequestDoubleReward()
        {
            _adScheduler.ShowDoubleReward(OnDoubleRewardGranted);
        }

        private void OnDoubleRewardGranted()
        {
            _wallet.Add(_lastRewardAmount);
        }

        private void ShowLevelRewardPopup(int levelNumber)
        {
            if (_skillsConfig == null)
            {
                Debug.LogWarning(
                    "[Fill] FillUIFabric: SkillsConfig is not assigned. Level reward popup is skipped. Drag the SkillsConfig asset into the Skills Config field.");
                return;
            }

            if (_levelRewardPopupPrefab == null)
            {
                Debug.LogWarning(
                    "[Fill] FillUIFabric: Level Reward Popup prefab is not assigned. Popup is skipped. Drag PopupCanvas.prefab into the Level Reward Popup Prefab field.");
                return;
            }

            foreach (SkillConfig config in _skillsConfig.Skills)
            {
                if (config == null)
                {
                    Debug.LogWarning(
                        "[Fill] SkillsConfig contains a missing entry. Open the asset and remove the empty list items.");
                    continue;
                }

                if (config.RequiredLevel == levelNumber)
                {
                    LevelRewardPopup popup = Instantiate(_levelRewardPopupPrefab);
                    popup.Initialize(levelNumber, config);
                    return;
                }
            }
        }

        private void OnPauseButtonClick()
        {
            PauseMenu pauseMenu = Instantiate(_pauseMenuPrefab);
            pauseMenu.Initialize(_pauser, _mixerController, showRestart: false, restartAction: null);
        }
    }
}
