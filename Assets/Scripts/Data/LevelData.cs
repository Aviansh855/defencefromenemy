using System;
using System.Collections.Generic;
using UnityEngine;

namespace MobRush
{
    public enum GateModifierType
    {
        Multiply,
        Add,
        Subtract,
        Divide
    }

    [Serializable]
    public class GateConfig
    {
        public Vector3 position;
        public GateModifierType type = GateModifierType.Multiply;
        public int value = 2;
        public bool isMoving = false;
        public float moveRange = 1.6f;
        public float moveSpeed = 2f;
        public AffectsTeam affectsTeam = AffectsTeam.PlayerOnly;
    }

    [Serializable]
    public class HazardConfig
    {
        public Vector3 position;
        public Vector3 scale = new Vector3(3f, 1f, 0.5f);
        public bool isRotating = false;
        public bool isOscillating = true;
        public float speed = 2.5f;
        public float range = 2.5f;
        public int blockCost = 0;
        public AffectsTeam affectsTeam = AffectsTeam.PlayerOnly;
    }

    [Serializable]
    public class LevelRawItem
    {
        public int id;
        public string title;
        public string desc;
        public string assets;
        public int pHP;
        public int eHP;
        public int burst;
        public float interval;
        public int towers;
        public int towerHP;
        public int block;
        public int coins;
        public int bricks;
        public int shields;
        public int cards;
        public int stars;
    }

    [Serializable]
    public class LevelConfig
    {
        public int levelId;
        public string title;
        public string objectiveText;
        public float trackLength = 26f;
        public int baseHealth = 100;
        public int playerCannonHealth = 200;
        public int completionReward = 100;
        public int completionBricks = 0;
        public int completionShields = 0;
        public int completionCards = 0;
        public int completionStars = 0;

        // Towers
        public int towerCount = 0;
        public int towerHealth = 0;

        // Enemy burst & continuous spawning
        public int burstSize = 6;
        public float burstInterval = 2.5f;
        public float burstDuration = 0.15f;
        public bool enableContinuousFire = true;
        public float enemyContinuousRate = 1.5f;
        public int totalEnemiesToSpawn = 60;

        public List<GateConfig> gates = new List<GateConfig>();
        public List<HazardConfig> hazards = new List<HazardConfig>();
    }

    public static class LevelDatabase
    {
        private static Dictionary<int, LevelRawItem> cachedLevels;

        private static void LoadCache()
        {
            if (cachedLevels != null) return;
            cachedLevels = new Dictionary<int, LevelRawItem>();

            TextAsset jsonAsset = Resources.Load<TextAsset>("Levels200Data");
            if (jsonAsset != null)
            {
                // Wrap in array object for json utility or manual deserialize
                string wrapped = "{\"items\":" + jsonAsset.text + "}";
                LevelListWrapper wrapper = JsonUtility.FromJson<LevelListWrapper>(wrapped);
                if (wrapper != null && wrapper.items != null)
                {
                    foreach (var item in wrapper.items)
                    {
                        cachedLevels[item.id] = item;
                    }
                }
            }
        }

        [Serializable]
        private class LevelListWrapper
        {
            public List<LevelRawItem> items;
        }

        public static LevelConfig GetLevel(int levelId)
        {
            levelId = Mathf.Clamp(levelId, 1, 200);
            LoadCache();

            LevelRawItem raw = null;
            if (cachedLevels != null && cachedLevels.ContainsKey(levelId))
            {
                raw = cachedLevels[levelId];
            }

            // Defaults if raw missing
            int pHP = raw != null ? raw.pHP : 200;
            int eHP = raw != null ? raw.eHP : 100;
            int burst = raw != null ? raw.burst : 8;
            float interval = raw != null && raw.interval > 0 ? raw.interval : 2.5f;
            int towers = raw != null ? raw.towers : 0;
            int towerHP = raw != null ? raw.towerHP : 0;
            int blockCost = raw != null ? raw.block : 0;
            string title = raw != null ? raw.title : $"Level {levelId}";
            string desc = raw != null ? raw.desc : "Advance through the gates!";

            int coins = raw != null ? raw.coins : 100;
            int bricks = raw != null ? raw.bricks : 0;
            int shields = raw != null ? raw.shields : 0;
            int cards = raw != null ? raw.cards : 0;
            int stars = raw != null ? raw.stars : 0;

            // Apply difficulty scaling per PRD v3.1:
            // Simple: Enemy burst = 60% of Medium (min 4); Enemy/Tower HP = 70%; Player cannon HP = 125%; Block cost = 50%
            // Medium: 100%
            // Hard: Enemy burst = 135% of Medium (max 24); Enemy/Tower HP = 140%; Player cannon HP = 85%; Block cost = 150%
            // Burst capped at 24 across all settings.
            GameDifficulty diff = SaveManager.Data != null ? SaveManager.Data.Difficulty : GameDifficulty.Medium;

            if (diff == GameDifficulty.Simple)
            {
                burst = Mathf.Clamp(Mathf.RoundToInt(burst * 0.60f), 4, 24);
                eHP = Mathf.RoundToInt(eHP * 0.70f);
                pHP = Mathf.RoundToInt(pHP * 1.25f);
                towerHP = Mathf.RoundToInt(towerHP * 0.70f);
                blockCost = Mathf.RoundToInt(blockCost * 0.50f);
            }
            else if (diff == GameDifficulty.Hard)
            {
                burst = Mathf.Clamp(Mathf.RoundToInt(burst * 1.35f), 1, 24);
                eHP = Mathf.RoundToInt(eHP * 1.40f);
                pHP = Mathf.RoundToInt(pHP * 0.85f);
                towerHP = Mathf.RoundToInt(towerHP * 1.40f);
                blockCost = Mathf.RoundToInt(blockCost * 1.50f);
            }
            else
            {
                burst = Mathf.Clamp(burst, 1, 24);
            }

            LevelConfig config = new LevelConfig
            {
                levelId = levelId,
                title = $"Level {levelId} — {title}",
                objectiveText = desc,
                trackLength = 36f + Mathf.Min(14f, levelId * 0.1f),
                playerCannonHealth = Mathf.Max(200, pHP),
                baseHealth = Mathf.Max(200, eHP * 5),
                towerCount = towers,
                towerHealth = Mathf.Max(50, towerHP * 2),
                burstSize = Mathf.Max(6, burst),
                burstInterval = Mathf.Clamp(interval * 0.6f, 1.2f, 2.5f),
                burstDuration = 0.15f,
                enableContinuousFire = true,
                enemyContinuousRate = Mathf.Clamp(2.0f + (levelId * 0.02f), 2.0f, 4.0f),
                totalEnemiesToSpawn = 80 + levelId * 5,
                completionReward = coins,
                completionBricks = Mathf.Max(25, bricks),
                completionShields = shields,
                completionCards = cards,
                completionStars = stars
            };

            // Setup level layout (Gates and Hazards / PlayerOnly objects)
            SetupLevelLayout(config, levelId, blockCost);

            return config;
        }

