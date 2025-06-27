// ✅ Google TTS Client + Firebase object_description fetcher

using UnityEngine;
using UnityEngine.Networking;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections;
using System.Text;
using System;
using System.IO;
using System.Collections.Generic;
using Ite.Rupp.Arunity;

[RequireComponent(typeof(AudioSource))]
public class TextToSpeechGoogle : MonoBehaviour
{
    [Header("Google Cloud Text-to-Speech")]
    public string apiKey = "";
    public string audioEncoding = "MP3"; // or "LINEAR16"

    private AudioSource audioSource;
    private FirebaseFirestore db;


    // Keeps track of which geoite_ids have already spoken and their text
    //private Dictionary<string, string> spokenGeoites = new Dictionary<string, string>();

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        db = FirebaseFirestore.DefaultInstance;
    }

    public void Speak(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
            StartCoroutine(SendToGoogleTTS(text));
    }

    public void SpeakObjectDescriptionFromMarker(string documentId)
    {
        // Called externally by button, not automatically
        db.Collection("markers").Document(documentId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsFaulted && task.Result.Exists)
            {
                string desc = task.Result.ContainsField("object_description") ? task.Result.GetValue<string>("object_description") : "";
                if (!string.IsNullOrEmpty(desc))
                {
                    Speak(desc);
                }
            }
        });
    }

    public void SpeakObjectDescriptionFromGeoite(string documentId)
    {
        // Called externally by button, not automatically
        //if (spokenGeoites.ContainsKey(documentId))
        //{
        //    Debug.Log("🔁 Geoite " + documentId + " has already spoken. Use ReplayLastSpoken() to replay.");
        //    return;
        //}

        db.Collection("geoites").Document(documentId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsFaulted && task.Result.Exists)
            {
                string desc = task.Result.ContainsField("object_description") ? task.Result.GetValue<string>("object_description") : "";
                if (!string.IsNullOrEmpty(desc))
                {
                    //spokenGeoites[documentId] = desc;
                    Speak(desc);
                }
            }
        });
    }

    //public void ReplayLastSpoken(string documentId)
    //{
    //    if (spokenGeoites.ContainsKey(documentId))
    //    {
    //        Speak(spokenGeoites[documentId]);
    //    }
    //}

    public string GetGeoiteIdFromGameObject(GameObject geoiteObj)
    {
        var geoite = geoiteObj.GetComponentInChildren<Geoite>();
        if (geoite != null && geoite.Data != null)
        {
            return geoite.Data.geoite_id;
        }
        return null;
    }

    IEnumerator SendToGoogleTTS(string text)
    {
        string json = "{\"input\":{\"text\":\"" + text + "\"}," +
                       "\"voice\":{\"languageCode\":\"en-US\",\"name\":\"en-US-Wavenet-D\"}," +
                       "\"audioConfig\":{\"audioEncoding\":\"" + audioEncoding + "\"}}";

        UnityWebRequest www = new UnityWebRequest("https://texttospeech.googleapis.com/v1/text:synthesize?key=" + apiKey, "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
        if (www.result != UnityWebRequest.Result.Success)
#else
        if (www.isNetworkError || www.isHttpError)
#endif
        {
            Debug.LogError("TTS failed: " + www.error);
            yield break;
        }

        var base64 = JsonUtility.FromJson<TTSResponse>(www.downloadHandler.text).audioContent;
        byte[] audioData = Convert.FromBase64String(base64);
        string ext = audioEncoding == "LINEAR16" ? ".wav" : ".mp3";
        string path = Path.Combine(Application.persistentDataPath, "tts_output" + ext);
        File.WriteAllBytes(path, audioData);

        AudioType type = audioEncoding == "LINEAR16" ? AudioType.WAV : AudioType.MPEG;
        UnityWebRequest audioReq = UnityWebRequestMultimedia.GetAudioClip("file://" + path, type);
        yield return audioReq.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
        if (audioReq.result != UnityWebRequest.Result.Success)
#else
        if (audioReq.isNetworkError || audioReq.isHttpError)
#endif
        {
            Debug.LogError("Audio file load failed: " + audioReq.error);
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(audioReq);
        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    [Serializable]
    public class TTSResponse { public string audioContent; }
}
