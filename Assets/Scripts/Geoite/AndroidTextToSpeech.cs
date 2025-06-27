//using UnityEngine;
//using System.Collections.Generic;
//using System.Collections;

//public class AndroidTextToSpeech : MonoBehaviour
//{
//#if UNITY_ANDROID && !UNITY_EDITOR
//    private AndroidJavaObject tts;
//#endif

//    void Start()
//    {
//#if UNITY_ANDROID && !UNITY_EDITOR
//        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
//        AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

//        tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", context, new TTSInitListener());
//#endif
//    }

//    public void Speak(string message)
//    {
//#if UNITY_ANDROID && !UNITY_EDITOR
//        if (tts != null)
//        {
//            tts.Call<int>("speak", message, 0, null, null);
//        }
//#endif
//    }

//#if UNITY_ANDROID && !UNITY_EDITOR
//    private class TTSInitListener : AndroidJavaProxy
//    {
//        public TTSInitListener() : base("android.speech.tts.TextToSpeech$OnInitListener") { }

//        public void onInit(int status)
//        {
//            Debug.Log("🔈 Android TTS initialized with status: " + status);
//        }
//    }
//#endif
//}
