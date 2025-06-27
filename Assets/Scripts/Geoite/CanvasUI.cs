namespace Ite.Rupp.Arunity
{
    using UnityEngine;
    using UnityEngine.XR.ARSubsystems;

    public class CanvasUI : MonoBehaviour
    {
        public GeoiteController geoiteController;
        
        public GameplayUI gameplayUI;
        public GameObject locatingUI;
        public GameObject privacyPrompt;

        /// <summary>
        /// The key name used in PlayerPrefs which indicates whether the start info has displayed
        /// at least one time.
        /// </summary>
        private const string _hasDisplayedStartInfoKey = "HasDisplayedStartInfo";

        // Start is called before the first frame update
        void Start() 
        {
            if (Application.isEditor) PlayerPrefs.DeleteKey(_hasDisplayedStartInfoKey);
            DisplayPrivacyPromptIfNecessary();
        }

        public void EarthAnchorsTrackingStateChanged(TrackingState newTrackingState) 
        {
            gameplayUI.gameObject.SetActive(newTrackingState == TrackingState.Tracking);
            locatingUI.SetActive(newTrackingState != TrackingState.Tracking);
        }

        /// <summary>
        /// Switch to privacy prompt, and disable all other screens.
        /// </summary>
        public void DisplayPrivacyPromptIfNecessary()
        {
            if (PlayerPrefs.HasKey(_hasDisplayedStartInfoKey))
            {
                SwitchToARView();
                return;
            }
            privacyPrompt.SetActive(true);
            gameplayUI.gameObject.SetActive(false);
            locatingUI.SetActive(false);
        }

        // ####################################################
        // Button Actions (Get Started and Learn More)

        /// <summary>
        /// Switch to AR view, and disable all other screens.
        /// </summary>
        public void SwitchToARView()
        {
            PlayerPrefs.SetInt(_hasDisplayedStartInfoKey, 1);
            
            privacyPrompt.SetActive(false);
            if (Application.isEditor) {
                gameplayUI.gameObject.SetActive(true);
                locatingUI.SetActive(false);
            } else {
                gameplayUI.gameObject.SetActive(false);
                locatingUI.SetActive(true);
            }

            geoiteController.SetPlatformActive(true);
        }

        /// <summary>
        /// Callback handling "Learn More" Button click event in Privacy Prompt.
        /// </summary>
        public void OnLearnMoreButtonClicked()
        {
            Debug.Log("Learn More clicked. Attempting to open URL...");
            Application.OpenURL(
                "https://developers.google.com/ar/cloud-anchors-privacy");
        }

        // ####################################################
    }
}