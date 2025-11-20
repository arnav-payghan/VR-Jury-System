// UITranscriptionPanel.cs
using UnityEngine;
using UnityEngine.UI;

public class UITranscriptionPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text promptText;
    [SerializeField] private Text transcriptionText;
    [SerializeField] private Button confirmButton;

    private void Start()
    {
        if (panel != null)
            panel.SetActive(false);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    public void ShowRecordingPrompt(string prompt)
    {
        if (panel != null)
        {
            panel.SetActive(true);
            if (promptText != null)
                promptText.text = prompt;

            if (transcriptionText != null)
                transcriptionText.text = "Press trigger to record...";

            if (confirmButton != null)
                confirmButton.gameObject.SetActive(false);
        }
    }

    public void DisplayTranscription(string text)
    {
        if (transcriptionText != null)
            transcriptionText.text = text;

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(true);
    }

    private void OnConfirmClicked()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}

