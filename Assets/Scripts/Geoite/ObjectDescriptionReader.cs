//using UnityEngine;
//using System.Collections;
//using System.Collections.Generic;
//using System;
//using System.IO;
//using Firebase;
//using Firebase.Firestore;
//using Firebase.Extensions;
//using UnityEngine.Networking;
//using System.Text;

//[RequireComponent(typeof(AudioSource))]
//public class ObjectDescriptionReader : MonoBehaviour
//{
//    public string collectionName = "balloons"; // or "balloons"
//    public string apiKey = ""; // Replace with your actual API key

//    private FirebaseFirestore db;
//    private AudioSource audioSource;

//    void Start()
//    {
//        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
//        {
//            if (task.Result == DependencyStatus.Available)
//            {
//                db = FirebaseFirestore.DefaultInstance;
//                audioSource = GetComponent<AudioSource>();

//                // ✅ Test REST TTS on startup
//                Speak("Hello world from Google Cloud");
//            }
//            else
//            {
//                Debug.LogError("❌ Firebase init failed: " + task.Result);
//            }
//        });
//    }

//    public void Speak(string message)
//    {
//        if (!string.IsNullOrWhiteSpace(message))
//            StartCoroutine(SendToGoogleTTS(message));
//    }

//    public void SpeakObjectDescription(string documentId)
//    {
//        StartCoroutine(FetchAndSpeak(documentId));
//    }

//    private IEnumerator FetchAndSpeak(string docId)
//    {
//        string description = null;
//        bool done = false;

//        db.Collection(collectionName).Document(docId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
//        {
//            if (!task.IsFaulted && !task.IsCanceled && task.Result.Exists)
//                task.Result.TryGetValue("object_description", out description);
//            done = true;
//        });

//        yield return new WaitUntil(() => done);

//        if (string.IsNullOrWhiteSpace(description))
//        {
//            Debug.LogWarning("❌ No object_description found.");
//            yield break;
//        }

//        StartCoroutine(SendToGoogleTTS(description));
//    }

//    private IEnumerator SendToGoogleTTS(string text)
//    {
//        string json = "{\"input\":{\"text\":\"" + text + "\"}," +
//                       "\"voice\":{\"languageCode\":\"en-US\",\"name\":\"en-US-Wavenet-D\"}," +
//                       "\"audioConfig\":{\"audioEncoding\":\"MP3\"}}";

//        UnityWebRequest www = new UnityWebRequest("https://texttospeech.googleapis.com/v1/text:synthesize?key=" + apiKey, "POST");
//        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
//        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
//        www.downloadHandler = new DownloadHandlerBuffer();
//        www.SetRequestHeader("Content-Type", "application/json");

//        yield return www.SendWebRequest();

//#if UNITY_2020_2_OR_NEWER
//        if (www.result != UnityWebRequest.Result.Success)
//#else
//        if (www.isNetworkError || www.isHttpError)
//#endif
//        {
//            Debug.LogError("TTS failed: " + www.error);
//            yield break;
//        }

//        string responseText = www.downloadHandler.text;
//        string base64Audio = JsonUtility.FromJson<TTSResponse>(responseText).audioContent;
//        byte[] audioBytes = Convert.FromBase64String(base64Audio);

//        string path = Path.Combine(Application.persistentDataPath, "tts.mp3");
//        File.WriteAllBytes(path, audioBytes);

//        using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG))
//        {
//            yield return audioRequest.SendWebRequest();

//#if UNITY_2020_2_OR_NEWER
//            if (audioRequest.result != UnityWebRequest.Result.Success)
//#else
//            if (audioRequest.isNetworkError || audioRequest.isHttpError)
//#endif
//            {
//                Debug.LogError("Audio file load failed: " + audioRequest.error);
//                yield break;
//            }

//            AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);

//            if (clip != null)
//            {
//                Debug.Log("✅ MP3 AudioClip loaded. Length: " + clip.length + " seconds");
//                audioSource.clip = clip;
//                audioSource.Play();
//            }
//            else
//            {
//                Debug.LogWarning("⚠️ AudioClip is null — failed to decode MP3.");
//            }
//        }
//    }

//    [System.Serializable]
//    public class TTSResponse
//    {
//        public string audioContent;
//    }
//}
