using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wuxing.Localization
{
    public class LocalizationManager : MonoBehaviour
    {
        private const string LanguagePrefKey = "game.language";
        private const string TableResourcePath = "Localization/GameText";

        private readonly Dictionary<string, LocalizationEntry> entries = new Dictionary<string, LocalizationEntry>();

        public static LocalizationManager Instance { get; private set; }

        public static event Action LanguageChanged;

        public GameLanguage CurrentLanguage { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadTable();
            LoadLanguage();
        }

        public static string GetText(string key)
        {
            if (Instance == null)
            {
                return key;
            }

            return Instance.ResolveText(key);
        }

        public static void SetLanguage(GameLanguage language)
        {
            if (Instance == null)
            {
                return;
            }

            if (Instance.CurrentLanguage == language)
            {
                return;
            }

            Instance.CurrentLanguage = language;
            PlayerPrefs.SetInt(LanguagePrefKey, (int)language);
            PlayerPrefs.Save();
            LanguageChanged?.Invoke();
        }

        public static void ToggleLanguage()
        {
            if (Instance == null)
            {
                return;
            }

            var nextLanguage = Instance.CurrentLanguage == GameLanguage.ChineseSimplified
                ? GameLanguage.English
                : GameLanguage.ChineseSimplified;

            SetLanguage(nextLanguage);
        }

        private void LoadTable()
        {
            entries.Clear();

            var textAsset = Resources.Load<TextAsset>(TableResourcePath);
            if (textAsset == null)
            {
                Debug.LogError("Localization table not found at Resources/Localization/GameText.json");
                return;
            }

            var table = JsonUtility.FromJson<LocalizationTable>(textAsset.text);
            if (table == null || table.entries == null)
            {
                Debug.LogError("Localization table is empty or invalid.");
                return;
            }

            for (var i = 0; i < table.entries.Length; i++)
            {
                var entry = table.entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.key))
                {
                    continue;
                }

                entries[entry.key] = entry;
            }
        }

        private void LoadLanguage()
        {
            var savedLanguage = PlayerPrefs.GetInt(LanguagePrefKey, (int)GameLanguage.ChineseSimplified);
            CurrentLanguage = (GameLanguage)savedLanguage;
        }

        private string ResolveText(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            LocalizationEntry entry;
            if (!entries.TryGetValue(key, out entry))
            {
                return key;
            }

            switch (CurrentLanguage)
            {
                case GameLanguage.English:
                    return string.IsNullOrEmpty(entry.en) ? entry.zhHans : entry.en;
                case GameLanguage.ChineseSimplified:
                default:
                    return string.IsNullOrEmpty(entry.zhHans) ? entry.en : entry.zhHans;
            }
        }
    }
}

