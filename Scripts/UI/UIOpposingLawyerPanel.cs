using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIOpposingLawyerPanel : MonoBehaviour
{
    // === Serialized Fields ===
    [Tooltip("The main UI panel GameObject to activate/deactivate.")]
    [SerializeField] private GameObject panel;

    [Tooltip("Text component for the lawyer's name.")]
    [SerializeField] private Text lawyerNameText;

    [Tooltip("Text component for the lawyer's argument/dialogue.")]
    [SerializeField] private Text argumentText;

    [Tooltip("GameObject for the 'Thinking...' visual indicator.")]
    [SerializeField] private GameObject thinkingIndicator;

    [Tooltip("How long the argument text remains visible before hiding the panel.")]
    [SerializeField] private float displayDuration = 5f;

    [Tooltip("The default name for the opposing lawyer.")]
    [SerializeField] private string defaultLawyerName = "Prosecution";

    // === Private State Variable ===
    private IEnumerator hideCoroutine; // Reference to the running coroutine

    private void Start()
    {
        // 1. Ensure the panel is hidden on start.
        if (panel != null)
            panel.SetActive(false);

        // 2. Set the initial lawyer name from the serialized field.
        if (lawyerNameText != null)
            lawyerNameText.text = defaultLawyerName;
    }

    /// <summary>
    /// Activates the panel and shows the thinking indicator, clearing any previous text.
    /// </summary>
    public void ShowThinking()
    {
        if (panel != null)
            panel.SetActive(true);

        if (thinkingIndicator != null)
            thinkingIndicator.SetActive(true);

        if (argumentText != null)
            argumentText.text = "";
            
        // Stop any previous hide timer immediately if we start a new sequence
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine); 
    }

    /// <summary>
    /// Displays the argument, stops the thinking indicator, and starts the hide timer.
    /// </summary>
    /// <param name="argument">The string argument to display.</param>
    public void DisplayArgument(string argument)
    {
        if (thinkingIndicator != null)
            thinkingIndicator.SetActive(false);

        if (argumentText != null)
            argumentText.text = argument;

        // **CRITICAL FIX:** Stop any previously running coroutine before starting a new one.
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);
        
        // Start the new coroutine and store the reference
        hideCoroutine = HideAfterDelay();
        StartCoroutine(hideCoroutine);
    }

    /// <summary>
    /// Immediately hides the panel and stops any active hide timer.
    /// </summary>
    public void Hide()
    {
        // Stop the coroutine in case an external script hides the panel
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine); 
            
        if (panel != null)
            panel.SetActive(false);
    }

    /// <summary>
    /// Allows an external script to change the lawyer's name dynamically.
    /// </summary>
    /// <param name="name">The new name for the lawyer.</param>
    public void SetLawyerName(string name)
    {
        if (lawyerNameText != null)
            lawyerNameText.text = name;
    }

    private IEnumerator HideAfterDelay()
    {
        // Use WaitForSecondsRealtime if the game time scale might be paused or changed
        yield return new WaitForSeconds(displayDuration); 
        
        // Only hide the panel if it's currently active to prevent unnecessary calls
        if (panel != null && panel.activeInHierarchy)
            panel.SetActive(false);
            
        // Clear the coroutine reference after it finishes
        hideCoroutine = null; 
    }
}