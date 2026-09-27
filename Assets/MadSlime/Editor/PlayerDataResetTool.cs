using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PlayerDataResetTool
{
    private const string EditorSavesPath = "Assets/PluginYourGames/Editor/SavesEditorYG2.json";
    private const int TestBalance = 9999999;

    [MenuItem("Mad Slime/Reset Player Data")]
    public static void ResetPlayerData()
    {
        if (EditorApplication.isPlaying == true)
        {
            Debug.LogWarning("[PlayerDataResetTool] Exit play mode first — the running session would overwrite the reset.");
            return;
        }

        if (File.Exists(EditorSavesPath) == false)
        {
            throw new InvalidOperationException($"Editor saves file not found: {EditorSavesPath}");
        }

        string json = File.ReadAllText(EditorSavesPath);

        json = ReplaceNumber(json, "CurrentLevel", 1);
        json = ReplaceNumber(json, "MaxLevel", 1);
        json = ReplaceNumber(json, "Balance", TestBalance);
        json = ReplaceNumber(json, "SelectedSkinType", 0);
        json = ReplaceArray(json, "_openSkins", "[0]");
        json = ReplaceNumber(json, "SpeedLevel", 0);
        json = ReplaceNumber(json, "AppetiteLevel", 0);
        json = ReplaceNumber(json, "TasteLevel", 0);
        json = ReplaceNumber(json, "MetabolismLevel", 0);
        json = ReplaceArray(json, "PurchasedPerks", "[]");
        json = ReplaceNumber(json, "LastFreeSpinUnixTime", 0);
        json = ReplaceArray(json, "RouletteAdSpinTimes", "[]");
        json = ReplaceNumber(json, "SkinSpinCount", 0);
        json = ReplaceString(json, "PreviousScene", "Menu");

        File.WriteAllText(EditorSavesPath, json);
        AssetDatabase.Refresh();

        Debug.Log($"[PlayerDataResetTool] progress reset, balance set to {TestBalance}");
    }

    private static string ReplaceNumber(string json, string field, int value)
    {
        string pattern = "\"" + field + "\":\\s*-?[0-9]+";
        string replacement = "\"" + field + "\": " + value;

        if (System.Text.RegularExpressions.Regex.IsMatch(json, pattern) == false)
        {
            throw new InvalidOperationException($"Field '{field}' not found in editor saves.");
        }

        return System.Text.RegularExpressions.Regex.Replace(json, pattern, replacement);
    }

    private static string ReplaceArray(string json, string field, string value)
    {
        string pattern = "\"" + field + "\":\\s*\\[[^\\]]*\\]";
        string replacement = "\"" + field + "\": " + value;

        if (System.Text.RegularExpressions.Regex.IsMatch(json, pattern) == false)
        {
            throw new InvalidOperationException($"Field '{field}' not found in editor saves.");
        }

        return System.Text.RegularExpressions.Regex.Replace(json, pattern, replacement);
    }

    private static string ReplaceString(string json, string field, string value)
    {
        string pattern = "\"" + field + "\":\\s*\"[^\"]*\"";
        string replacement = "\"" + field + "\": \"" + value + "\"";

        if (System.Text.RegularExpressions.Regex.IsMatch(json, pattern) == false)
        {
            throw new InvalidOperationException($"Field '{field}' not found in editor saves.");
        }

        return System.Text.RegularExpressions.Regex.Replace(json, pattern, replacement);
    }
}
