using UnityEngine;
using UnityEngine.UI;
using TMPro; // If you’re using TextMeshPro (recommended)

public class RoundManager : MonoBehaviour
{
    public int currentRound = 0;

    [Header("UI References")]
    public GameObject roundScreen;       // Panel that shows "Round #"
    public TMP_Text roundText;           // Text that says "Round #"
    public GameObject nextRoundScreen;   // Panel for "Go to next round"
    public Button nextRoundButton;       // Button to trigger next round

    private void Start()
    {
        // Hide UI at start
        roundScreen.SetActive(false);
        nextRoundScreen.SetActive(true);

        // Hook up button
        nextRoundButton.onClick.AddListener(StartNextRound);
    }

    public void StartNextRound()
    {
        currentRound++;
        ShowRoundScreen();
    }

    private void ShowRoundScreen()
    {
        // Update the round text
        roundText.text = "Round " + currentRound;

        // Show round UI for a few seconds
        roundScreen.SetActive(true);
        nextRoundScreen.SetActive(false);

        // Hide it after 2 seconds and show the "next round" screen
        Invoke(nameof(HideRoundScreen), 2f);
    }

    private void HideRoundScreen()
    {
        roundScreen.SetActive(false);
        nextRoundScreen.SetActive(true);
    }
}
