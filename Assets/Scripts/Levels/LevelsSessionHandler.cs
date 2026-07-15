using Game;
using Levels.Levels;
using UnityEngine;
using UnityEngine.UI;
using YG;

namespace Levels
{
    [RequireComponent(typeof(LevelTransitor))]
    public class LevelsSessionHandler : MonoBehaviour
    {
        [SerializeField] private LevelProgressPanel _panel;
        [SerializeField] private LevelRewardPopup _rewardPopupPrefab;
        [SerializeField] private Button _closeButton;
        [SerializeField] private int _totalLevels = 10;

        private LevelTransitor _levelTransitor;
        private bool _isInitialized;

        private void Awake()
        {
            _levelTransitor = GetComponent<LevelTransitor>();
        }

        private void OnEnable()
        {
            YG2.onGetSDKData += OnSDKDataLoaded;

            if (YG2.isSDKEnabled == true)
            {
                Initialize();
            }
        }

        private void OnDisable()
        {
            YG2.onGetSDKData -= OnSDKDataLoaded;

            _closeButton?.onClick.RemoveListener(Close);
            _panel.LevelClicked -= OnLevelClicked;
        }

        private void OnSDKDataLoaded()
        {
            if (_isInitialized == true)
            {
                return;
            }

            Initialize();
        }

        private void Initialize()
        {
            _closeButton.onClick.AddListener(Close);
            _panel.LevelClicked += OnLevelClicked;

            int currentLevel = YG2.saves.CurrentLevel;
            _panel.Populate(_totalLevels, currentLevel);

            _isInitialized = true;
        }

        private void OnLevelClicked(int level)
        {
            LevelRewardPopup popup = Instantiate(_rewardPopupPrefab);
            popup.Show(level);
        }

        private void Close()
        {
            string previousScene = YG2.saves.PreviousScene;
            _levelTransitor.LoadScene(previousScene);
        }
    }
    
}