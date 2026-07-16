using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class GameSaveData
{
    public GameMetaData meta = new GameMetaData();
    public PlayerData player = new PlayerData();
    
}

[Serializable]
public class GameMetaData
{
    public string saveDate;      // ISO string, e.g. DateTime.Now.ToString("o")
    public float playTimeSeconds;
    public int saveVersion = 1;  // useful later for migrating old saves
}
