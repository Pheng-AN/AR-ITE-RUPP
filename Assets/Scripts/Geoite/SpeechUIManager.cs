using UnityEngine;
using UnityEngine.UI;
using Ite.Rupp.Arunity;

public class SpeechUIManager : MonoBehaviour
{
    public Button speakButton;
    // public Button replayButton;  // Assign in Inspector
    public float proximityDistance = 2.5f;



    private Geoite currentGeoite = null;
    private Transform cameraTransform;


    void Start()
    {
        cameraTransform = Camera.main.transform;
        speakButton.gameObject.SetActive(false);
        speakButton.onClick.AddListener(OnSpeakClicked);

        //replayButton.gameObject.SetActive(false);
        //replayButton.onClick.AddListener(OnReplayClicked);

    }

    void Update()
    {
        Geoite[] allGeoites = FindObjectsOfType<Geoite>();
        float closestDist = Mathf.Infinity;
        Geoite closestGeoite = null;

        foreach (var b in allGeoites)
        {
            if (b == null || b.Data == null || b.tts == null) continue;

            float dist = Vector3.Distance(cameraTransform.position, b.transform.position);
            if (dist < proximityDistance && dist < closestDist)
            {
                closestDist = dist;
                closestGeoite = b;
            }
        }

        if (closestGeoite != null)
        {
            currentGeoite = closestGeoite;
            speakButton.gameObject.SetActive(true);
            //replayButton.gameObject.SetActive(true);
        }
        else
        {
            currentGeoite = null;
            speakButton.gameObject.SetActive(false);
            //replayButton.gameObject.SetActive(false);
        }

    }
    public void SetFocusedGeoite(Geoite geoite)
    {
        currentGeoite = geoite;

        bool hasGeoite = currentGeoite != null;
        speakButton.gameObject.SetActive(hasGeoite);
        // replayButton.gameObject.SetActive(hasGeoite);
    }

    public void OnSpeakClicked()
    {
        if (currentGeoite != null)
            currentGeoite.OnSpeakClicked();
    }

    //public void OnReplayClicked()
    //{
    //    if (currentGeoite != null)
    //        currentGeoite.OnReplayClicked();
    //}

}
