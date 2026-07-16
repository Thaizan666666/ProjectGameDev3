using UnityEngine;

public class Player : MonoBehaviour, ISaveable
{
    public int money = 0;
    public string playerName = "A";

    void Start()
    {
        Debug.Log(Application.persistentDataPath);
    }

    void OnEnable()  => SaveManager.Instance.Register(this);
    void OnDisable() => SaveManager.Instance.Unregister(this);

    public void SaveTo(GameSaveData saveData)
    {
        saveData.player.money = money;
        saveData.player.playerName = playerName;
        // other thing we want to save about player. Implement below.

    }

    public void LoadFrom(GameSaveData saveData)
    {
        money = saveData.player.money;
        playerName = saveData.player.playerName;
        // call data from "PlayerData.cs" implement below.

    }

}
