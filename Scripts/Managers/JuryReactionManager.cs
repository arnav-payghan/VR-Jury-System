using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JuryReactionManager : MonoBehaviour
{
    [Header("Jury Members")]
    [SerializeField] private List<JuryMember> juryMembers = new List<JuryMember>();

    [Header("Verdict Display")]
    [SerializeField] private GameObject verdictPanel;
    [SerializeField] private UnityEngine.UI.Text verdictText;

    private void Start()
    {
        if (verdictPanel != null)
            verdictPanel.SetActive(false);
    }

    // React to an argument based on sentiment
    public void ReactToArgument(SentimentResult sentiment, bool isDefense)
    {
        foreach (JuryMember juror in juryMembers)
        {
            juror.ShowReaction(sentiment, isDefense);
        }
    }

    public void ShowFinalVerdict(string verdict, string defendantName)
    {
        if (verdictPanel != null)
        {
            verdictPanel.SetActive(true);
            if (verdictText != null)
                verdictText.text = $"{defendantName}\n\n{verdict}";
        }

        // All jurors show final reaction
        foreach (JuryMember juror in juryMembers)
        {
            juror.ShowVerdictReaction(verdict);
        }
    }

    public void ResetJury()
    {
        foreach (JuryMember juror in juryMembers)
        {
            juror.ResetReaction();
        }

        if (verdictPanel != null)
            verdictPanel.SetActive(false);
    }
}

