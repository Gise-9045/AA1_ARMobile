using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using TextSpeech;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Events;

public class VoiceController : MonoBehaviour
{
    [Serializable]
    public class StringEvent : UnityEvent<string> { }

    public enum KeywordMatchMode
    {
        ExactText,
        Contains,
        WholeWord
    }

    [Serializable]
    public class KeywordEventBinding
    {
        public string keyword;
        public KeywordMatchMode matchMode = KeywordMatchMode.WholeWord;
        public bool ignoreCase = true;
        public UnityEvent onDetected;
    }

    [Header("General")]
    [SerializeField] private string languageCode = "en-US";
    [SerializeField] private TMP_Text uiText;

    [Header("Startup")]
    [SerializeField] private bool autoActivate = true;

    [Header("Listening Flow")]
    [SerializeField] private bool autoRestartAfterPhrase = true;
    [SerializeField] private float restartDelay = 0.25f;
    [SerializeField] private float stopAfterEndOfSpeechDelay = 0.75f;

    [Header("Safety")]
    [SerializeField] private bool useSilenceWatchdog = true;
    [SerializeField] private float silenceWatchdogSeconds = 4f;

    [Header("TTS")]
    [SerializeField] private float ttsRate = 1f;
    [SerializeField] private float ttsPitch = 1f;

    [Header("Keyword Events")]
    [SerializeField] private List<KeywordEventBinding> keywordEvents = new List<KeywordEventBinding>();
    [SerializeField] private StringEvent onFinalResult;

    private bool isInitialized;
    private bool isListening;
    private bool isUserSpeaking;
    private bool isRestartScheduled;
    private bool isShuttingDown;
    private float lastSpeechActivityTime;
    private Coroutine delayedStopCoroutine;
    private Coroutine restartCoroutine;

    private void Start()
    {
        if (uiText == null)
        {
            Debug.LogWarning("uiText no está asignado. El sistema seguirá funcionando, pero sin feedback visual.");
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(InitAndroidVoice());
#else
        SetUi("No Android / Editor");
        Debug.LogWarning("Plugin de voz: prueba real en Android, no en Editor");
#endif
    }

    private void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!isInitialized || !isListening || !useSilenceWatchdog || isUserSpeaking)
            return;

        if (Time.time - lastSpeechActivityTime >= silenceWatchdogSeconds)
        {
            Debug.LogWarning("Silence watchdog: cortando grabación para evitar sesión colgada");
            StopListeningInternal(autoRestartAfterPhrase);
        }
#endif
    }

    private void OnDisable()
    {
        isShuttingDown = true;

        if (delayedStopCoroutine != null)
        {
            StopCoroutine(delayedStopCoroutine);
            delayedStopCoroutine = null;
        }

        if (restartCoroutine != null)
        {
            StopCoroutine(restartCoroutine);
            restartCoroutine = null;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (isListening)
        {
            StopListeningInternal(false);
        }
#endif
    }

    private IEnumerator InitAndroidVoice()
    {
        try
        {
            // Callbacks
            SpeechToText.Instance.onReadyForSpeechCallback = OnReadyForSpeech;
            SpeechToText.Instance.onBeginningOfSpeechCallback = OnBeginningOfSpeech;
            SpeechToText.Instance.onEndOfSpeechCallback = OnEndOfSpeech;
            SpeechToText.Instance.onRmsChangedCallback = OnRmsChanged;
            SpeechToText.Instance.onErrorCallback = OnSpeechError;
            SpeechToText.Instance.onPartialResultsCallback = OnPartialSpeechResult;
            SpeechToText.Instance.onResultCallback = OnFinalSpeechResult;

            TextToSpeech.Instance.onStartCallBack = OnSpeakStart;
            TextToSpeech.Instance.onDoneCallback = OnSpeakStop;

            // Sin popup Android
            SpeechToText.Instance.isShowPopupAndroid = false;

            SetUi("Callbacks OK");
        }
        catch (Exception e)
        {
            Debug.LogError("Error registrando callbacks: " + e);
            SetUi("Error callbacks: " + e.Message);
            yield break;
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            SetUi("Pidiendo permiso micrófono");
            Permission.RequestUserPermission(Permission.Microphone);

            float timeout = 10f;
            while (timeout > 0f && !Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                timeout -= 0.25f;
                yield return new WaitForSeconds(0.25f);
            }
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            SetUi("Permiso micrófono denegado");
            Debug.LogError("Permiso de micrófono denegado");
            yield break;
        }

        try
        {
            SetUi("Init TTS...");
            TextToSpeech.Instance.Setting(languageCode, ttsRate, ttsPitch);

            SetUi("Init STT...");
            SpeechToText.Instance.Setting(languageCode);
        }
        catch (Exception e)
        {
            Debug.LogError("Error inicializando voz: " + e);
            SetUi("Error voz: " + e.Message);
            yield break;
        }

        isInitialized = true;
        SetUi("Voice OK");

        if (autoActivate)
        {
            StartListening();
        }
    }

    public void StartSpeaking(string message)
    {
        try
        {
            TextToSpeech.Instance.StartSpeak(message);
        }
        catch (Exception e)
        {
            Debug.LogError("Error StartSpeaking: " + e);
            SetUi("Error Speak: " + e.Message);
        }
    }

    public void StopSpeaking()
    {
        try
        {
            TextToSpeech.Instance.StopSpeak();
        }
        catch (Exception e)
        {
            Debug.LogError("Error StopSpeaking: " + e);
        }
    }

    public void StartListening()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (isShuttingDown || !isInitialized || isListening)
            return;

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            SetUi("Sin permiso de micrófono");
            return;
        }

        try
        {
            isListening = true;
            isUserSpeaking = false;
            lastSpeechActivityTime = Time.time;

            if (delayedStopCoroutine != null)
            {
                StopCoroutine(delayedStopCoroutine);
                delayedStopCoroutine = null;
            }

            SpeechToText.Instance.StartRecording();
            SetUi("Escuchando...");
        }
        catch (Exception e)
        {
            isListening = false;
            Debug.LogError("Error StartListening: " + e);
            SetUi("Error Listen: " + e.Message);
        }
