using UnityEngine;
using UnityEngine.UI;

public class UIRoundIndicator : MonoBehaviour
{
    [SerializeField] private Text roundText;

    private void Start()
    {
        if (TrialRoundManager.Instance != null)
        {
            TrialRoundManager.Instance.OnRoundStart.AddListener(UpdateRoundDisplay);
        }
    }

    private void UpdateRoundDisplay(int round)
    {
        if (roundText != null)
        {
            int maxRounds = TrialRoundManager.Instance != null ?
                TrialRoundManager.Instance.GetMaxRounds() : 3;
            roundText.text = $"Round {round}/{maxRounds}";
        }
    }
}