        private static void SetupLevelLayout(LevelConfig config, int levelId, int blockCost)
        {
            // Level 1: Meadow Dawn (Opener: one x2 gate)
            if (levelId == 1)
            {
                config.gates.Add(new GateConfig { position = new Vector3(0f, 1f, 12.0f), type = GateModifierType.Multiply, value = 2 });
                return;
            }

            // Level 2: Meadow Dusk (Choice: Safe x2 or -30 Breaker Gate that turns into moving x5!)
            if (levelId == 2)
            {
                config.gates.Add(new GateConfig { position = new Vector3(-1.8f, 1f, 12.0f), type = GateModifierType.Multiply, value = 2 });
                config.gates.Add(new GateConfig { position = new Vector3(1.8f, 1f, 12.0f), type = GateModifierType.Subtract, value = 30 });
                return;
            }

            // Level 3: Meadow Fog (Safe x2 or -40 Breaker Gate)
            if (levelId == 3)
            {
                config.gates.Add(new GateConfig { position = new Vector3(-1.8f, 1f, 12.0f), type = GateModifierType.Multiply, value = 2 });
                config.gates.Add(new GateConfig { position = new Vector3(1.8f, 1f, 12.0f), type = GateModifierType.Subtract, value = 40 });
                return;
            }

            // Level 4: Meadow Rain (Safe x2 or -50 Breaker Gate -> Moving x5; no unfair player-killing hazard bars)
            if (levelId == 4)
            {
                config.gates.Add(new GateConfig { position = new Vector3(-1.8f, 1f, 12.0f), type = GateModifierType.Multiply, value = 2 });
                config.gates.Add(new GateConfig { position = new Vector3(1.8f, 1f, 12.0f), type = GateModifierType.Subtract, value = 50 });
                return;
            }

            // Level 5: Meadow Gold (Safe x2 or -65 Breaker Gate)
            if (levelId == 5)
            {
                config.gates.Add(new GateConfig { position = new Vector3(-1.8f, 1f, 12.0f), type = GateModifierType.Multiply, value = 2 });
                config.gates.Add(new GateConfig { position = new Vector3(1.8f, 1f, 12.0f), type = GateModifierType.Subtract, value = 65 });
                return;
            }

            // Level 6: Meadow Night (Moving x2 or -80 Breaker Gate -> Moving x5)
            if (levelId == 6)
            {
                config.gates.Add(new GateConfig { position = new Vector3(-1.8f, 1f, 12.0f), type = GateModifierType.Multiply, value = 2, isMoving = true, moveRange = 1.2f, moveSpeed = 1.8f });
                config.gates.Add(new GateConfig { position = new Vector3(1.8f, 1f, 12.0f), type = GateModifierType.Subtract, value = 80 });
                return;
            }

            // General 200 level progression layout generator
            // Left safe lane gate
            int gateVal = (levelId % 5 == 0) ? 3 : 2;
            bool leftMoving = (levelId % 3 == 0);
            config.gates.Add(new GateConfig
            {
                position = new Vector3(-1.8f, 1f, 12.0f),
                type = GateModifierType.Multiply,
                value = gateVal,
                isMoving = leftMoving,
                moveRange = 1.3f,
                moveSpeed = 1.9f
            });

            // Right lane: iconic -99 Breaker Gate that explodes into moving x5 on breakthrough!
            int subVal = Mathf.Clamp(30 + levelId * 2, 30, 99);
            config.gates.Add(new GateConfig
            {
                position = new Vector3(1.8f, 1f, 12.0f),
                type = GateModifierType.Subtract,
                value = subVal
            });

            // Second gate row for higher levels
            if (levelId >= 10)
            {
                config.gates.Add(new GateConfig
                {
                    position = new Vector3(0f, 1f, 22.0f),
                    type = GateModifierType.Multiply,
                    value = (levelId % 10 == 0) ? 4 : 3,
                    isMoving = true,
                    moveRange = 1.6f,
                    moveSpeed = 2.2f
                });
            }
        }
    }
}
