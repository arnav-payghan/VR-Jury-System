using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random; // Use UnityEngine.Random for Unity projects

// --- NEW CLASS FOR CORRECT JSON INPUT FORMAT ---
[System.Serializable]
public class HFTextInput
{
    public string[] inputs; // The JSON key MUST be 'inputs'
}

// ----------------------------------------------

[System.Serializable]
public class HFSpeechToTextResponse
{
    public string text;
}

[System.Serializable]
public class HFTextGenerationResponse
{
    // Keeping this class as it might be needed for other models later, 
    // but the local generation logic won't use it.
    public string generated_text;
}

[System.Serializable]
public class SentimentScore
{
    public string label;
    public float score;
}

[System.Serializable]
public class SentimentResult
{
    public float positive;
    public float negative;
    public float neutral;
    public string dominantSentiment;
}

/// <summary>
/// A structure to hold the three possible arguments the AI Lawyer can choose from.
/// </summary>
public struct AILawyerArgument
{
    public string argumentType; // e.g., "Good", "Neutral", "Bad"
    public string promptInstruction; // The specific instruction (now unused in local generation)
}


public class HuggingFaceAPIManager : MonoBehaviour
{
    [Header("API Configuration")]
    [SerializeField] private string apiKey = "YOUR_HUGGINGFACE_API_KEY";
    
    // Model endpoints
    private const string SPEECH_TO_TEXT_MODEL = "openai/whisper-large-v3";
    private const string TEXT_GENERATION_MODEL = "mistralai/Mistral-7B-Instruct-v0.2"; 
    private const string SENTIMENT_MODEL = "tabularisai/multilingual-sentiment-analysis";
    
    // Using the new router endpoint
    private const string BASE_ROUTER_URL = "https://router.huggingface.co/hf-inference/models/";

    private string speechToTextURL => $"{BASE_ROUTER_URL}{SPEECH_TO_TEXT_MODEL}";
    private string textGenerationURL => $"{BASE_ROUTER_URL}{TEXT_GENERATION_MODEL}"; 
    private string sentimentURL => $"{BASE_ROUTER_URL}{SENTIMENT_MODEL}";

    public static HuggingFaceAPIManager Instance { get; private set; }

    // --- NEW: COLLECTIONS OF ARGUMENTS FOR RANDOMIZATION ---
    // These strings are designed to be analyzed as the corresponding sentiment type.
    private readonly List<string> goodArguments = new List<string>
    {
        "The defense conveniently overlooks the forensic evidence presented in Exhibit A. The prosecution asserts this irrefutable proof places the defendant at the scene, directly contradicting their fabricated timeline. The evidence speaks for itself, Your Honor.",
        "We have presented three separate, unimpeachable witness testimonies that corroborate the defendant's guilt. The defense has provided only conjecture. The weight of the evidence is clearly on the side of justice and conviction.",
        "The intent to commit the crime is clearly documented in the defendant's own communications, which are part of the record. This premeditation alone proves the defendant's culpability beyond a reasonable doubt."
    };

    private readonly List<string> neutralArguments = new List<string>
    {
        "The argument regarding the witness's credibility is noted, but the core issue remains the physical evidence. We must focus on the established facts, not secondary emotional appeals. The case stands on the law, not conjecture.",
        "The time of the incident, while debated, does not fundamentally alter the sequence of events as established by police reports. The defense is splitting hairs over a non-essential detail. We accept the police report's finding.",
        "We have reached a point where both sides have established their positions. We rely on the jury to compare the defense's theory with the physical reality of the evidence we have submitted. The facts are clear."
    };

    private readonly List<string> badArguments = new List<string>
    {
        "While the defense makes a passionate plea, their argument hinges on speculation about the defendant's character. Character, however admirable, does not negate the overwhelming circumstantial facts presented. We stand by the charges.",
        "We maintain that the defendant's actions following the incident, though not directly criminal, suggest a lack of ethical responsibility. The defense should focus less on excuses and more on the harm caused.",
        "I admit, the defense raised an interesting point about the motive. However, we believe that focusing on motive distracts from the clear statutory violation that occurred. Motive is secondary to action."
    };
    // --------------------------------------------------------

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- API COROUTINES (Speech-to-Text and Sentiment remain API-dependent) ---

    // Speech to Text - Accepts audio bytes
    public IEnumerator TranscribeAudio(byte[] audioData, Action<string> onSuccess, Action<string> onError)
    {
        Debug.Log($"API Call: Transcribing audio ({audioData.Length} bytes) using {SPEECH_TO_TEXT_MODEL}...");
        UnityWebRequest request = new UnityWebRequest(speechToTextURL, "POST");
        request.uploadHandler = new UploadHandlerRaw(audioData);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        request.SetRequestHeader("Content-Type", "audio/wav");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                string response = request.downloadHandler.text;
                HFSpeechToTextResponse data = JsonUtility.FromJson<HFSpeechToTextResponse>(response);
                Debug.Log($"SUCCESS: Transcription received: {data.text.Substring(0, Math.Min(data.text.Length, 50))}...");
                onSuccess?.Invoke(data.text);
            }
            catch (Exception e)
            {
                Debug.LogError($"API Parse Error (TranscribeAudio): {e.Message}");
                onError?.Invoke($"Parse error: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"API Error (TranscribeAudio): {request.error}");
            onError?.Invoke($"API Error: {request.error}");
        }
    }

