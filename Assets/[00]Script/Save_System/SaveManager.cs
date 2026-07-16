using UnityEngine;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance {get; private set;}

    private List<ISaveable> saveables = new List<ISaveable>();

    void Awake()
    {
        Instance = this;
    }

    public void Register(ISaveable saveable) => saveables.Add(saveable);
    public void Unregister(ISaveable saveable) => saveables.Remove(saveable);

    public void SaveGame()
    {
        GameSaveData data = new GameSaveData();
        foreach (var s in saveables)
        {
            s.SaveTo(data);
        }

        SaveSystem.Save(data);
    }

    public void LoadGame()
    {
        GameSaveData data = SaveSystem.Load();
        foreach (var s in saveables)
        {
            s.LoadFrom(data);
        }
    }
}