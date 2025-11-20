using UnityEngine;

// Simple case data - no ScriptableObject needed, just a plain class
[System.Serializable]
public class CaseData
{
    public string caseTitle;
    public string charge;
    [TextArea(3, 6)]
    public string summary;
    public string defendantName;
    public string victimName;

    // Constructor for easy creation
    public CaseData(string title, string charge, string summary, string defendant, string victim)
    {
        this.caseTitle = title;
        this.charge = charge;
        this.summary = summary;
        this.defendantName = defendant;
        this.victimName = victim;
    }

    // Get full context for AI
    public string GetFullContext()
    {
        return $"Case: {caseTitle}\nCharge: {charge}\nDefendant: {defendantName}\nVictim: {victimName}\n\nSummary: {summary}";
    }
}