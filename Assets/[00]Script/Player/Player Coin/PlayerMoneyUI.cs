using UnityEngine;
using TMPro;

public class PlayerMoneyUI : MonoBehaviour
{
    [SerializeField] private PlayerWallet playerWallet;
    [SerializeField] private TextMeshProUGUI coinText;
    private void OnEnable()
    {
        playerWallet.OnMoneyChanged += UpdateMoneyUI;
    }

    private void Osable()
    {
        playerWallet.OnMoneyChanged -= UpdateMoneyUI;
    }

    private void UpdateMoneyUI(int money)
    {
        coinText.text = $"{money}";
    }
}
