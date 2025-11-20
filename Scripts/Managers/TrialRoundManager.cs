using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TrialRoundManager : MonoBehaviour
{
    [Header("Trial Configuration")]
    [SerializeField] private int maxRounds = 3;

    // Selected case from the UI dropdown
    private CaseData currentCase;
    // Used when prompting the AI (built from currentCase)
    private string caseDescription = "";

    [Header("References")]
    [SerializeField] private JuryReactionManager juryManager;
    [SerializeField] private UITranscriptionPanel transcriptionPanel;
    [SerializeField] private UIOpposingLawyerPanel opposingLawyerPanel;

    [Header("Events")]
    public UnityEvent<int> OnRoundStart;
    public UnityEvent<string> OnVerdictReached;

    private int currentRound = 0;
    private float cumulativeDefenseSentiment = 0f;
    private float cumulativeProsecutionSentiment = 0f;
    private readonly List<RoundData> roundHistory = new List<RoundData>();

    private bool waitingForPlayer = false;
    private bool processingAI = false;

    public static TrialRoundManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // IMPORTANT: Do NOT auto-start the trial here.
    // The UI CaseSelectionPanel will call SetCurrentCase() then StartTrial().
    private void Start() { }

    // Called by Case Selection UI AFTER SetCurrentCase()
    public void StartTrial()
    {
        currentRound = 0;
        cumulativeDefenseSentiment = 0f;
        cumulativeProsecutionSentiment = 0f;
        roundHistory.Clear();

        StartNextRound();
    }

    private void StartNextRound()
    {
        currentRound++;

        if (currentRound > maxRounds)
        {
            CalculateFinalVerdict();
            return;
        }

        Debug.Log($"Starting Round {currentRound}");
        OnRoundStart?.Invoke(currentRound);

        waitingForPlayer = true;
        if (transcriptionPanel != null)
            transcriptionPanel.ShowRecordingPrompt($"Round {currentRound}: Present your argument");
    }

    // Called when player finishes their argument
    public void OnPlayerArgumentComplete(string transcribedText)
    {
        if (!waitingForPlayer) return;

        waitingForPlayer = false;

        // Display transcription for confirmation
        if (transcriptionPanel != null)
            transcriptionPanel.DisplayTranscription(transcribedText);

        // Analyze player's sentiment
        StartCoroutine(AnalyzePlayerArgument(transcribedText));
    }

    private IEnumerator AnalyzePlayerArgument(string playerText)
    {
        bool analysisComplete = false;
        SentimentResult playerSentiment = null;

        yield return HuggingFaceAPIManager.Instance.AnalyzeSentiment(
            playerText,
            result =>
            {
                playerSentiment = result;
                analysisComplete = true;
            },
            error =>
            {
                Debug.LogError($"Sentiment analysis error: {error}");
                analysisComplete = true;
            }
        );

        yield return new WaitUntil(() => analysisComplete);

        if (playerSentiment != null)
        {
            // Calculate defense score (positive sentiment helps defense)
            float defenseScore = CalculateArgumentScore(playerSentiment);
            cumulativeDefenseSentiment += defenseScore;

            // Trigger jury reactions based on sentiment
            if (juryManager != null)
                juryManager.ReactToArgument(playerSentiment, true); // true = defense

            // Store round data
            RoundData roundData = new RoundData
            {
                roundNumber = currentRound,
                defenseArgument = playerText,
                defenseSentiment = playerSentiment,
                defenseScore = defenseScore
            };

            // Now generate AI response
            StartCoroutine(GenerateAIResponse(playerText, roundData));
        }
    }

    private IEnumerator GenerateAIResponse(string playerText, RoundData roundData)
    {
        processingAI = true;
        if (opposingLawyerPanel != null)
            opposingLawyerPanel.ShowThinking();

        bool generationComplete = false;
        string aiResponse = "";

        // FIX: Added the 'roundData.defenseSentiment' argument here (the 4th argument).
        yield return HuggingFaceAPIManager.Instance.GenerateOpposingArgument(
            caseDescription,
            playerText,
            currentRound,
            roundData.defenseSentiment, // <-- THIS WAS THE MISSING ARGUMENT
            result =>
            {
                aiResponse = result;
                generationComplete = true;
            },
            error =>
            {
                Debug.LogError($"AI generation error: {error}");
                aiResponse = "The prosecution rests.";
                generationComplete = true;
            }
        );

        yield return new WaitUntil(() => generationComplete);

        // Display AI response
        if (opposingLawyerPanel != null)
            opposingLawyerPanel.DisplayArgument(aiResponse);

        // Analyze AI sentiment
        yield return AnalyzeAIArgument(aiResponse, roundData);
    }

    private IEnumerator AnalyzeAIArgument(string aiText, RoundData roundData)
    {
        bool analysisComplete = false;
        SentimentResult aiSentiment = null;

        yield return HuggingFaceAPIManager.Instance.AnalyzeSentiment(
            aiText,
            result =>
            {
                aiSentiment = result;
                analysisComplete = true;
            },
            error =>
            {
                Debug.LogError($"AI sentiment analysis error: {error}");
                analysisComplete = true;
            }
        );

        yield return new WaitUntil(() => analysisComplete);

        if (aiSentiment != null)
        {
            // Calculate prosecution score
            float prosecutionScore = CalculateArgumentScore(aiSentiment);
            cumulativeProsecutionSentiment += prosecutionScore;

            // Trigger jury reactions
            if (juryManager != null)
                juryManager.ReactToArgument(aiSentiment, false); // false = prosecution

            // Complete round data
            roundData.prosecutionArgument = aiText;
            roundData.prosecutionSentiment = aiSentiment;
            roundData.prosecutionScore = prosecutionScore;

            roundHistory.Add(roundData);
        }

        processingAI = false;

        // Wait 2 seconds before next round
        yield return new WaitForSeconds(2f);
        StartNextRound();
    }

    private float CalculateArgumentScore(SentimentResult sentiment)
    {
        // Positive sentiment = strong argument
        // Negative sentiment = weak argument
        // Score range: -1 to 1
        return sentiment.positive - sentiment.negative;
    }

    private void CalculateFinalVerdict()
    {
        Debug.Log("Trial Complete - Calculating Verdict");

        float totalDefense = cumulativeDefenseSentiment;
        float totalProsecution = cumulativeProsecutionSentiment;

        float sentimentDifference = totalDefense - totalProsecution;

        string verdict;
        if (sentimentDifference > 0.5f)
            verdict = "NOT GUILTY";
        else if (sentimentDifference < -0.5f)
            verdict = "GUILTY";
        else
            verdict = "HUNG JURY"; // Too close to call

        Debug.Log($"Defense Score: {totalDefense} | Prosecution Score: {totalProsecution}");
        Debug.Log($"Verdict: {verdict}");

        OnVerdictReached?.Invoke(verdict);

        // PASS DEFENDANT NAME as required by JuryReactionManager.ShowFinalVerdict(string, string)
        string defendant = currentCase != null && !string.IsNullOrWhiteSpace(currentCase.defendantName)
            ? currentCase.defendantName
            : "Defendant";

        if (juryManager != null)
            juryManager.ShowFinalVerdict(verdict, defendant);
    }

    // === Added: called by UICaseSelectionPanel ===
    public void SetCurrentCase(CaseData data)
    {
        currentCase = data;
        caseDescription = (data != null) ? data.GetFullContext() : "";
        Debug.Log($"Current case set: {(data != null ? data.caseTitle : "null")}");
    }

    // Legacy string setter kept for compatibility if you call it elsewhere
    public void SetCaseDescription(string description)
    {
        caseDescription = description ?? "";
    }

    public int GetCurrentRound() => currentRound;
    public int GetMaxRounds() => maxRounds;
}

[System.Serializable]
public class RoundData
{
    public int roundNumber;
    public string defenseArgument;
    public SentimentResult defenseSentiment;
    public float defenseScore;
    public string prosecutionArgument;
    public SentimentResult prosecutionSentiment;
    public float prosecutionScore;
}