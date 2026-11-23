using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MobRush
{
    public static class SaveManager
    {
        private static readonly string SaveFileName = "mobrush_save.json";
        private static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        private static PlayerData currentData;

        public static PlayerData Data
        {
            get
            {
                if (currentData == null)
                {
                    Load();
                }
                return currentData;
            }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    currentData = JsonUtility.FromJson<PlayerData>(json);
                }
                else if (PlayerPrefs.HasKey("MobRush_Save"))
                {
                    string json = PlayerPrefs.GetString("MobRush_Save");
                    currentData = JsonUtility.FromJson<PlayerData>(json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Failed to read save file: {ex.Message}. Using default profile.");
            }

            if (currentData == null)
            {
                currentData = new PlayerData();
                Save();
            }
            else
            {
                // Ensure null lists or backward compatibility are handled
                if (currentData.tutorialStepsDone == null)
                {
                    currentData.tutorialStepsDone = new List<string>();
                }

                if (currentData.saveVersion < 2)
                {
                    currentData.saveVersion = 2;
                    Save();
                }
            }
        }

        public static void Save()
        {
            if (currentData == null) return;

            try
            {
                string json = JsonUtility.ToJson(currentData, true);
                File.WriteAllText(SaveFilePath, json);
                PlayerPrefs.SetString("MobRush_Save", json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to save game data: {ex.Message}");
            }
        }

        public static void ResetSave()
        {
            currentData = new PlayerData();
            Save();
        }
    }
}
