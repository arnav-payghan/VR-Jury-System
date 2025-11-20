using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UICaseSelectionPanel : MonoBehaviour
{
    [SerializeField] private Dropdown caseDropdown;
    [SerializeField] private Text caseSummaryText;
    [SerializeField] private Button startTrialButton;

    private List<CaseData> availableCases;
    private CaseData selectedCase;

    private void Start()
    {
        // Create our one case
        availableCases = new List<CaseData>
        {
            new CaseData(
                "State v. Michael J. Hawthorne",
                "First-Degree Murder",
                "On Oct 18, 2025, Emily R. Lawson was found dead in her apartment from blunt force trauma. A blood-stained bat containing partial prints linked to Michael J. Hawthorne was recovered. A witness claims they saw a hooded figure near the residence around 12:15 AM. Prior threatening emails and a work dispute suggest possible motive. The defendant claims innocence and states the prints came from a previous visit.",
                "Michael J. Hawthorne",
                "Emily R. Lawson"
            )
        };

        SetupDropdown();

        if (startTrialButton != null)
            startTrialButton.onClick.AddListener(OnStartTrialClicked);
    }

    private void SetupDropdown()
    {
        if (caseDropdown == null) return;

        caseDropdown.ClearOptions();

        List<string> caseNames = new List<string>();
        foreach (var c in availableCases)
        {
            caseNames.Add(c.caseTitle);
        }

        caseDropdown.AddOptions(caseNames);
        caseDropdown.onValueChanged.AddListener(OnCaseSelected);

        // Auto-select first case
        OnCaseSelected(0);
    }

    private void OnCaseSelected(int index)
    {
        if (index >= 0 && index < availableCases.Count)
        {
            selectedCase = availableCases[index];

            if (caseSummaryText != null)
            {
                caseSummaryText.text = $"<b>{selectedCase.caseTitle}</b>\n\n{selectedCase.summary}";
            }

            Debug.Log($"Selected case: {selectedCase.caseTitle}");
        }
    }

    private void OnStartTrialClicked()
    {
        if (selectedCase == null)
        {
            Debug.LogWarning("No case selected!");
            return;
        }

        // Store globally
        CurrentCaseStore.SelectedCase = selectedCase;
        Debug.Log($"[CaseSelection] Chosen Case: {selectedCase.caseTitle}");

        // Try to start trial if manager exists
        var manager = FindFirstObjectByType<TrialRoundManager>();
        if (manager != null)
        {
            try { manager.StartTrial(); } catch { }
        }

        gameObject.SetActive(false);
    }


}
