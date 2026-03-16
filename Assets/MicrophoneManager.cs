using UnityEngine;
using UnityEngine.Android;
using System.Collections;

public class MicrophoneManager : MonoBehaviour
{
    AudioClip micClip;
    string micDevice;

    void Start()
    {
        Debug.LogError("🔴 MicManager START");

        StartCoroutine(InitMic());
    }

    IEnumerator InitMic()
    {
        Debug.LogError("🔴 Checking microphone permission...");

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Debug.LogError("🔴 Microphone permission NOT granted");

            Permission.RequestUserPermission(Permission.Microphone);

            Debug.LogError("🔴 Waiting for user to respond to permission popup...");

            while (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Debug.LogError("🔴 Still waiting for microphone permission...");
                yield return new WaitForSeconds(1f);
            }
        }

        Debug.LogError("🔴 Microphone permission GRANTED");

        DetectMicrophones();

        StartMicrophone();
    }

    void DetectMicrophones()
    {
        Debug.LogError("🔴 Detecting microphones...");

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("🔴 NO MICROPHONES DETECTED");
            return;
        }

        Debug.LogError("🔴 Number of microphones found: " + Microphone.devices.Length);

        foreach (var device in Microphone.devices)
        {
            Debug.LogError("🔴 Mic device found: " + device);
        }

        micDevice = Microphone.devices[0];

        Debug.LogError("🔴 Using microphone: " + micDevice);
    }

    void StartMicrophone()
    {
        if (string.IsNullOrEmpty(micDevice))
        {
            Debug.LogError("🔴 Cannot start microphone: device is null");
            return;
        }

        Debug.LogError("🔴 Starting microphone recording...");

        micClip = Microphone.Start(micDevice, true, 10, 44100);

        StartCoroutine(CheckMicWorking());
    }

    IEnumerator CheckMicWorking()
    {
        Debug.LogError("🔴 Checking if microphone started correctly...");

        yield return new WaitForSeconds(2f);

        if (Microphone.IsRecording(micDevice))
        {
            Debug.LogError("🔴 MICROPHONE IS RECORDING SUCCESSFULLY");
        }
        else
        {
            Debug.LogError("🔴 MICROPHONE FAILED TO START");
        }
    }
}