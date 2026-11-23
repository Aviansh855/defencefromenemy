using System;
using UnityEngine;

namespace MobRush
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null) _instance = FindAnyObjectByType<UIManager>();
                return _instance;
            }
            private set => _instance = value;
        }

        public enum UIState
        {
            MainMenu,
            GameplayHUD,
            Pause,
            Win,
            Lose,
            UpgradeShop,
            BasePanel,
            Settings
        }

        public UIState currentState = UIState.MainMenu;
        private UIState previousState = UIState.MainMenu;

        private int currentLevelDisplay = 1;
        private int lastRewardDisplay = 0;
        private int lastBricksDisplay = 0;
        private bool hasClaimedDouble = false;

        // End-of-Level Multiplier Needle Minigame (9.5 Juice)
        private float needlePos = 0.5f;
        private bool isNeedleStopped = false;
        private int stoppedMultiplier = 2;
        private bool hasClaimedNeedleBonus = false;

        // Feedback toasts
        private string feedbackMessage = "";
        private float feedbackTimer = 0f;

        // Reset progress confirm dialog
        private bool showResetConfirm = false;

        // Tutorial system
        private string activeTutorialStep = null;
        private string activeTutorialText = null;
        private float levelStartTime = 0f;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else if (_instance != this) Destroy(this);
        }

        private void Update()
        {
            if (feedbackTimer > 0f && Time.unscaledTime > feedbackTimer)
            {
                feedbackMessage = "";
            }

            // Check Tutorial triggers during gameplay
            if (currentState == UIState.GameplayHUD && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                CheckTutorialConditions();
            }
        }

        public void ShowMainMenu()
        {
            currentState = UIState.MainMenu;
            showResetConfirm = false;
        }

        public void ShowGameplayHUD(int levelId)
        {
            currentState = UIState.GameplayHUD;
            currentLevelDisplay = levelId;
            levelStartTime = Time.time;
            activeTutorialStep = null;
            activeTutorialText = null;

            // Trigger T1 on Level 1 start
            if (levelId == 1 && !SaveManager.Data.tutorialStepsDone.Contains("T1"))
            {
                TriggerTutorialStep("T1", "Drag left or right to aim.");
            }
            // Trigger T4 on Level 4 start
            else if (levelId == 4 && !SaveManager.Data.tutorialStepsDone.Contains("T4"))
            {
                TriggerTutorialStep("T4", "This bar hits only your units.");
            }
            // Trigger T5 on Level 5 start
            else if (levelId == 5 && !SaveManager.Data.tutorialStepsDone.Contains("T5"))
            {
                TriggerTutorialStep("T5", "Their cannon fires too. Shoot the group.");
            }
        }

        private void CheckTutorialConditions()
        {
            if (currentLevelDisplay == 1)
            {
                // T2: After the cannon moves, or after 3 seconds
                if (!SaveManager.Data.tutorialStepsDone.Contains("T2") && SaveManager.Data.tutorialStepsDone.Contains("T1"))
                {
                    CannonController cannon = LevelManager.Instance?.cannon;
                    bool moved = cannon != null && cannon.hasMovedSinceStart;
                    bool timePassed = (Time.time - levelStartTime) >= 3.0f;

                    if ((moved || timePassed) && activeTutorialStep == null)
                    {
                        TriggerTutorialStep("T2", "Your cannon keeps sending units.");
                    }
                }
                // T3: First gate on screen
                else if (!SaveManager.Data.tutorialStepsDone.Contains("T3") && SaveManager.Data.tutorialStepsDone.Contains("T2"))
                {
                    if ((Time.time - levelStartTime) >= 4.5f && activeTutorialStep == null)
                    {
                        TriggerTutorialStep("T3", "Choose a gate to grow your crowd.");
                    }
                }
            }
        }

        public void TriggerTutorialStep(string stepId, string text = null)
        {
            if (SaveManager.Data.tutorialStepsDone.Contains(stepId)) return;

            activeTutorialStep = stepId;
            if (text != null) activeTutorialText = text;
            else
            {
                switch (stepId)
                {
                    case "T1": activeTutorialText = "Drag left or right to aim."; break;
                    case "T2": activeTutorialText = "Your cannon keeps sending units."; break;
                    case "T3": activeTutorialText = "Choose a gate to grow your crowd."; break;
                    case "T4": activeTutorialText = "This bar hits only your units."; break;
                    case "T5": activeTutorialText = "Their cannon fires too. Shoot the group."; break;
                    case "T6": activeTutorialText = "Reach their cannon to win."; break;
                }
            }
        }

        private void DismissTutorialStep()
        {
            if (activeTutorialStep != null)
            {
                if (!SaveManager.Data.tutorialStepsDone.Contains(activeTutorialStep))
                {
                    SaveManager.Data.tutorialStepsDone.Add(activeTutorialStep);
                    SaveManager.Save();
                }
                activeTutorialStep = null;
                activeTutorialText = null;
            }
        }

        public void ShowPausePanel()
        {
            currentState = UIState.Pause;
        }

        public void HidePausePanel()
        {
            currentState = UIState.GameplayHUD;
        }

        public void ShowWinPanel(int reward)
        {
            currentState = UIState.Win;
            lastRewardDisplay = reward;
            lastBricksDisplay = GameManager.Instance != null ? GameManager.Instance.LastEarnedBricks : 15;
            hasClaimedDouble = false;
            isNeedleStopped = false;
            hasClaimedNeedleBonus = false;
            stoppedMultiplier = 2;
        }

        public void UpdateWinRewardDisplay(int newReward)
        {
            lastRewardDisplay = newReward;
            hasClaimedDouble = true;
        }

        public void ShowLosePanel()
        {
            currentState = UIState.Lose;
        }

        public void ShowUpgradeShop()
        {
            previousState = currentState;
            currentState = UIState.UpgradeShop;
        }

        public void ShowBasePanel()
        {
            currentState = UIState.BasePanel;
        }

        public void ShowSettings()
        {
            currentState = UIState.Settings;
            showResetConfirm = false;
        }

        private void ShowToast(string message)
        {
            feedbackMessage = message;
            feedbackTimer = Time.unscaledTime + 2.5f;
        }

        private const float DesignWidth = 450f;
        private const float DesignHeight = 800f;

        private void OnGUI()
        {
            float scaleX = (float)Screen.width / DesignWidth;
            float scaleY = (float)Screen.height / DesignHeight;
            float scale = Mathf.Min(scaleX, scaleY);

            // Compute centered viewport so UI adapts cleanly on any phone resolution and aspect ratio (16:9, 19.5:9, 21:9)
            float scaledW = DesignWidth * scale;
            float scaledH = DesignHeight * scale;
            float offsetX = ((float)Screen.width - scaledW) / 2f;
            float offsetY = ((float)Screen.height - scaledH) / 2f;

            Matrix4x4 origMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));

            GUI.skin.button.fontSize = 16;
            GUI.skin.label.fontSize = 16;

            switch (currentState)
            {
                case UIState.MainMenu:
                    DrawMainMenu();
                    break;
                case UIState.GameplayHUD:
                    DrawGameplayHUD();
                    break;
                case UIState.Pause:
                    DrawPausePanel();
                    break;
                case UIState.Win:
                    DrawWinPanel();
                    break;
                case UIState.Lose:
                    DrawLosePanel();
                    break;
                case UIState.UpgradeShop:
                    DrawUpgradeShop();
                    break;
                case UIState.BasePanel:
                    DrawBasePanel();
                    break;
                case UIState.Settings:
                    DrawSettings();
                    break;
            }

            // Draw toast notification if active
            if (!string.IsNullOrEmpty(feedbackMessage))
            {
                DrawToastNotification();
            }

            GUI.matrix = origMatrix;
        }

        private void DrawToastNotification()
        {
            float sw = DesignWidth;
            float tw = Mathf.Min(340f, sw - 40f);
            float tx = (sw - tw) / 2f;
            float ty = DesignHeight * 0.78f;

            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            GUI.Box(new Rect(tx, ty, tw, 44), "");
            GUIStyle toastStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            toastStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);
            GUI.Label(new Rect(tx, ty, tw, 44), feedbackMessage, toastStyle);
            GUI.backgroundColor = Color.white;
        }

        // ==========================================
        // 8.1 MAIN MENU
        // ==========================================
        private void DrawMainMenu()
        {
            float sw = DesignWidth;
            float sh = DesignHeight;

            // Top Bar
            GUI.Box(new Rect(10, 10, sw - 20, 52), "");

            // Coin counter (Section 8.1: "Opens the same upgrade panel. It is not a dead label.")
            GUI.backgroundColor = new Color(0.2f, 0.25f, 0.35f);
            if (GUI.Button(new Rect(18, 16, 140, 40), $"🪙 {SaveManager.Data.coins} [+]"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                ShowUpgradeShop();
            }

            // Bricks button (Section 8.1: "Base: Opens the base panel. Shows bricks owned and base level.")
            GUI.backgroundColor = new Color(0.35f, 0.25f, 0.2f);
            if (GUI.Button(new Rect(165, 16, 120, 40), $"🧱 {SaveManager.Data.bricks} [Base]"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                ShowBasePanel();
            }

            // Unlocked Level label
            GUIStyle levelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            levelStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(sw - 190, 18, 175, 36), $"Unlocked: Lv {SaveManager.Data.unlockedLevel}", levelStyle);

            GUI.backgroundColor = Color.white;

            // Title Banner
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 38,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(0.2f, 0.8f, 1f);
            GUI.Label(new Rect(0, sh * 0.17f, sw, 48), "MOB RUSH", titleStyle);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15
            };
            subStyle.normal.textColor = new Color(0.8f, 0.85f, 0.9f);
            GUI.Label(new Rect(0, sh * 0.23f, sw, 26), "Crowd Multiplier Strategy", subStyle);

            // Difficulty Selector Bar
            float diffW = Mathf.Min(sw * 0.84f, 360f);
            float diffX = (sw - diffW) / 2f;
            float diffY = sh * 0.28f;
            float segW = (diffW - 12f) / 3f;
            float segH = 36f;

            GameDifficulty currentDiff = SaveManager.Data.Difficulty;

            // Simple Mode Button
            bool isSimple = (currentDiff == GameDifficulty.Simple);
            GUI.backgroundColor = isSimple ? new Color(0.2f, 0.9f, 0.4f) : new Color(0.25f, 0.3f, 0.35f, 0.75f);
            if (GUI.Button(new Rect(diffX, diffY, segW, segH), isSimple ? "★ SIMPLE" : "Simple"))
            {
                SaveManager.Data.Difficulty = GameDifficulty.Simple;
                SaveManager.Save();
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                ShowToast("Difficulty: Simple (Relaxed Pacing)");
            }

            // Medium Mode Button
            bool isMed = (currentDiff == GameDifficulty.Medium);
            GUI.backgroundColor = isMed ? new Color(1f, 0.8f, 0.2f) : new Color(0.25f, 0.3f, 0.35f, 0.75f);
            if (GUI.Button(new Rect(diffX + segW + 6f, diffY, segW, segH), isMed ? "★ MEDIUM" : "Medium"))
            {
                SaveManager.Data.Difficulty = GameDifficulty.Medium;
                SaveManager.Save();
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                ShowToast("Difficulty: Medium (Standard)");
            }

            // Hard Mode Button
            bool isHard = (currentDiff == GameDifficulty.Hard);
            GUI.backgroundColor = isHard ? new Color(1f, 0.3f, 0.35f) : new Color(0.25f, 0.3f, 0.35f, 0.75f);
            if (GUI.Button(new Rect(diffX + (segW + 6f) * 2f, diffY, segW, segH), isHard ? "★ HARD" : "Hard"))
            {
                SaveManager.Data.Difficulty = GameDifficulty.Hard;
                SaveManager.Save();
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                ShowToast("Difficulty: Hard (Aggressive Horde)");
            }

            GUI.backgroundColor = Color.white;

            // Center Actions
            float btnW = Mathf.Min(sw * 0.84f, 360f);
            float btnH = 52f;
            float btnX = (sw - btnW) / 2f;
            float startY = diffY + 48f;

            // 1. Play: "Load the gameplay scene on the highest unlocked level. First launch loads level 1 and shows T1."
            GUI.backgroundColor = new Color(0.1f, 0.88f, 0.35f);
            if (GUI.Button(new Rect(btnX, startY, btnW, btnH), $"▶  PLAY LEVEL {SaveManager.Data.unlockedLevel}"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                GameManager.Instance?.PlayHighestUnlockedLevel();
            }

            // 2. Upgrade: "Open the upgrade panel. Coins and the three cards match the save."
            GUI.backgroundColor = new Color(0.2f, 0.65f, 1f);
            if (GUI.Button(new Rect(btnX, startY + 62f, btnW, btnH), "⚡  UPGRADES"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                ShowUpgradeShop();
            }

            // 3. Base: "Opens the base panel. Shows bricks owned and base level."
            GUI.backgroundColor = new Color(0.85f, 0.5f, 0.15f);
            if (GUI.Button(new Rect(btnX, startY + 124f, btnW, btnH), $"🏰  BASE (Lv {SaveManager.Data.baseLevel})"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                ShowBasePanel();
            }

            // 4. Daily Reward: "If today's claim is unused, add the day's coins, save the date, and show the amount. If already claimed, show the countdown to the next local midnight and do not pay again."
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            bool canClaimDaily = SaveManager.Data.dailyClaimDate != today;

            if (canClaimDaily)
            {
                int rewardAmt = UpgradeConfig.GetDailyReward(SaveManager.Data.dailyStreak);
                GUI.backgroundColor = new Color(1f, 0.78f, 0.1f);
                if (GUI.Button(new Rect(btnX, startY + 186f, btnW, btnH), $"🎁  DAILY REWARD (+{rewardAmt}🪙)"))
                {
                    SaveManager.Data.coins += rewardAmt;
                    SaveManager.Data.dailyClaimDate = today;
                    SaveManager.Data.dailyStreak++;
                    SaveManager.Save();

                    if (AudioManager.Instance != null) AudioManager.Instance.PlayUpgrade();
                    ShowToast($"🎁 Claimed +{rewardAmt} Daily Coins!");
                }
            }
            else
            {
                DateTime midnight = DateTime.Today.AddDays(1);
                TimeSpan remaining = midnight - DateTime.Now;
                string timerText = string.Format("{0:D2}:{1:D2}:{2:D2}", remaining.Hours, remaining.Minutes, remaining.Seconds);

                GUI.backgroundColor = new Color(0.45f, 0.45f, 0.45f);
                if (GUI.Button(new Rect(btnX, startY + 186f, btnW, btnH), $"🎁  CLAIMED ({timerText})"))
                {
                    ShowToast("Already claimed today! Next reward at midnight.");
                }
            }

            // 5. Settings: "Open settings."
            GUI.backgroundColor = new Color(0.6f, 0.65f, 0.75f);
            if (GUI.Button(new Rect(btnX, startY + 248f, btnW, btnH), "⚙  SETTINGS"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                ShowSettings();
            }

            // 6. Native Ad Card (Format 5/6: Embedded Native In-Feed Ad)
            if (AdManager.Instance != null)
            {
                AdManager.Instance.DrawNativeAdCard(new Rect(btnX, startY + 312f, btnW, 64f));
            }

            GUI.backgroundColor = Color.white;
        }

        // ==========================================
        // 8.2 FIGHT HUD
        // ==========================================
        private void DrawGameplayHUD()
        {
            float sw = DesignWidth;

            // Top Bar
            GUI.Box(new Rect(10, 10, sw - 20, 50), "");

            // Pause Button: "Time scale 0. Cannons, units, bursts, and moving objects stop. Open the pause panel."
            if (GUI.Button(new Rect(18, 15, 42, 40), "II"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                GameManager.Instance?.PauseGame();
            }

            // Level Display with Difficulty Tag
            GUIStyle levelTitle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            levelTitle.normal.textColor = Color.white;
            string diffBadge = (SaveManager.Data != null) ? SaveManager.Data.Difficulty.ToString().ToUpper() : "SIMPLE";
            GUI.Label(new Rect(70, 15, sw - 210, 40), $"LEVEL {currentLevelDisplay}  •  {diffBadge}", levelTitle);

            // Crowd Count: "Display only. Shows living player units. Not tappable."
            int aliveCount = (ObjectPooler.Instance != null) ? ObjectPooler.Instance.GetActivePlayerUnitCount() : 0;
            GUIStyle countStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 17,
                fontStyle = FontStyle.Bold
            };
            countStyle.normal.textColor = new Color(0.2f, 0.9f, 1f);
            GUI.Label(new Rect(sw - 140, 15, 120, 40), $"👥 {aliveCount}", countStyle);

            // Cannon HP & Enemy Citadel HP Banner
            CannonController cannon = LevelManager.Instance?.cannon;
            BaseController citadel = LevelManager.Instance?.enemyBase;

            float infoY = 65f;
            GUIStyle hpStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            if (cannon != null)
            {
                hpStyle.normal.textColor = new Color(0.2f, 0.8f, 1f);
                GUI.Label(new Rect(20, infoY, 160, 25), $"🛡️ Cannon HP: {cannon.currentHealth}/{cannon.maxHealth}", hpStyle);

                // Champion Charge Bar (Mob Control Official Mechanic)
                float champBarY = DesignHeight - 75f;
                float champBarW = sw - 40f;
                float champBarH = 20f;
                float champProgress = Mathf.Clamp01(cannon.championCharge / cannon.maxChampionCharge);

                // Background
                GUI.backgroundColor = new Color(0.1f, 0.12f, 0.16f, 0.9f);
                GUI.Box(new Rect(20, champBarY, champBarW, champBarH), "");

                // Fill
                GUI.backgroundColor = (champProgress >= 0.99f) ? new Color(1f, 0.85f, 0.1f) : new Color(0.1f, 0.75f, 1f);
                GUI.Box(new Rect(20, champBarY, champBarW * champProgress, champBarH), "");

                // Label
                GUIStyle champStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    fontStyle = FontStyle.Bold
                };
                champStyle.normal.textColor = Color.white;
                string champLabel = (champProgress >= 0.99f) ? "⚡ CHAMPION READY! ⚡" : $"⚡ CHAMPION CHARGE: {Mathf.RoundToInt(champProgress * 100)}%";
                GUI.Label(new Rect(20, champBarY, champBarW, champBarH), champLabel, champStyle);
                GUI.backgroundColor = Color.white;
            }

            if (citadel != null && citadel.gameObject.activeInHierarchy)
            {
                hpStyle.normal.textColor = new Color(1f, 0.35f, 0.35f);
                hpStyle.alignment = TextAnchor.MiddleRight;
                GUI.Label(new Rect(sw - 180, infoY, 160, 25), $"🏰 Citadel HP: {citadel.currentHealth}/{citadel.maxHealth}", hpStyle);
            }

            // Tutorial Hint (Section 8.8: "Tutorial text is not a blocker. Tapping the hint dismisses that step and saves it so it does not return. The fight keeps running under the hint.")
            if (!string.IsNullOrEmpty(activeTutorialText))
            {
                DrawTutorialBanner(sw);
            }
        }

        private void DrawTutorialBanner(float sw)
        {
            float bw = Mathf.Min(sw - 30f, 380f);
            float bx = (sw - bw) / 2f;
            float by = 98f;
            float bh = 60f;

            GUI.backgroundColor = new Color(0.08f, 0.35f, 0.65f, 0.92f);
            if (GUI.Button(new Rect(bx, by, bw, bh), ""))
            {
                DismissTutorialStep();
            }

            GUIStyle tutStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            tutStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(bx + 10, by + 6, bw - 20, 28), activeTutorialText, tutStyle);

            GUIStyle subTut = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };
            subTut.normal.textColor = new Color(0.8f, 0.95f, 1f);
            GUI.Label(new Rect(bx + 10, by + 34, bw - 20, 20), "👆 Tap hint to dismiss", subTut);

            GUI.backgroundColor = Color.white;
        }

        // ==========================================
        // 8.3 PAUSE PANEL
        // ==========================================
        private void DrawPausePanel()
        {
            float sw = DesignWidth;
            float sh = DesignHeight;
            float w = 310f;
            float h = 360f;
            float x = (sw - w) / 2f;
            float y = (sh - h) / 2f;

            GUI.Box(new Rect(x, y, w, h), "");

            GUIStyle pauseTitle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold
            };
            pauseTitle.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y + 15, w, 40), "PAUSED", pauseTitle);

            float bw = w - 50f;
            float bx = x + 25f;

            // Resume: "Close the panel and set time scale back to 1."
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
            if (GUI.Button(new Rect(bx, y + 65, bw, 44), "RESUME"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                GameManager.Instance?.ResumeGame();
            }

            // Restart: "Reload this level from its start. No reward. Cannon, bursts, and objects reset."
            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.1f);
            if (GUI.Button(new Rect(bx, y + 118, bw, 44), "RESTART"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                GameManager.Instance?.RetryLevel();
            }

            // Music: "Toggle music mute and save."
            bool musicOn = SaveManager.Data.musicOn;
            GUI.backgroundColor = musicOn ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.5f, 0.5f, 0.5f);
            if (GUI.Button(new Rect(bx, y + 171, bw, 42), musicOn ? "🎵 MUSIC: ON" : "🎵 MUSIC: OFF"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetMusicEnabled(!musicOn);
                }
                else
                {
                    SaveManager.Data.musicOn = !musicOn;
                    SaveManager.Save();
                }
            }

            // Sound: "Toggle effect mute and save."
            bool soundOn = SaveManager.Data.soundOn;
            GUI.backgroundColor = soundOn ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.5f, 0.5f, 0.5f);
            if (GUI.Button(new Rect(bx, y + 222, bw, 42), soundOn ? "🔊 SOUND: ON" : "🔊 SOUND: OFF"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetSfxEnabled(!soundOn);
                }
                else
                {
                    SaveManager.Data.soundOn = !soundOn;
                    SaveManager.Save();
                }
            }

            // Home: "Save upgrades and currencies. Load the main menu. This attempt's reward is not granted."
            GUI.backgroundColor = new Color(0.6f, 0.65f, 0.75f);
            if (GUI.Button(new Rect(bx, y + 275, bw, 42), "MAIN MENU"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                SaveManager.Save();
                GameManager.Instance?.ShowMainMenu();
            }

            GUI.backgroundColor = Color.white;
        }

        // ==========================================
        // 8.4 WIN PANEL
        // ==========================================
        private void DrawWinPanel()
        {
            float sw = DesignWidth;
            float sh = DesignHeight;
            float w = 340f;
            float h = 470f;
            float x = (sw - w) / 2f;
            float y = (sh - h) / 2f;

            GUI.Box(new Rect(x, y, w, h), "");

            GUIStyle winTitle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold
            };
            winTitle.normal.textColor = new Color(0.2f, 0.9f, 0.3f);
            GUI.Label(new Rect(x, y + 12, w, 34), "VICTORY!", winTitle);

            GUIStyle sub = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14
            };
            sub.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y + 46, w, 20), "Enemy Citadel Captured!", sub);

            // Granted rewards display
            GUIStyle rewardStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 19,
                fontStyle = FontStyle.Bold
            };
            rewardStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(x, y + 68, w, 26), $"🪙 +{lastRewardDisplay} Coins   🧱 +{lastBricksDisplay} Bricks", rewardStyle);

            // MULTIPLIER NEEDLE MINIGAME
            float gaugeY = y + 98f;
            float gaugeW = w - 40f;
            float gaugeX = x + 20f;
            float gaugeH = 28f;

            if (!isNeedleStopped)
            {
                needlePos = Mathf.PingPong(Time.unscaledTime * 2.2f, 1f);
            }

            // Draw Gauge background & segments: [2X] [3X] [5X] [10X] [5X] [3X] [2X]
            GUI.Box(new Rect(gaugeX, gaugeY, gaugeW, gaugeH), "");
            GUIStyle segStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            segStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(gaugeX, gaugeY + 5, gaugeW, 18), "2X  |  3X  |  5X  |  ★ 10X ★  |  5X  |  3X  |  2X", segStyle);

            // Draw needle indicator
            float needleXPos = gaugeX + needlePos * (gaugeW - 14f);
            GUI.backgroundColor = isNeedleStopped ? Color.green : Color.red;
            GUI.Box(new Rect(needleXPos, gaugeY - 6f, 14f, gaugeH + 12f), "▼");
            GUI.backgroundColor = Color.white;

            float bw = w - 50f;
            float bx = x + 25f;
            float nextY = gaugeY + gaugeH + 14f;

            if (!isNeedleStopped)
            {
                GUI.backgroundColor = new Color(1f, 0.85f, 0.1f);
                if (GUI.Button(new Rect(bx, nextY, bw, 42), "🎯 TAP TO STOP & MULTIPLY!"))
                {
                    isNeedleStopped = true;
                    float d = Mathf.Abs(needlePos - 0.5f);
                    if (d < 0.08f) stoppedMultiplier = 10;
                    else if (d < 0.22f) stoppedMultiplier = 5;
                    else if (d < 0.38f) stoppedMultiplier = 3;
                    else stoppedMultiplier = 2;

                    if (!hasClaimedNeedleBonus)
                    {
                        hasClaimedNeedleBonus = true;
                        int bonus = (stoppedMultiplier - 1) * lastRewardDisplay;
                        SaveManager.Data.coins += bonus;
                        lastRewardDisplay *= stoppedMultiplier;
                        SaveManager.Save();
                        if (AudioManager.Instance != null)
                        {
                            AudioManager.Instance.PlayChampionSpawn();
                            AudioManager.Instance.PlayCoinPickup();
                        }
                        ShowToast($"🎉 JACKPOT! {stoppedMultiplier}X MULTIPLIER GRANTED!");
                    }
                }
                GUI.backgroundColor = Color.white;
                nextY += 48f;
            }
            else
            {
                GUIStyle hitStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 15,
                    fontStyle = FontStyle.Bold
                };
                hitStyle.normal.textColor = new Color(0.3f, 1f, 0.4f);
                GUI.Label(new Rect(x, nextY, w, 24), $"🎉 {stoppedMultiplier}X BONUS APPLIED!", hitStyle);
                nextY += 28f;
            }

            // Double Reward / Extra Ad button
            if (!hasClaimedDouble)
            {
                GUI.backgroundColor = new Color(1f, 0.75f, 0.1f);
                if (GUI.Button(new Rect(bx, nextY, bw, 40), "🎁 EXTRA 2X COINS (Ad)"))
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                    GameManager.Instance?.DoubleReward(
                        onCompleted: () => {
                            hasClaimedDouble = true;
                            ShowToast("Double coins granted!");
                        },
                        onFailed: () => {
                            ShowToast("Ad failed to complete.");
                        }
                    );
                }
                nextY += 46f;
            }

            // Next Level
            GUI.backgroundColor = new Color(0.1f, 0.75f, 1f);
            if (GUI.Button(new Rect(bx, nextY, bw, 44), "NEXT LEVEL ▶"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                GameManager.Instance?.NextLevel();
            }

            // Upgrades
            GUI.backgroundColor = new Color(0.2f, 0.65f, 1f);
            if (GUI.Button(new Rect(bx, nextY + 50f, bw, 38), "⚡  UPGRADES"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                ShowUpgradeShop();
            }

            // Main Menu
            GUI.backgroundColor = new Color(0.6f, 0.65f, 0.75f);
            if (GUI.Button(new Rect(bx, nextY + 94f, bw, 36), "MAIN MENU"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                SaveManager.Save();
                GameManager.Instance?.ShowMainMenu();
            }

            GUI.backgroundColor = Color.white;
        }

        // ==========================================
        // 8.5 LOSE PANEL
        // ==========================================
        private void DrawLosePanel()
        {
            float sw = DesignWidth;
            float sh = DesignHeight;
            float w = 310f;
            float h = 300f;
            float x = (sw - w) / 2f;
            float y = (sh - h) / 2f;

            GUI.Box(new Rect(x, y, w, h), "");

            GUIStyle loseTitle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold
            };
            loseTitle.normal.textColor = new Color(1f, 0.3f, 0.3f);
            GUI.Label(new Rect(x, y + 20, w, 40), "DEFEAT", loseTitle);

            GUIStyle sub = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15
            };
            sub.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y + 65, w, 25), "Cannon was destroyed!", sub);

            float bw = w - 60f;
            float bx = x + 30f;

            // Retry: "Reload this same level from the start. There is no energy cost on Retry."
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
            if (GUI.Button(new Rect(bx, y + 105, bw, 45), "🔄  RETRY"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();

                // Format 4/6: Rewarded Interstitial Ad (Chance for quick revive/retry boost)
                if (AdManager.Instance != null)
                {
                    AdManager.Instance.ShowRewardedInterstitial(
                        onRewarded: () =>
                        {
                            SaveManager.Data.coins += 50; // Free pity coins for watching
                            SaveManager.Save();
                            ShowToast("Bonus +50 Coins Granted!");
                            GameManager.Instance?.RetryLevel();
                        },
                        onClosed: () =>
                        {
                            GameManager.Instance?.RetryLevel();
                        }
                    );
                }
                else
                {
                    GameManager.Instance?.RetryLevel();
                }
            }

            // Upgrade: "Open the upgrade panel. Closing it returns to the lose panel."
            GUI.backgroundColor = new Color(0.2f, 0.65f, 1f);
            if (GUI.Button(new Rect(bx, y + 158, bw, 42), "⚡  UPGRADES"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                ShowUpgradeShop();
            }

            // Home: "Load the main menu. Nothing new is unlocked."
            GUI.backgroundColor = new Color(0.6f, 0.65f, 0.75f);
            if (GUI.Button(new Rect(bx, y + 208, bw, 42), "MAIN MENU"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                GameManager.Instance?.ShowMainMenu();
            }

            GUI.backgroundColor = Color.white;
        }

        // ==========================================
        // 8.6 UPGRADE PANEL
        // ==========================================
        private void DrawUpgradeShop()
        {
            float sw = DesignWidth;
            float sh = DesignHeight;

            GUI.Box(new Rect(10, 10, sw - 20, sh - 20), "");

            // Header
            GUIStyle title = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 26,
                fontStyle = FontStyle.Bold
            };
            title.normal.textColor = Color.white;
            GUI.Label(new Rect(0, 20, sw, 40), "ARMORY UPGRADES", title);

            GUIStyle coinStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            coinStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(0, 60, sw, 30), $"🪙 Available Coins: {SaveManager.Data.coins}", coinStyle);

            float cardW = Mathf.Min(sw - 40f, 340f);
            float cardX = (sw - cardW) / 2f;
            float startY = 105f;

            // 1. Buy on Fire Rate: "Spend coins, add 1 level, apply the new fire rate, save, refresh the card."
            DrawUpgradeCard(cardX, startY, cardW, "CANNON FIRE RATE",
                SaveManager.Data.fireRateLevel,
                $"{UpgradeConfig.GetFireRate(SaveManager.Data.fireRateLevel):F1} mobs/sec",
                $"+0.65 mobs/sec",
                () => {
                    SaveManager.Data.fireRateLevel++;
                });

            // 2. Buy on Strength: "Same for strength."
            DrawUpgradeCard(cardX, startY + 95, cardW, "UNIT STRENGTH",
                SaveManager.Data.strengthLevel,
                $"Power: {UpgradeConfig.GetUnitStrength(SaveManager.Data.strengthLevel)} DMG",
                $"+Damage vs Base & Mobs",
                () => {
                    SaveManager.Data.strengthLevel++;
                });

            // 3. Buy on Starting push: "Same for the starting push."
            DrawUpgradeCard(cardX, startY + 190, cardW, "STARTING PUSH",
                SaveManager.Data.startingPushLevel,
                $"Burst: {UpgradeConfig.GetStartingPush(SaveManager.Data.startingPushLevel)} units",
                $"+3 starting units",
                () => {
                    SaveManager.Data.startingPushLevel++;
                });

            // Close: "Return to whichever screen opened the panel: menu, win, or lose."
            GUI.backgroundColor = new Color(0.6f, 0.65f, 0.75f);
            if (GUI.Button(new Rect(cardX, startY + 300, cardW, 45), "◀  CLOSE"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                currentState = previousState;
            }

            GUI.backgroundColor = Color.white;
        }

        private void DrawUpgradeCard(float x, float y, float w, string name, int currentLevel, string currentValueText, string nextBenefit, System.Action onBuy)
        {
            GUI.Box(new Rect(x, y, w, 85), "");

            GUIStyle nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            nameStyle.normal.textColor = new Color(0.2f, 0.8f, 1f);
            GUI.Label(new Rect(x + 12, y + 8, w - 110, 24), $"{name} (Lv {currentLevel}/10)", nameStyle);

            GUIStyle infoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13
            };
            infoStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x + 12, y + 32, w - 110, 20), currentValueText, infoStyle);

            GUIStyle subInfo = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11
            };
            subInfo.normal.textColor = new Color(0.7f, 0.85f, 0.7f);
            GUI.Label(new Rect(x + 12, y + 54, w - 110, 20), nextBenefit, subInfo);

            int cost = UpgradeConfig.GetUpgradeCost(currentLevel);
            if (currentLevel >= UpgradeConfig.MaxLevel)
            {
                GUI.backgroundColor = Color.gray;
                GUI.Button(new Rect(x + w - 95, y + 20, 85, 45), "MAX");
            }
            else
            {
                bool canAfford = SaveManager.Data.coins >= cost;
                // Section 8.6: "Disabled buy state is visual only when coins are short. The button still handles the tap and shows 'Not enough coins.'"
                GUI.backgroundColor = canAfford ? new Color(0.2f, 0.85f, 0.3f) : new Color(0.6f, 0.3f, 0.3f);

                if (GUI.Button(new Rect(x + w - 95, y + 20, 85, 45), $"🪙 {cost}\nBUY"))
                {
                    if (canAfford)
                    {
                        SaveManager.Data.coins -= cost;
                        onBuy.Invoke();
                        SaveManager.Save();
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayUpgrade();
                    }
                    else
                    {
                        ShowToast("Not enough coins.");
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayPop();
                    }
                }
            }

            GUI.backgroundColor = Color.white;
        }

        // ==========================================
        // BASE PANEL (Section 8.1)
        // ==========================================
        private void DrawBasePanel()
        {
            float sw = DesignWidth;
            float sh = DesignHeight;
            float w = 320f;
            float h = 330f;
            float x = (sw - w) / 2f;
            float y = (sh - h) / 2f;

            GUI.Box(new Rect(x, y, w, h), "");

            GUIStyle title = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            title.normal.textColor = new Color(1f, 0.65f, 0.2f);
            GUI.Label(new Rect(x, y + 16, w, 35), "BASE HEADQUARTERS", title);

            // Shows bricks owned and base level
            GUIStyle infoStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 17,
                fontStyle = FontStyle.Bold
            };
            infoStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y + 65, w, 28), $"🧱 Bricks Owned: {SaveManager.Data.bricks}", infoStyle);
            GUI.Label(new Rect(x, y + 100, w, 28), $"🏰 Base Level: {SaveManager.Data.baseLevel}", infoStyle);

            int cost = UpgradeConfig.GetBaseUpgradeCost(SaveManager.Data.baseLevel);
            bool canAfford = SaveManager.Data.bricks >= cost;

            float bw = w - 50f;
            float bx = x + 25f;

            // Upgrade Base Button: "Upgrade Base spends the brick cost and raises base level by 1. Base brick costs: level 1 costs 5, then each next base level costs the previous cost + 5."
            GUI.backgroundColor = canAfford ? new Color(0.2f, 0.85f, 0.3f) : new Color(0.55f, 0.35f, 0.35f);
            if (GUI.Button(new Rect(bx, y + 155, bw, 52), $"🧱 {cost}  UPGRADE BASE"))
            {
                if (canAfford)
                {
                    SaveManager.Data.bricks -= cost;
                    SaveManager.Data.baseLevel++;
                    SaveManager.Save();
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayUpgrade();
                    ShowToast($"🏰 Base upgraded to Level {SaveManager.Data.baseLevel}!");
                }
                else
                {
                    ShowToast("Not enough bricks.");
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayPop();
                }
            }

            // Close
            GUI.backgroundColor = new Color(0.6f, 0.65f, 0.75f);
            if (GUI.Button(new Rect(bx, y + 235, bw, 44), "◀  BACK"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                ShowMainMenu();
            }

            GUI.backgroundColor = Color.white;
        }

        // ==========================================
        // 8.7 SETTINGS
        // ==========================================
        private void DrawSettings()
        {
            float sw = DesignWidth;
            float sh = DesignHeight;
            float w = 310f;
            float h = showResetConfirm ? 340f : 445f;
            float x = (sw - w) / 2f;
            float y = (sh - h) / 2f;

            GUI.Box(new Rect(x, y, w, h), "");

            GUIStyle title = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            title.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y + 14, w, 35), "SETTINGS", title);

            float bw = w - 50f;
            float bx = x + 25f;

            if (showResetConfirm)
            {
                // Reset confirmation dialog: "Ask 'Erase all progress?' Confirm deletes the save and creates the default profile. Cancel does nothing."
                GUIStyle prompt = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 17,
                    fontStyle = FontStyle.Bold
                };
                prompt.normal.textColor = new Color(1f, 0.3f, 0.3f);
                GUI.Label(new Rect(x + 10, y + 65, w - 20, 40), "Erase all progress?", prompt);

                GUI.backgroundColor = new Color(0.9f, 0.2f, 0.2f);
                if (GUI.Button(new Rect(bx, y + 130, bw, 46), "YES, ERASE ALL"))
                {
                    SaveManager.ResetSave();
                    showResetConfirm = false;
                    ShowToast("Progress erased. Default profile created.");
                    ShowMainMenu();
                }

                GUI.backgroundColor = new Color(0.6f, 0.65f, 0.75f);
                if (GUI.Button(new Rect(bx, y + 190, bw, 44), "CANCEL"))
                {
                    showResetConfirm = false;
                }

                GUI.backgroundColor = Color.white;
                return;
            }

            // 1. Music: "Toggle and save."
            bool musicOn = SaveManager.Data.musicOn;
            GUI.backgroundColor = musicOn ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.5f, 0.5f, 0.5f);
            if (GUI.Button(new Rect(bx, y + 55, bw, 40), musicOn ? "🎵 MUSIC: ON" : "🎵 MUSIC: OFF"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetMusicEnabled(!musicOn);
                }
                else
                {
                    SaveManager.Data.musicOn = !musicOn;
                    SaveManager.Save();
                }
            }

            // 2. Sound: "Toggle and save."
            bool soundOn = SaveManager.Data.soundOn;
            GUI.backgroundColor = soundOn ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.5f, 0.5f, 0.5f);
            if (GUI.Button(new Rect(bx, y + 102, bw, 40), soundOn ? "🔊 SOUND: ON" : "🔊 SOUND: OFF"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetSfxEnabled(!soundOn);
                }
                else
                {
                    SaveManager.Data.soundOn = !soundOn;
                    SaveManager.Save();
                }
            }

            // 3. Vibration: "Toggle and save. Gameplay haptic uses this flag."
            bool vibOn = SaveManager.Data.vibrationOn;
            GUI.backgroundColor = vibOn ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.5f, 0.5f, 0.5f);
            if (GUI.Button(new Rect(bx, y + 149, bw, 40), vibOn ? "📳 VIBRATION: ON" : "📳 VIBRATION: OFF"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                SaveManager.Data.vibrationOn = !vibOn;
                SaveManager.Save();
            }

            // 4. Difficulty Selector: Simple / Medium / Hard
            float dSegW = (bw - 8f) / 3f;
            GameDifficulty cDiff = SaveManager.Data.Difficulty;

            bool isS = (cDiff == GameDifficulty.Simple);
            GUI.backgroundColor = isS ? new Color(0.2f, 0.9f, 0.4f) : new Color(0.35f, 0.4f, 0.45f);
            if (GUI.Button(new Rect(bx, y + 196, dSegW, 38), isS ? "★ Simple" : "Simple"))
            {
                SaveManager.Data.Difficulty = GameDifficulty.Simple;
                SaveManager.Save();
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                ShowToast("Difficulty set to Simple");
            }

            bool isM = (cDiff == GameDifficulty.Medium);
            GUI.backgroundColor = isM ? new Color(1f, 0.8f, 0.2f) : new Color(0.35f, 0.4f, 0.45f);
            if (GUI.Button(new Rect(bx + dSegW + 4f, y + 196, dSegW, 38), isM ? "★ Medium" : "Medium"))
            {
                SaveManager.Data.Difficulty = GameDifficulty.Medium;
                SaveManager.Save();
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                ShowToast("Difficulty set to Medium");
            }

            bool isH = (cDiff == GameDifficulty.Hard);
            GUI.backgroundColor = isH ? new Color(1f, 0.3f, 0.35f) : new Color(0.35f, 0.4f, 0.45f);
            if (GUI.Button(new Rect(bx + (dSegW + 4f) * 2f, y + 196, dSegW, 38), isH ? "★ Hard" : "Hard"))
            {
                SaveManager.Data.Difficulty = GameDifficulty.Hard;
                SaveManager.Save();
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                ShowToast("Difficulty set to Hard");
            }

            // 5. Privacy: "Open the privacy URL. If the URL is empty, show 'Privacy page is not set' instead of doing nothing."
            GUI.backgroundColor = new Color(0.3f, 0.5f, 0.75f);
            if (GUI.Button(new Rect(bx, y + 243, bw, 40), "🔒 PRIVACY POLICY"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                if (string.IsNullOrEmpty(SaveManager.Data.privacyUrl))
                {
                    ShowToast("Privacy page is not set");
                }
                else
                {
                    Application.OpenURL(SaveManager.Data.privacyUrl);
                }
            }

            // 6. Reset progress: "Ask 'Erase all progress?' Confirm deletes the save and creates the default profile. Cancel does nothing."
            GUI.backgroundColor = new Color(0.85f, 0.25f, 0.25f);
            if (GUI.Button(new Rect(bx, y + 290, bw, 40), "⚠️ RESET PROGRESS"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                showResetConfirm = true;
            }

            // 7. Close: "Return to the menu."
            GUI.backgroundColor = new Color(0.6f, 0.65f, 0.75f);
            if (GUI.Button(new Rect(bx, y + 345, bw, 42), "◀  CLOSE"))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUITap();
                ShowMainMenu();
            }

            GUI.backgroundColor = Color.white;
        }
    }
}