#endif
    }

    public void StopListening()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        StopListeningInternal(false);
#endif
    }

    private void StopListeningInternal(bool scheduleRestart)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!isListening)
            return;

        isListening = false;
        isUserSpeaking = false;

        try
        {
            SpeechToText.Instance.StopRecording();
        }
        catch (Exception e)
        {
            Debug.LogWarning("Error StopListening: " + e);
        }

        if (scheduleRestart)
        {
            ScheduleRestart();
        }
#endif
    }

    private void ScheduleRestart()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (isShuttingDown || !autoRestartAfterPhrase || isRestartScheduled)
            return;

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            return;

        restartCoroutine = StartCoroutine(RestartListeningRoutine());
#endif
    }

    private IEnumerator RestartListeningRoutine()
    {
        isRestartScheduled = true;
        yield return new WaitForSeconds(restartDelay);
        isRestartScheduled = false;
        restartCoroutine = null;

        if (!isShuttingDown && isInitialized && !isListening)
        {
            StartListening();
        }
    }

    private void OnReadyForSpeech(string _)
    {
        lastSpeechActivityTime = Time.time;
        Debug.Log("STT Ready");
    }

    private void OnBeginningOfSpeech()
    {
        isUserSpeaking = true;
        lastSpeechActivityTime = Time.time;
        Debug.Log("User speech started");
    }

    private void OnEndOfSpeech()
    {
        isUserSpeaking = false;
        lastSpeechActivityTime = Time.time;
        Debug.Log("User speech ended");

        if (delayedStopCoroutine != null)
        {
            StopCoroutine(delayedStopCoroutine);
        }

        delayedStopCoroutine = StartCoroutine(StopAfterEndOfSpeechRoutine());
    }

    private IEnumerator StopAfterEndOfSpeechRoutine()
    {
        yield return new WaitForSeconds(stopAfterEndOfSpeechDelay);

        if (isListening && !isUserSpeaking)
        {
            StopListeningInternal(autoRestartAfterPhrase);
        }

        delayedStopCoroutine = null;
    }

    private void OnRmsChanged(float rms)
    {
        if (isListening)
        {
            lastSpeechActivityTime = Time.time;
        }
    }

    private void OnSpeechError(string error)
    {
        Debug.LogWarning("STT Error: " + error);
        SetUi("STT Error: " + error);
        StopListeningInternal(autoRestartAfterPhrase);
    }

    private void OnFinalSpeechResult(string result)
    {
        result = result?.Trim() ?? string.Empty;

        SetUi(string.IsNullOrEmpty(result) ? "(vacío)" : result);

        if (!string.IsNullOrEmpty(result))
        {
            onFinalResult?.Invoke(result);
            EvaluateKeywordEvents(result);
        }

        StopListeningInternal(autoRestartAfterPhrase);
    }

    private void OnPartialSpeechResult(string result)
    {
        if (!string.IsNullOrWhiteSpace(result))
        {
            lastSpeechActivityTime = Time.time;
            SetUi(result);
        }
    }

    private void EvaluateKeywordEvents(string recognizedText)
    {
        for (int i = 0; i < keywordEvents.Count; i++)
        {
            KeywordEventBinding entry = keywordEvents[i];

            if (entry == null || string.IsNullOrWhiteSpace(entry.keyword))
                continue;

            if (IsKeywordMatch(recognizedText, entry))
            {
                entry.onDetected?.Invoke();
            }
        }
    }

    private bool IsKeywordMatch(string input, KeywordEventBinding entry)
    {
        string keyword = entry.keyword.Trim();
        string text = input.Trim();

        StringComparison comparison = entry.ignoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        switch (entry.matchMode)
        {
            case KeywordMatchMode.ExactText:
                return string.Equals(text, keyword, comparison);

            case KeywordMatchMode.Contains:
                return text.IndexOf(keyword, comparison) >= 0;

            case KeywordMatchMode.WholeWord:
                RegexOptions options = entry.ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
                string pattern = $@"\b{Regex.Escape(keyword)}\b";
                return Regex.IsMatch(text, pattern, options);

            default:
                return false;
        }
    }

    private void OnSpeakStart()
    {
        Debug.Log("Talking Started...");
    }

    private void OnSpeakStop()
    {
        Debug.Log("Talking Stopped");
    }

    private void SetUi(string message)
    {
        if (uiText != null)
        {
            uiText.text = message;
        }

        Debug.Log(message);
    }
}