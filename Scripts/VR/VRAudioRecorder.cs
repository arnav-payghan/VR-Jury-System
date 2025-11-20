using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class VRAudioRecorder : MonoBehaviour
{
    [Header("Recording Settings")]
    [SerializeField] private int recordingLength = 10; // seconds
    [SerializeField] private int sampleRate = 16000; // Whisper prefers 16kHz

    [Header("VR Input")]
    [SerializeField] private OVRInput.Button recordButton = OVRInput.Button.PrimaryIndexTrigger;
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch;

    [Header("Events")]
    public UnityEvent OnRecordingStarted;
    public UnityEvent OnRecordingStopped;
    public UnityEvent<string> OnTranscriptionReceived;

    [Header("UI")]
    [SerializeField] private GameObject recordingIndicator;

    private AudioClip recordedClip;
    private bool isRecording = false;
    private string microphoneDevice;

    // --- NEW: Default Text for Testing ---
    //private const string DEFAULT_TEST_TEXT = "The evidence overwhelmingly shows that the defendant was wrongfully targeted. The prosecution's key witness has been proven unreliable and deceptive. Justice demands a verdict of reasonable doubt and freedom.";
    private const string DEFAULT_TEST_TEXT = "We realize our documentation is incomplete regarding the defendant's whereabouts, and some of our core claims are, regrettably, based on hearsay. The defense has little else to offer at this time.";
    //private const string DEFAULT_TEST_TEXT = "This point addresses the procedural step of evidence admissibility. We are strictly arguing the legal technicality of the filing date, independent of the evidence's content or relevance to the case.";
    // ------------------------------------

    private void Start()
    {
        Debug.Log("VRAudioRecorder Initializing...");
        if (recordingIndicator != null)
            recordingIndicator.SetActive(false);

        // Get microphone device
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            Debug.Log($"Using microphone: {microphoneDevice}");
        }
        else
        {
            Debug.LogError("No microphone detected! Using default text input for testing.");
            microphoneDevice = null; // Ensure it's null or empty if no mic is found
        }
    }

    private void Update()
    {
        // Check for VR button press
        if (OVRInput.GetDown(recordButton, controller) && !isRecording)
        {
            StartRecording();
        }
        else if (OVRInput.GetUp(recordButton, controller) && isRecording)
        {
            StopRecording();
        }

        // Keyboard Testing Input (R for Start, T for Stop)
        if (Input.GetKeyDown(KeyCode.R) && !isRecording)
        {
            StartRecording();
            Debug.Log("Started Recording (Keyboard R)...");
        }
        else if (Input.GetKeyDown(KeyCode.T) && isRecording)
        {
            StopRecording();
            Debug.Log("Stopped Recording (Keyboard T)...");
        }
    }

    public void StartRecording()
    {
        if (string.IsNullOrEmpty(microphoneDevice))
        {
            Debug.LogError("NO MICROPHONE DETECTED. SKIPPING RECORDING AND INJECTING DEFAULT TEXT.");
            
            // ********** DEFAULT TEXT INJECTION POINT **********
            // Instead of starting the mic, we directly move to the transcription processing phase
            // with the default text. This allows the rest of the game logic to continue.
            ProcessTranscription(DEFAULT_TEST_TEXT); 
            // **************************************************

            return; // Exit the function since recording cannot start
        }

        isRecording = true;
        recordedClip = Microphone.Start(microphoneDevice, false, recordingLength, sampleRate);

        if (recordingIndicator != null)
            recordingIndicator.SetActive(true);

        OnRecordingStarted?.Invoke();
        Debug.Log("Recording started...");
    }

    public void StopRecording()
    {
        if (!isRecording) return;

        int recordPosition = Microphone.GetPosition(microphoneDevice);
        Microphone.End(microphoneDevice);
        isRecording = false;

        if (recordingIndicator != null)
            recordingIndicator.SetActive(false);

        OnRecordingStopped?.Invoke();
        Debug.Log("Recording stopped");

        // Trim the audio clip to actual recorded length
        AudioClip trimmedClip = TrimAudioClip(recordedClip, recordPosition);

        // Convert to WAV bytes and send to API
        byte[] wavData = ConvertAudioClipToWav(trimmedClip);
        StartCoroutine(TranscribeAudioAPI(wavData)); // Changed method name
    }

    // --- New/Renamed Method: Handles API Call or Direct Text Processing ---
    private void ProcessTranscription(string transcribedText)
    {
        Debug.Log($"FINAL ARGUMENT READY (Sent to Manager): {transcribedText}");
        OnTranscriptionReceived?.Invoke(transcribedText);

        // Send to trial manager
        if (TrialRoundManager.Instance != null)
        {
            TrialRoundManager.Instance.OnPlayerArgumentComplete(transcribedText);
        }
        else
        {
            Debug.LogError("TrialRoundManager.Instance is null. Cannot progress game logic.");
        }
    }

    // --- Renamed Coroutine: Transcribes audio via API and then calls ProcessTranscription ---
    private IEnumerator TranscribeAudioAPI(byte[] audioData)
    {
        Debug.Log("Starting API Transcription process...");
        bool transcriptionComplete = false;
        string transcribedText = "";

        yield return HuggingFaceAPIManager.Instance.TranscribeAudio(
            audioData,
            result =>
            {
                transcribedText = result;
                transcriptionComplete = true;
                Debug.Log($"SUCCESS: API returned transcription: {transcribedText}");
            },
            error =>
            {
                Debug.LogError($"ERROR: Transcription API error: {error}");
                transcribedText = "[Transcription failed: " + error + "]";
                transcriptionComplete = true;
            }
        );

        yield return new WaitUntil(() => transcriptionComplete);

        // Continue the flow with the result (either successful or failed transcription)
        ProcessTranscription(transcribedText);
    }
    // ----------------------------------------------------------------------------------

    private AudioClip TrimAudioClip(AudioClip clip, int samples)
    {
        float[] data = new float[samples * clip.channels];
        clip.GetData(data, 0);

        AudioClip trimmed = AudioClip.Create(clip.name + "_trimmed", samples, clip.channels, clip.frequency, false);
        trimmed.SetData(data, 0);
        
        Debug.Log($"Audio trimmed to {samples} samples.");

        return trimmed;
    }

    private byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        Debug.Log("Converting AudioClip to WAV byte array...");
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream);

        // WAV file header
        writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
        writer.Write(0); // File size (filled later)
        writer.Write(new char[4] { 'W', 'A', 'V', 'E' });

        // Format chunk
        writer.Write(new char[4] { 'f', 'm', 't', ' ' });
        writer.Write(16); // Chunk size
        writer.Write((ushort)1); // Audio format (PCM)
        writer.Write((ushort)clip.channels);
        writer.Write(clip.frequency);
        writer.Write(clip.frequency * clip.channels * 2); // Byte rate
        writer.Write((ushort)(clip.channels * 2)); // Block align
        writer.Write((ushort)16); // Bits per sample

        // Data chunk
        writer.Write(new char[4] { 'd', 'a', 't', 'a' });
        writer.Write(samples.Length * 2);

        // Convert samples to 16-bit PCM
        foreach (float sample in samples)
        {
            short value = (short)(sample * 32767f);
            writer.Write(value);
        }

        // Update file size
        writer.Seek(4, System.IO.SeekOrigin.Begin);
        writer.Write((int)stream.Length - 8);

        byte[] wavData = stream.ToArray();
        writer.Close();
        stream.Close();
        Debug.Log($"WAV conversion complete. Size: {wavData.Length} bytes.");
        
        return wavData;
    }

    // Alternative: Manual start/stop for UI buttons
    public void ManualStartRecording()
    {
        if (!isRecording)
            StartRecording();
    }

    public void ManualStopRecording()
    {
        if (isRecording)
            StopRecording();
    }
}