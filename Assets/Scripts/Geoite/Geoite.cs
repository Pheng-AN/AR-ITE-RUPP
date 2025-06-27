namespace Ite.Rupp.Arunity
{
    using System;
    using System.Collections;
    // using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using ElRaccoone.Tweens;
    using ElRaccoone.Tweens.Core;
    using TMPro;
    using UnityEngine.UI;
    using UnityEngine.Networking;

    [Serializable] public class GeoiteEvent : UnityEvent<Geoite> { }



    public class Geoite : MonoBehaviour//, GeoiteDataVisualChangesListener //, IFocusablePrefab
    {

        public TextMeshProUGUI nameText; // ← drag your Text_Title into this

        public TextMeshProUGUI descriptionText; // Assign in Inspector


        public TextToSpeechGoogle tts;

        public string ObjectCode => _Data?.object_code;


        static float GEOITE_FADE_MIN_DIST_TO_CAM = 10f;


        static float GEOITE_FADE_MAX_DIST_TO_CAM = 14f;


        static float DIST_TO_CAM_RANGE = GEOITE_FADE_MAX_DIST_TO_CAM - GEOITE_FADE_MIN_DIST_TO_CAM;



        static public float ESTIMATED_CAM_HEIGHT_FROM_FLOOR = 1.3f;


        public Transform geoiteRoot;

        public GeoiteData _Data = null;
        public GeoiteData Data { get { return _Data; } }



        private bool _debugCanvasVisible = false;


        public TextMeshProUGUI debugText;


        void Awake()
        {


            //geoiteWasPopped = new GeoiteEvent();
            _Data = new GeoiteData();
            //_Data.SetVisualChangesListener(this);


        }



        void Update()
        {


            if (!Application.isEditor)
            {
                // Make sure the position and scale of the Geoite is correct
                Vector3 p = this.geoiteRoot.localPosition;
                p.x = 0;
                p.z = 0;
                // Leave yPos alone for the SetGeoiteWorldYPos() method
                this.geoiteRoot.localPosition = p;
            }

            // GeoiteRoot.localPosition = Vector3.zero;
            geoiteRoot.localScale = Vector3.one;
        }




        /// <param name="camYPosWorld">The world y position of the camera</param>
        /// <param name="distToCamera">The distance of this Geoite to the camera</param>
        /// <param name="camPos">The world position of the camera</param>
        /// <param name="adjustFade">Should the fade of this Geoite be adjusted?</param>
        public void UpdateGeoiteCamYPosFadeAndDistToCamera(
            float camYPosWorld, float distToCamera, Vector3 camPos, bool adjustFade)
        {

            float distPercent = Mathf.Max(0, Mathf.Min(1f,
                                    (distToCamera - GEOITE_FADE_MIN_DIST_TO_CAM) / DIST_TO_CAM_RANGE
                                ));

            float contribution = (1f - distPercent);



            Vector3 p = this.geoiteRoot.position;


            p.y = (camYPosWorld - ESTIMATED_CAM_HEIGHT_FROM_FLOOR);

            this.geoiteRoot.position = p;


            if (!DebugSettings.Shared.DisplayGeoiteDebug) return;

            GameObject canvasGO = debugText.transform.parent.parent.gameObject;
            if (!_debugCanvasVisible && !canvasGO.activeSelf)
            {
                _debugCanvasVisible = true;
                StartCoroutine(ShowDebugCanvas(0.1f));
            }

            // UPDATE THE DEBUG TEXT
            float angle = -90f + (-Mathf.Rad2Deg * Mathf.Atan2(camPos.z - debugText.transform.parent.parent.position.z,
                                            camPos.x - debugText.transform.parent.parent.position.x));
            debugText.transform.parent.parent.rotation = Quaternion.Euler(0, angle, 0);

            float strLength = this._Data != null ? this._Data.geoite_string_length : 0;
            float numPopped = this._Data != null ? this._Data.num_popped : 0;

            debugText.text = string.Format("Dist to cam: {0:00.00}m\nmin:{1}m, max:{2}m\n", distToCamera, GEOITE_FADE_MIN_DIST_TO_CAM, GEOITE_FADE_MAX_DIST_TO_CAM) +
                             string.Format("Fade:{0:0.0} ", contribution) +
                             string.Format("Rope: {0:0.0}", strLength);
        }

        /// <param name="delay">Delay time</param>
        public IEnumerator ShowDebugCanvas(float delay)
        {
            yield return new WaitForSeconds(delay);
            GameObject canvasGO = debugText.transform.parent.parent.gameObject;
            canvasGO.SetActive(true);
        }


        public void SetGeoiteData(GeoiteData newGeoiteData)
        {
            _Data = newGeoiteData;
            //_Data.SetVisualChangesListener(this);


            // ✅ Display object_name (title) if text field exists
            if (nameText != null)
                nameText.text = _Data.object_name;

            // ✅ Display object_description (detail)
            if (descriptionText != null)
                descriptionText.text = _Data.object_description;


        }

        public void OnSpeakClicked()
        {
            if (tts != null && Data != null && !string.IsNullOrEmpty(Data.geoite_id))
            {
                tts.SpeakObjectDescriptionFromGeoite(Data.geoite_id);
            }
        }


        /// <param name="oldPoppedUntil">Old popped until datetime</param>
        /// <param name="poppedUntil">New popped until datetime</param>
        /// <param name="poppedByUserID">New popped-by userID</param>
        /// <param name="curUserID">The current user ID</param>

        /// <param name="animIn">Animate it in or out?</param>

        /// <param name="height">The height </param>

        /// <param name="percent">The percent to fully inflated</param>

        /// <param name="delay">Delay time</param>

        /// <param name="col">The new pop cloud colour</param>

        /// <param name="cloudColor">The color of the pop clouds</param>

        /// <param name="delay">Delay until hiding</param>

        /// <param name="other">The other collider</param>


    }
}