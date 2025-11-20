// TestJuror.cs
using UnityEngine;

public class TestJuror : MonoBehaviour
{
    public JuryMember jurorToTest;
    
    void Update()
    {
        // Press H to test happy reaction
        if (Input.GetKeyDown(KeyCode.H))
        {
            SentimentResult testSentiment = new SentimentResult
            {
                positive = 0.8f,
                negative = 0.1f,
                neutral = 0.1f,
                dominantSentiment = "positive"
            };
            jurorToTest.ShowReaction(testSentiment, true);
        }
        
        // Press S to test sad reaction
        if (Input.GetKeyDown(KeyCode.S))
        {
            SentimentResult testSentiment = new SentimentResult
            {
                positive = 0.1f,
                negative = 0.8f,
                neutral = 0.1f,
                dominantSentiment = "negative"
            };
            jurorToTest.ShowReaction(testSentiment, false);
        }
    }
}