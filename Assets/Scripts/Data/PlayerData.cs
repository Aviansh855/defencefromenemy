using System;
using System.Collections.Generic;

namespace MobRush
{
    public enum GameDifficulty
    {
        Simple = 0,
        Medium = 1,
        Hard = 2
    }

    [Serializable]
    public class PlayerData
    {
        public int saveVersion = 2;
        public int coins = 0;
        public int bricks = 0;
        public int shields = 0;
        public int leagueStars = 0;
        public int unlockedLevel = 1;
        public int currentLevel = 1;

        // Difficulty: 0 = Simple, 1 = Medium, 2 = Hard (Default is Medium = 1 per PRD v3.1)
        public int difficulty = 1;

        public GameDifficulty Difficulty
        {
            get => (GameDifficulty)Math.Clamp(difficulty, 0, 2);
            set => difficulty = (int)value;
        }

        // Upgrade levels (0 to 10)
        public int fireRateLevel = 0;
        public int strengthLevel = 0;
        public int startingPushLevel = 0;
        public int baseLevel = 0;

        // Backward compatibility aliases
        public int unitStrengthLevel
        {
            get => strengthLevel;
            set => strengthLevel = value;
        }

        public int startingCrowdLevel
        {
            get => startingPushLevel;
            set => startingPushLevel = value;
        }

        // Champion
        public bool championUnlocked = false;

        // Tutorial steps completed
        public List<string> tutorialStepsDone = new List<string>();

        // Daily Reward
        public string dailyClaimDate = "";
        public int dailyStreak = 0;

        // Settings
        public bool musicOn = true;
        public bool soundOn = true;
        public bool vibrationOn = true;

        // Rewards tracking
        public string lastRewardAttemptId = "";
        public string privacyUrl = "";

        // Backward compatibility getters/setters if referenced
        public bool sfxEnabled
        {
            get => soundOn;
            set => soundOn = value;
        }

        public bool musicEnabled
        {
            get => musicOn;
            set => musicOn = value;
        }

        public bool vibrationEnabled
        {
            get => vibrationOn;
            set => vibrationOn = value;
        }
    }
}
