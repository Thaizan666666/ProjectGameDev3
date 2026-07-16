using UnityEngine;
using System.IO;
using System;

public static class SaveSystem
{

    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static void Save (GameSaveData data)
    {
        // data.meta.saveDate = DateTime.Now.ToString("o");     save date today in real life to file "save.json"
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"Saved to: {SavePath}");
    }

    public static GameSaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("No save file found.");
            return new GameSaveData();
        }
        else
        {
            string json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<GameSaveData>(json);
        }
    }

    public static bool SaveExists() => File.Exists(SavePath);

    public static void DeleteSave()
    {
        if(File.Exists(SavePath)) File.Delete(SavePath);
    }
}
