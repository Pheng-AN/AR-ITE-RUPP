using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine.Networking;
using System;
using TMPro;
using Ite.Rupp.Arunity;

[System.Serializable]
public class MarkerData
{
    public string marker_id;
    public string object_code;
    public string object_name;
    public string object_description;
    public string image_url;
}


public class MarkerManager : MonoBehaviour
{
    public ARTrackedImageManager trackedImageManager;
    public GameObject defaultPrefab;
    public List<GeoiteController.GeoitePrefabEntry> prefabList;
    private Dictionary<string, GameObject> prefabDict;
    private Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();
    private FirebaseFirestore firestore;
    private MutableRuntimeReferenceImageLibrary mutableLibrary;
    private Dictionary<string, MarkerData> markerDict = new Dictionary<string, MarkerData>();


    void Awake()
    {
        firestore = FirebaseFirestore.DefaultInstance;

        prefabDict = new Dictionary<string, GameObject>();
        foreach (var entry in prefabList)
        {
            if (!prefabDict.ContainsKey(entry.ObjectCode))
                prefabDict.Add(entry.ObjectCode, entry.Prefab);
        }
    }

    void Start()
    {
        var library = trackedImageManager.referenceLibrary as MutableRuntimeReferenceImageLibrary;
        if (library == null)
        {
            Debug.LogError("You must enable 'Use Mutable Library' in the ReferenceImageLibrary.");
            return;
        }

        mutableLibrary = library;
        LoadMarkersFromFirebase();
    }

    void LoadMarkersFromFirebase()
    {
        firestore.Collection("markers").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Failed to load markers from Firebase.");
                return;
            }

            foreach (var doc in task.Result.Documents)
            {
                var data = doc.ToDictionary();

                MarkerData marker = new MarkerData
                {
                    marker_id = data["marker_id"].ToString(),
                    object_code = data["object_code"].ToString(),
                    object_name = data["object_name"].ToString(),
                    object_description = data["object_description"].ToString(),
                    image_url = data["image_url"].ToString()
                };

                markerDict[marker.marker_id] = marker;

                StartCoroutine(DownloadAndAddImage(marker));
            }
        });
    }

    IEnumerator DownloadAndAddImage(MarkerData marker)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(marker.image_url);
        yield return www.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
    if (www.result != UnityWebRequest.Result.Success)
#else
        if (www.isNetworkError || www.isHttpError)
#endif
        {
            Debug.LogError("Failed to download marker image: " + www.error);
            yield break;
        }

        Texture2D tex = DownloadHandlerTexture.GetContent(www);
        if (tex == null)
        {
            Debug.LogError("Texture is null.");
            yield break;
        }

        mutableLibrary.ScheduleAddImageWithValidationJob(tex, marker.marker_id, 0.62f); // Adjust size
    }


    void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var trackedImage in args.added)
            ShowPrefab(trackedImage);
        foreach (var trackedImage in args.updated)
            ShowPrefab(trackedImage);
        foreach (var trackedImage in args.removed)
            HidePrefab(trackedImage);
    }

    void ShowPrefab(ARTrackedImage trackedImage)
    {
        string name = trackedImage.referenceImage.name;

        if (!spawnedPrefabs.ContainsKey(name))
        {
            GameObject prefabToSpawn = prefabDict.ContainsKey(name) ? prefabDict[name] : defaultPrefab;
            GameObject obj = Instantiate(prefabToSpawn, trackedImage.transform);
            spawnedPrefabs[name] = obj;

            // 🔊 Trigger TTS here
            TextToSpeechGoogle tts = FindObjectOfType<TextToSpeechGoogle>();
            if (tts != null)
            {
                tts.SpeakObjectDescriptionFromMarker(name); // name should match your marker_id
            }
        }
        else
        {
            spawnedPrefabs[name].SetActive(true);
        }

        spawnedPrefabs[name].transform.position = trackedImage.transform.position;
        spawnedPrefabs[name].transform.rotation = trackedImage.transform.rotation;
    }




    void HidePrefab(ARTrackedImage trackedImage)
    {
        string name = trackedImage.referenceImage.name;
        if (spawnedPrefabs.ContainsKey(name))
        {
            spawnedPrefabs[name].SetActive(false);
        }
    }
}