    // Generate AI Lawyer Response - *** NOW USES RANDOMIZED LOCAL LOGIC ***
    public IEnumerator GenerateOpposingArgument(string caseContext, string playerArgument, int roundNumber, SentimentResult playerSentiment, Action<string> onSuccess, Action<string> onError)
    {
        // 1. Determine the AI lawyer's strategy based on player's last argument sentiment
        AILawyerArgument selectedStrategy = SelectAIPrompt(playerSentiment);
        Debug.Log($"AI Strategy: Player's dominant sentiment was '{playerSentiment.dominantSentiment}'. AI will use a **{selectedStrategy.argumentType}** randomized local argument.");

        // 2. Select a RANDOM pre-written argument based on the chosen strategy
        string generatedArgument = GetLocalOpposingArgument(selectedStrategy.argumentType, roundNumber);

        // 3. Wait for one frame to simulate asynchronous loading/network delay
        yield return null; 

        // 4. Invoke success callback with the locally generated argument
        Debug.Log($"SUCCESS: Local Argument generated ({selectedStrategy.argumentType}): {generatedArgument.Substring(0, Math.Min(generatedArgument.Length, 50))}...");
        onSuccess?.Invoke(generatedArgument);
    }
    
    // Analyze Sentiment
    public IEnumerator AnalyzeSentiment(string text, Action<SentimentResult> onSuccess, Action<string> onError)
    {
        // Use the wrapper class to serialize the input text as an array
        var requestData = new HFTextInput { inputs = new[] { text } };
        string jsonData = JsonUtility.ToJson(requestData);
        
        Debug.Log($"API Call: Analyzing sentiment for text: {text.Substring(0, Math.Min(text.Length, 50))}... using {SENTIMENT_MODEL}.");

        UnityWebRequest request = new UnityWebRequest(sentimentURL, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                string response = request.downloadHandler.text;
                // Response is nested array: [[{label, score}]]
                response = response.Trim('[', ']');
                
                List<SentimentScore> scores = JsonUtility.FromJson<List<SentimentScore>>(response);
                Debug.Log($"SUCCESS: Sentiment scores received. Processing {scores.Count} labels.");
                
                SentimentResult result = ProcessSentimentScores(scores);
                onSuccess?.Invoke(result);
            }
            catch (Exception e)
            {
                Debug.LogError($"API Parse Error (AnalyzeSentiment): {e.Message} \nFull Response: {request.downloadHandler.text}");
                onError?.Invoke($"Parse error: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"API Error (AnalyzeSentiment): {request.error} \nURL: {request.url} \nPayload: {jsonData}");
            onError?.Invoke($"API Error: {request.error}");
        }
    }

    // --- GAME LOGIC FUNCTIONS ---

    /// <summary>
    /// Chooses a local, pre-written argument based on the desired type RANDOMLY.
    /// </summary>
    private string GetLocalOpposingArgument(string argumentType, int round)
    {
        List<string> selectedList;

        switch (argumentType.ToLower())
        {
            case "good":
                selectedList = goodArguments;
                break;
            case "bad":
                selectedList = badArguments;
                break;
            case "neutral":
            default:
                selectedList = neutralArguments;
                break;
        }

        // Select a random argument from the chosen list.
        if (selectedList.Count > 0)
        {
            int randomIndex = Random.Range(0, selectedList.Count);
            return selectedList[randomIndex];
        }

        // Fallback
        return "The prosecution submits their argument.";
    }

    /// <summary>
    /// Selects the AI lawyer's argument type based on the player's performance (sentiment).
    /// </summary>
    private AILawyerArgument SelectAIPrompt(SentimentResult playerSentiment)
    {
        // Rule: Reward bad arguments (negative sentiment) with a strong counter (Good AI argument).
        // Rule: Punish good arguments (positive sentiment) with a weak counter (Bad AI argument).
        // Rule: Keep neutral arguments balanced (Neutral AI argument).

        if (playerSentiment.dominantSentiment == "negative")
        {
            // Player did poorly -> AI lawyer counters strongly to maintain pressure.
            return new AILawyerArgument 
            { 
                argumentType = "Good", 
                promptInstruction = "Your counter-argument must be very strong."
            };
        }
        else if (playerSentiment.dominantSentiment == "positive")
        {
            // Player did well -> AI lawyer struggles/stalls with a weak counter.
            return new AILawyerArgument 
            { 
                argumentType = "Bad", 
                promptInstruction = "Your counter-argument must be weak."
            };
        }
        else
        {
            // Player was neutral -> AI lawyer gives a standard, solid argument.
            return new AILawyerArgument 
            { 
                argumentType = "Neutral", 
                promptInstruction = "Your counter-argument should be professional and average." 
            };
        }
    }

    // Process Sentiment Scores - Handling 5 classes from the multilingual model
    private SentimentResult ProcessSentimentScores(List<SentimentScore> scores)
    {
        float positive = 0f;
        float negative = 0f;
        float neutral = 0f;

        // Model labels are: Very Positive, Positive, Neutral, Negative, Very Negative
        foreach (var score in scores)
        {
            string label = score.label.ToLower();

            if (label.Contains("positive"))
            {
                // Aggregate 'Positive' and 'Very Positive' scores
                positive += score.score;
            }
            else if (label.Contains("negative"))
            {
                // Aggregate 'Negative' and 'Very Negative' scores
                negative += score.score;
            }
            else if (label.Contains("neutral"))
            {
                neutral = score.score;
            }
        }
        
        SentimentResult result = new SentimentResult
        {
            positive = positive,
            negative = negative,
            neutral = neutral,
            dominantSentiment = GetDominantSentiment(positive, negative, neutral)
        };
        Debug.Log($"Sentiment Processed: Dominant={result.dominantSentiment} (P:{positive:F2}, N:{negative:F2}, U:{neutral:F2})");
        return result;
    }

    private string GetDominantSentiment(float positive, float negative, float neutral)
    {
        // Compare the aggregates to find the dominant score.
        if (positive > negative && positive > neutral)
            return "positive";
        else if (negative > positive && negative > neutral)
            return "negative";
        else
            return "neutral";
    }
}