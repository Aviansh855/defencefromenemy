using UnityEngine;

namespace MobRush
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        Won,
        Lost
    }

    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null) _instance = FindAnyObjectByType<GameManager>();
                return _instance;
            }
            private set => _instance = value;
        }

        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        [Header("System References")]
        public LevelManager levelManager;
        public UIManager uiManager;

        private LevelManager LevelMgr => levelManager != null ? levelManager : (levelManager = LevelManager.Instance);
        private UIManager UIMgr => uiManager != null ? uiManager : (uiManager = UIManager.Instance);

        public int LastEarnedCoins { get; private set; }
        public int LastEarnedBricks { get; private set; }
        private bool hasRewardedCurrentWin = false;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(this);
            }
        }

        private void Start()
        {
            SaveManager.Load();
            ShowMainMenu();
        }

        public void ShowMainMenu()
        {
            Time.timeScale = 1f;
            CurrentState = GameState.MainMenu;

            if (LevelMgr != null)
            {
                LevelMgr.ClearCurrentLevel();
            }

            if (UIMgr != null)
            {
                UIMgr.ShowMainMenu();
            }
        }

        public void PlayHighestUnlockedLevel()
        {
            int lvl = Mathf.Max(1, SaveManager.Data.unlockedLevel);
            StartLevel(lvl);
        }

        public void StartLevel(int levelId)
        {
            Time.timeScale = 1f;
            hasRewardedCurrentWin = false;

            SaveManager.Data.currentLevel = levelId;
            SaveManager.Save();

            CurrentState = GameState.Playing;

            if (LevelMgr != null)
            {
                LevelMgr.LoadLevel(levelId);
            }

            if (UIMgr != null)
            {
                UIMgr.ShowGameplayHUD(levelId);
            }
        }

        public void StartCurrentLevel()
        {
            StartLevel(SaveManager.Data.currentLevel);
        }

        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;

            CurrentState = GameState.Paused;
            Time.timeScale = 0f;

            if (UIMgr != null)
            {
                UIMgr.ShowPausePanel();
            }
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;

            CurrentState = GameState.Playing;
            Time.timeScale = 1f;

            if (UIMgr != null)
            {
                UIMgr.HidePausePanel();
            }
        }

        public void OnLevelWon()
        {
            if (CurrentState != GameState.Playing) return;

            CurrentState = GameState.Won;

            // AC-08: Win on level N grants that row's coins once, unlocks N+1, and a second open of the panel does not pay again
            if (!hasRewardedCurrentWin)
            {
                hasRewardedCurrentWin = true;

                int baseReward = (LevelMgr != null && LevelMgr.CurrentConfig != null)
                    ? LevelMgr.CurrentConfig.completionReward
                    : 100;
                int bricksReward = (LevelMgr != null && LevelMgr.CurrentConfig != null)
                    ? LevelMgr.CurrentConfig.completionBricks
                    : 0;
                int shieldsReward = (LevelMgr != null && LevelMgr.CurrentConfig != null)
                    ? LevelMgr.CurrentConfig.completionShields
                    : 0;
                int starsReward = (LevelMgr != null && LevelMgr.CurrentConfig != null)
                    ? LevelMgr.CurrentConfig.completionStars
                    : 0;

                LastEarnedCoins = baseReward;
                LastEarnedBricks = bricksReward;

                SaveManager.Data.coins += LastEarnedCoins;
                SaveManager.Data.bricks += LastEarnedBricks;
                SaveManager.Data.shields += shieldsReward;
                SaveManager.Data.leagueStars += starsReward;

                int nextLevel = SaveManager.Data.currentLevel + 1;
                if (nextLevel > SaveManager.Data.unlockedLevel)
                {
                    SaveManager.Data.unlockedLevel = nextLevel;
                }

                // AC-12: Champion stays hidden through level 9 and can spawn on level 11 after a level 10 win
                if (SaveManager.Data.currentLevel >= 10)
                {
                    SaveManager.Data.championUnlocked = true;
                }

                SaveManager.Save();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWin();
            }

            if (UIMgr != null)
            {
                UIMgr.ShowWinPanel(LastEarnedCoins);
            }
        }

        public void DoubleReward(System.Action onCompleted = null, System.Action onFailed = null)
        {
            // AC-11: Double pays only after a completed rewarded ad
            if (AdManager.Instance != null)
            {
                AdManager.Instance.ShowRewardedAd(
                    onRewarded: () =>
                    {
                        SaveManager.Data.coins += LastEarnedCoins;
                        SaveManager.Save();

                        if (AudioManager.Instance != null)
                        {
                            AudioManager.Instance.PlayUpgrade();
                        }

                        if (UIMgr != null)
                        {
                            UIMgr.UpdateWinRewardDisplay(LastEarnedCoins * 2);
                        }

                        onCompleted?.Invoke();
                    },
                    onClosed: () =>
                    {
                        // Ad closed
                    }
                );
            }
            else
            {
                SaveManager.Data.coins += LastEarnedCoins;
                SaveManager.Save();

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayUpgrade();
                }

                if (UIMgr != null)
                {
                    UIMgr.UpdateWinRewardDisplay(LastEarnedCoins * 2);
                }

                onCompleted?.Invoke();
            }
        }

        public void OnLevelLost()
        {
            if (CurrentState != GameState.Playing) return;

            CurrentState = GameState.Lost;
            // AC-09: Loss grants 0 coins and does not unlock next level

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDefeat();
            }

            if (UIMgr != null)
            {
                UIMgr.ShowLosePanel();
            }
        }

        public void NextLevel()
        {
            int next = Mathf.Max(1, SaveManager.Data.unlockedLevel);

            // Show Interstitial Ad (Format 2/6: Full-Screen Interstitial between levels)
            if (AdManager.Instance != null && next % 2 == 0)
            {
                AdManager.Instance.ShowInterstitial(onClosed: () =>
                {
                    StartLevel(next);
                });
            }
            else
            {
                StartLevel(next);
            }
        }

        public void RetryLevel()
        {
            StartLevel(SaveManager.Data.currentLevel);
        }

        public void GameOver(bool won)
        {
            if (won) OnLevelWon();
            else OnLevelLost();
        }

        public void ShowRewardedAdForDoubleReward(System.Action<bool> callback)
        {
            DoubleReward(
                onCompleted: () => callback?.Invoke(true),
                onFailed: () => callback?.Invoke(false)
            );
        }
    }
}
