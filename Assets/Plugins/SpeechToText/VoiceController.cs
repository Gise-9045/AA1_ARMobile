using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;



public class VoiceController : MonoBehaviour, ISpeechToTextListener
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
    [SerializeField] private string languageCode = "es-ES";
    [SerializeField] private Text uiText;

    [Header("Flow")]
    [SerializeField] private bool autoActivate = true;
    [SerializeField] private bool autoRestartAfterResult = true;
    [SerializeField] private bool preferOfflineRecognition = true;
    [SerializeField] private bool useFreeFormLanguageModel = true;
    [SerializeField] private float restartDelay = 0.35f;

    [Header("Keywords")]
    [SerializeField] private List<KeywordEventBinding> keywordEvents = new();
    [SerializeField] private bool stopAfterFirstKeywordMatch = true;
    [SerializeField] private StringEvent onFinalResult;

    private bool isInitialized;
    private bool isListening;
    private Coroutine restartCoroutine;
    private float lastVoiceTime;

    private void Start()
    {
        InitializeSpeech();
    }

    private void OnDisable()
    {
        if (restartCoroutine != null)
        {
            StopCoroutine(restartCoroutine);
            restartCoroutine = null;
        }

        if (SpeechToText.IsBusy())
        {
            SpeechToText.Cancel();
        }
    }

    private void InitializeSpeech()
    {
        bool ok = SpeechToText.Initialize(languageCode);

        if (!ok)
        {
            SetUi("No se pudo inicializar SpeechToText");
            return;
        }

        isInitialized = true;
        SetUi("SpeechToText listo");

        if (autoActivate)
        {
            StartListening();
        }
    }

    public void StartListening()
    {
        if (!isInitialized)
        {
            InitializeSpeech();
            if (!isInitialized)
                return;
        }

        if (!SpeechToText.IsServiceAvailable(preferOfflineRecognition))
        {
            SetUi(preferOfflineRecognition
                ? "Servicio STT offline no disponible"
                : "Servicio STT no disponible");
            return;
        }

        if (SpeechToText.IsBusy())
        {
            return;
        }

        SpeechToText.RequestPermissionAsync(permission =>
        {
            if (permission != SpeechToText.Permission.Granted)
            {
                SetUi("Permiso de micrófono denegado");
                return;
            }

            bool started = SpeechToText.Start(
                this,
                useFreeFormLanguageModel: useFreeFormLanguageModel,
                preferOfflineRecognition: preferOfflineRecognition
            );

            if (started)
            {
                isListening = true;
                SetUi("Escuchando...");
            }
            else
            {
                isListening = false;
                SetUi("No se pudo arrancar la escucha");
            }
        });
    }

    public void StopListening()
    {
        if (!SpeechToText.IsBusy())
        {
            isListening = false;
            return;
        }

        SpeechToText.ForceStop();
    }

    private void ScheduleRestart()
    {
        if (!autoRestartAfterResult)
            return;

        if (restartCoroutine != null)
        {
            StopCoroutine(restartCoroutine);
        }

        restartCoroutine = StartCoroutine(RestartRoutine());
    }

    private IEnumerator RestartRoutine()
    {
        yield return new WaitForSeconds(restartDelay);

        restartCoroutine = null;

        if (!SpeechToText.IsBusy())
        {
            StartListening();
        }
    }

    public void OnReadyForSpeech()
    {
        lastVoiceTime = Time.time;
        Debug.Log("OnReadyForSpeech");
    }

    public void OnBeginningOfSpeech()
    {
        lastVoiceTime = Time.time;
        SetUi("Hablando...");
        Debug.Log("OnBeginningOfSpeech");
    }

    public void OnVoiceLevelChanged(float normalizedVoiceLevel)
    {
        lastVoiceTime = Time.time;
        // Si quieres animar UI aquí, perfecto
    }

    public void OnPartialResultReceived(string spokenText)
    {
        if (!string.IsNullOrWhiteSpace(spokenText))
        {
            SetUi(spokenText);
        }
    }

    public void OnResultReceived(string spokenText, int? errorCode)
    {
        isListening = false;

        string result = spokenText?.Trim() ?? string.Empty;

        Debug.Log($"OnResultReceived => '{result}' error={errorCode}");

        if (errorCode.HasValue)
        {
            // 0 = cancelado manualmente
            // 6 = timeout/sin habla
            // 9 = permiso del Google app en algunos Android
            if (errorCode.Value == 0)
            {
                SetUi("Escucha cancelada");
                return;
            }

            if (errorCode.Value == 6)
            {
                SetUi("Sin voz detectada");
                ScheduleRestart();
                return;
            }

            if (errorCode.Value == 9)
            {
                SetUi("Falta permiso de micrófono en Google app");
                return;
            }

            SetUi("Error STT: " + errorCode.Value);
            ScheduleRestart();
            return;
        }

        if (string.IsNullOrEmpty(result))
        {
            SetUi("(sin resultado)");
            ScheduleRestart();
            return;
        }

        SetUi(result);
        onFinalResult?.Invoke(result);
        EvaluateKeywordEvents(result);

        ScheduleRestart();
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

                if (stopAfterFirstKeywordMatch)
                    return;
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

    private void SetUi(string message)
    {
        if (uiText != null)
        {
            uiText.text = message;
        }

        Debug.Log(message);
    }
}