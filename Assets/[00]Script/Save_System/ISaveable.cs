using UnityEngine;
using System;

public interface ISaveable
{
    void SaveTo(GameSaveData saveData);
    void LoadFrom(GameSaveData saveData);

}