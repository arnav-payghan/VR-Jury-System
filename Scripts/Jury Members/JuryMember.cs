using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class JuryMember : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The UI bubble that appears above the juror's head")]
    [SerializeField] private GameObject reactionBubble;
    
    [Tooltip("The image component inside the bubble that displays emoji")]
    [SerializeField] private Image emojiImage;
    
    [Header("Emoji Sprites")]
    [Tooltip("Happy face emoji - shown for positive arguments")]
    [SerializeField] private Sprite happyEmoji;
    
    [Tooltip("Sad face emoji - shown for weak/negative arguments")]
    [SerializeField] private Sprite sadEmoji;
    
    [Tooltip("Angry face emoji - shown for very negative arguments")]
    [SerializeField] private Sprite angryEmoji;
    
    [Tooltip("Heart emoji - shown for very strong positive arguments")]
    [SerializeField] private Sprite heartEmoji;
    
    [Tooltip("Neutral face emoji - shown for balanced arguments")]
    [SerializeField] private Sprite neutralEmoji;
    
    [Tooltip("Confused face emoji - shown for unclear arguments")]
    [SerializeField] private Sprite confusedEmoji;
    
    [Header("Personality Settings")]
    [Tooltip("How sympathetic this juror is to the defense (0=prosecution bias, 1=defense bias)")]
    [SerializeField] [Range(0f, 1f)] private float sympathyForDefense = 0.5f;
    
    [Header("Display Settings")]
    [Tooltip("How long emoji stays visible (seconds)")]
    [SerializeField] private float emojiDisplayDuration = 3f;
    
    // Internal tracking
    private float currentDefenseLeaning = 0f;

    private void Start()
    {
        // Hide the reaction bubble at start
        if (reactionBubble != null)
            reactionBubble.SetActive(false);
    }

    /// <summary>
    /// Called by JuryReactionManager when an argument is made
    /// </summary>
    public void ShowReaction(SentimentResult sentiment, bool isDefense)
    {
        // Calculate how this argument affects juror's opinion
        float argumentImpact = CalculateArgumentImpact(sentiment);
        
        // Adjust juror's leaning based on who argued (defense or prosecution)
        if (isDefense)
        {
            // Defense argument - affects based on sympathy level
            currentDefenseLeaning += argumentImpact * (0.5f + sympathyForDefense * 0.5f);
        }
        else
        {
            // Prosecution argument - works against defense
            currentDefenseLeaning -= argumentImpact * (0.5f + (1f - sympathyForDefense) * 0.5f);
        }

        // Keep leaning in reasonable bounds
        currentDefenseLeaning = Mathf.Clamp(currentDefenseLeaning, -2f, 2f);

        // Determine which emoji to show
        Sprite chosenEmoji = DetermineEmoji(sentiment, isDefense);
        
        // Display the emoji
        ShowEmojiReaction(chosenEmoji);
    }

    /// <summary>
    /// Calculates how impactful an argument was based on sentiment
    /// </summary>
    private float CalculateArgumentImpact(SentimentResult sentiment)
    {
        // Strong positive sentiment = very impactful argument
        if (sentiment.positive > 0.6f)
            return 1f;
        // Moderate positive
        else if (sentiment.positive > 0.4f)
            return 0.5f;
        // Strong negative sentiment = backfired argument
        else if (sentiment.negative > 0.6f)
            return -0.5f;
        // Weak/unclear argument
        else
            return 0.2f;
    }

    /// <summary>
    /// Chooses appropriate emoji based on sentiment and context
    /// </summary>
    private Sprite DetermineEmoji(SentimentResult sentiment, bool isDefense)
    {
        // Very positive sentiment (score > 0.6)
        if (sentiment.positive > 0.6f)
        {
            // Randomly pick between happy and heart for variety
            return Random.value > 0.5f ? happyEmoji : heartEmoji;
        }
        // Moderately positive (0.4 - 0.6)
        else if (sentiment.positive > 0.4f)
        {
            return happyEmoji;
        }
        // Very negative sentiment
        else if (sentiment.negative > 0.6f)
        {
            // Randomly pick sad or angry
            return Random.value > 0.5f ? sadEmoji : angryEmoji;
        }
        // Moderately negative
        else if (sentiment.negative > 0.4f)
        {
            return sadEmoji;
        }
        // Neutral/unclear
        else
        {
            return Random.value > 0.5f ? neutralEmoji : confusedEmoji;
        }
    }

    /// <summary>
    /// Actually displays the emoji bubble above juror's head
    /// </summary>
    private void ShowEmojiReaction(Sprite emoji)
    {
        if (reactionBubble != null && emojiImage != null)
        {
            // Set the emoji sprite
            emojiImage.sprite = emoji;
            
            // Show the bubble
            reactionBubble.SetActive(true);
            
            // Auto-hide after duration
            StartCoroutine(HideReactionAfterDelay(emojiDisplayDuration));
        }
    }

    /// <summary>
    /// Hides emoji bubble after a delay
    /// </summary>
    private IEnumerator HideReactionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (reactionBubble != null)
            reactionBubble.SetActive(false);
    }

    /// <summary>
    /// Shows juror's final reaction to the verdict
    /// </summary>
    public void ShowVerdictReaction(string verdict)
    {
        Sprite finalEmoji;
        
        if (verdict == "NOT GUILTY")
        {
            // If juror was leaning defense, show love/happiness
            // If juror was leaning prosecution, show confusion
            finalEmoji = currentDefenseLeaning > 0 ? heartEmoji : confusedEmoji;
        }
        else if (verdict == "GUILTY")
        {
            // If juror was leaning prosecution, show neutral/satisfied
            // If juror was leaning defense, show sadness
            finalEmoji = currentDefenseLeaning < 0 ? neutralEmoji : sadEmoji;
        }
        else // HUNG JURY
        {
            finalEmoji = confusedEmoji;
        }

        ShowEmojiReaction(finalEmoji);
    }

    /// <summary>
    /// Resets juror state for new trial
    /// </summary>
    public void ResetReaction()
    {
        currentDefenseLeaning = 0f;
        if (reactionBubble != null)
            reactionBubble.SetActive(false);
    }
}