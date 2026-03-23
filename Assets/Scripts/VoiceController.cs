using System;
using System.Collections;
using TextSpeech;
using TMPro;
using UnityEngine;
using UnityEngine.Android;

public class VoiceController : MonoBehaviour
{
    private const string LANG_CODE = "en-US";
    [SerializeField] private TMP_Text uiText;

    private void Start()
    {
        if (uiText == null)
        {
            Debug.LogError("uiText no está asignado en el Inspector");
            return;
        }

        uiText.text = "Start entrando";
        Debug.Log("Start entrando");

#if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(InitAndroidVoice());
#else
        uiText.text = "No Android / Editor";
        Debug.LogWarning("Plugin de voz: prueba real en Android, no en Editor");
#endif
    }

    private IEnumerator InitAndroidVoice()
    {
        // 1) Callbacks primero
        try
        {
            SpeechToText.Instance.onPartialResultsCallback = OnPartialSpeechResult;
            SpeechToText.Instance.onResultCallback = OnFinalSpeechResult;
            TextToSpeech.Instance.onStartCallBack = OnSpeakStart;
            TextToSpeech.Instance.onDoneCallback = OnSpeakStop;

            uiText.text = "Callbacks OK";
            Debug.Log("Callbacks OK");
        }
        catch (Exception e)
        {
            Debug.LogError("Error registrando callbacks: " + e);
            uiText.text = "Error callbacks: " + e.Message;
            yield break;
        }

        // 2) Permiso de micro antes de STT
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            uiText.text = "Pidiendo permiso micrófono";
            Permission.RequestUserPermission(Permission.Microphone);

            // Espera hasta que el usuario responda
            yield return new WaitForSeconds(0.5f);

            float timeout = 10f;
            while (timeout > 0f && !Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                timeout -= 0.5f;
                yield return new WaitForSeconds(0.5f);
            }
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            uiText.text = "Permiso micrófono denegado";
            Debug.LogError("Permiso de micrófono denegado");
            yield break;
        }

        // 3) Inicializa TTS y STT por separado
        try
        {
            uiText.text = "Init TTS...";
            Debug.Log("Init TTS...");
            TextToSpeech.Instance.Setting(LANG_CODE, 1, 1);
            Debug.Log("TTS OK");
        }
        catch (Exception e)
        {
            Debug.LogError("Error en TTS.Setting: " + e);
            uiText.text = "Error TTS: " + e.Message;
            yield break;
        }

        try
        {
            uiText.text = "Init STT...";
            Debug.Log("Init STT...");
            SpeechToText.Instance.Setting(LANG_CODE);
            Debug.Log("STT OK");
        }
        catch (Exception e)
        {
            Debug.LogError("Error en STT.Setting: " + e);
            uiText.text = "Error STT: " + e.Message;
            yield break;
        }

        uiText.text = "Voice OK";
        Debug.Log("Voice OK");
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
            uiText.text = "Error Speak: " + e.Message;
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
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            uiText.text = "Sin permiso de micrófono";
            return;
        }

        try
        {
            SpeechToText.Instance.StartRecording();
            uiText.text = "Escuchando...";
        }
        catch (Exception e)
        {
            Debug.LogError("Error StartListening: " + e);
            uiText.text = "Error Listen: " + e.Message;
        }
#endif
    }

    public void StopListening()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            SpeechToText.Instance.StopRecording();
        }
        catch (Exception e)
        {
            Debug.LogError("Error StopListening: " + e);
        }
#endif
    }

    private void OnFinalSpeechResult(string result)
    {
        uiText.text = result;
    }

    private void OnPartialSpeechResult(string result)
    {
        uiText.text = result;
    }

    private void OnSpeakStart()
    {
        Debug.Log("Talking Started...");
    }

    private void OnSpeakStop()
    {
        Debug.Log("Talking Stopped");
    }
}