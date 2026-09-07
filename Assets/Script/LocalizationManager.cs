using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum Language
{
    English = 0,
    Thai = 1
}

public static class LocalizationManager
{
    private const string LANGUAGE_KEY = "TowelBlanket_Language";
    public static event Action OnLanguageChanged;

    private static Language currentLanguage = Language.English;
    private static bool isInitialized = false;

    private static readonly Dictionary<string, string> englishDict = new Dictionary<string, string>
    {
        { "menu_title", "A Towel Blanket Is Peaceful" },
        { "menu_subtitle", "An Interactive Visual Novel" },
        { "menu_play", "Play" },
        { "menu_continue", "Continue" },
        { "menu_settings", "Settings" },
        { "menu_quit", "Quit" },
        { "menu_no_save", "No save data found" },
        { "menu_resume_hint", "Saved: {0} ({1})" },

        { "settings_title", "Settings" },
        { "settings_audio_header", "Audio Settings" },
        { "settings_master", "Master Volume" },
        { "settings_bgm", "Music (BGM)" },
        { "settings_sfx", "Sound Effects (SFX)" },
        { "settings_lang_header", "Language & Text" },
        { "settings_language", "Language" },
        { "settings_font_size", "Text Size" },
        { "settings_lang_en", "English" },
        { "settings_lang_th", "ภาษาไทย" },
        { "settings_back", "Back" },

        { "pause_title", "Paused" },
        { "pause_resume", "Resume" },
        { "pause_save", "Save Game" },
        { "pause_saved_toast", "Game Saved Successfully!" },
        { "pause_settings", "Settings" },
        { "pause_main_menu", "Main Menu" },
        { "pause_button_hud", "Menu / Save" },

        { "story_intro_quote", "At that time, when Adam and Eve bit into the fruit of wisdom, we fell from the Garden of Eden forever -----------------------------------------------------------------" },
        { "story_intro_hint", "(Click to start)" },
        { "story_ending_text", "The curtains still flutter overhead." },
        { "story_ending_return", "Click to return to Main Menu" }
    };

    private static readonly Dictionary<string, string> thaiDict = new Dictionary<string, string>
    {
        { "menu_title", "A Towel Blanket Is Peaceful" },
        { "menu_subtitle", "นิยายภาพเชิงโต้ตอบ" },
        { "menu_play", "เล่น" },
        { "menu_continue", "ดำเนินการต่อ" },
        { "menu_settings", "ตั้งค่า" },
        { "menu_quit", "ออกจากเกม" },
        { "menu_no_save", "ไม่พบข้อมูลบันทึก" },
        { "menu_resume_hint", "บันทึก: {0} ({1})" },

        { "settings_title", "ตั้งค่า" },
        { "settings_audio_header", "ตั้งค่าระดับเสียง" },
        { "settings_master", "ระดับเสียงหลัก" },
        { "settings_bgm", "เสียงดนตรี (BGM)" },
        { "settings_sfx", "เสียงเอฟเฟกต์ (SFX)" },
        { "settings_lang_header", "ภาษาและขนาดข้อความ" },
        { "settings_language", "ภาษา" },
        { "settings_font_size", "ขนาดตัวอักษร" },
        { "settings_lang_en", "English" },
        { "settings_lang_th", "ภาษาไทย" },
        { "settings_back", "ย้อนกลับ" },

        { "pause_title", "หยุดชั่วคราว" },
        { "pause_resume", "เล่นต่อ" },
        { "pause_save", "บันทึกเกม" },
        { "pause_saved_toast", "บันทึกเกมเรียบร้อยแล้ว!" },
        { "pause_settings", "ตั้งค่า" },
        { "pause_main_menu", "หน้าหลัก" },
        { "pause_button_hud", "เมนู / บันทึก" },

        { "story_intro_quote", "ยามนั้นเมื่ออดัมและอีฟกัดผลแห่งปัญญา พวกเราก็ร่วงหล่นจากสวนอีเดนตลอดกาล -----------------------------------------------------------------" },
        { "story_intro_hint", "(คลิกเพื่อเริ่มเรื่อง)" },
        { "story_ending_text", "ผ้าม่านยังคงปลิวไสวอยู่เหนือหัว" },
        { "story_ending_return", "คลิกเพื่อกลับสู่หน้าหลัก" }
    };

    public static Language CurrentLanguage
    {
        get
        {
            EnsureInitialized();
            return currentLanguage;
        }
    }

    public static void EnsureInitialized()
    {
        if (isInitialized) return;

        int savedLang = PlayerPrefs.GetInt(LANGUAGE_KEY, (int)Language.English);
        currentLanguage = Enum.IsDefined(typeof(Language), savedLang) ? (Language)savedLang : Language.English;
        isInitialized = true;
    }

    public static void SetLanguage(Language lang)
    {
        EnsureInitialized();
        if (currentLanguage == lang) return;

        currentLanguage = lang;
        PlayerPrefs.SetInt(LANGUAGE_KEY, (int)currentLanguage);
        PlayerPrefs.Save();

        Debug.Log($"[LocalizationManager] Language changed to: {currentLanguage}");
        OnLanguageChanged?.Invoke();
    }

    public static string Get(string key, params object[] args)
    {
        EnsureInitialized();

        Dictionary<string, string> dict = (currentLanguage == Language.Thai) ? thaiDict : englishDict;

        if (dict.TryGetValue(key, out string val))
        {
            string result = val;
            if (args != null && args.Length > 0)
            {
                try
                {
                    result = string.Format(val, args);
                }
                catch
                {
                    result = val;
                }
            }

            if (currentLanguage == Language.Thai)
            {
                result = ThaiFontAdjuster.Adjust(result);
            }
            return result;
        }

        if (englishDict.TryGetValue(key, out string fallback))
        {
            if (args != null && args.Length > 0)
            {
                try { return string.Format(fallback, args); } catch { return fallback; }
            }
            return fallback;
        }

        return key;
    }
}
