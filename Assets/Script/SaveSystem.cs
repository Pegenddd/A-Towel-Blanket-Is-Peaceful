using System;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public string sceneName = "First";
    public int dialogueIndex = 0;
    public string saveDateFormatted = "";
    public string previewText = "";
    public long timestamp = 0;
}

public static class SaveSystem
{
    private const string SAVE_KEY = "TowelBlanket_SaveData";

    public static bool HasPendingResume { get; private set; } = false;
    public static int PendingDialogueIndex { get; private set; } = 0;

    public static string SaveFilePath => System.IO.Path.Combine(Application.persistentDataPath, "savegame.json");

    public static void SaveGame(string sceneName, int dialogueIndex, string previewText = "")
    {
        SaveData data = new SaveData
        {
            sceneName = string.IsNullOrEmpty(sceneName) ? "First" : sceneName,
            dialogueIndex = Mathf.Max(0, dialogueIndex),
            saveDateFormatted = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            previewText = previewText,
            timestamp = DateTime.UtcNow.Ticks
        };

        string json = JsonUtility.ToJson(data, true);

        // 1. Save to PlayerPrefs (Windows Registry)
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();

        // 2. Save to JSON File in persistentDataPath
        try
        {
            System.IO.File.WriteAllText(SaveFilePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveSystem] Could not write file to {SaveFilePath}: {ex.Message}");
        }

        Debug.Log($"[SaveSystem] Game saved successfully!\n- Path: {SaveFilePath}\n- Scene: {data.sceneName}, Dialogue Index: {data.dialogueIndex}, Date: {data.saveDateFormatted}");
    }

    public static SaveData LoadGame()
    {
        if (!HasSaveData())
        {
            return null;
        }

        string json = "";

        // Priority 1: Load from JSON File if available
        if (System.IO.File.Exists(SaveFilePath))
        {
            try
            {
                json = System.IO.File.ReadAllText(SaveFilePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Error reading file {SaveFilePath}: {ex.Message}");
            }
        }

        // Priority 2: Fallback to PlayerPrefs
        if (string.IsNullOrEmpty(json))
        {
            json = PlayerPrefs.GetString(SAVE_KEY, "");
        }

        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveSystem] Error parsing save data: {ex.Message}");
            return null;
        }
    }

    public static bool HasSaveData()
    {
        if (System.IO.File.Exists(SaveFilePath))
        {
            return true;
        }
        return PlayerPrefs.HasKey(SAVE_KEY) && !string.IsNullOrEmpty(PlayerPrefs.GetString(SAVE_KEY, ""));
    }

    public static void ClearSaveData()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();

        try
        {
            if (System.IO.File.Exists(SaveFilePath))
            {
                System.IO.File.Delete(SaveFilePath);
            }
        }
        catch { }

        HasPendingResume = false;
        PendingDialogueIndex = 0;
        Debug.Log("[SaveSystem] Save data cleared.");
    }

    public static void PrepareResume(int dialogueIndex)
    {
        HasPendingResume = true;
        PendingDialogueIndex = dialogueIndex;
    }

    public static void ClearPendingResume()
    {
        HasPendingResume = false;
        PendingDialogueIndex = 0;
    }
}
