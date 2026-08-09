using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class LevelUp_building : MonoBehaviour
{
    public bool canUpgrade;
    void Awake()
    {
        canUpgrade = false;
        Debug.Log($"canUpgrade is {canUpgrade}");
    }

    public void OnTriggerEnter(Collider other)
    {
        canUpgrade = true;
        Debug.Log($"Area upgrade has triggered and canUpgrade is {canUpgrade}");
    }
    
    public void OnTriggerExit(Collider other)
    {
        canUpgrade = false;
        Debug.Log($"canUpgrade is {canUpgrade}");
    }
}