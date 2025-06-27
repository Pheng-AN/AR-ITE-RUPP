namespace Ite.Rupp.Arunity
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Events;
    using UnityEngine.XR.ARFoundation;
    using UnityEngine.XR.ARSubsystems;
    using Google.XR.ARCoreExtensions;



    [Serializable] public class GeoiteCountEvent : UnityEvent<int> { }

  
    [Serializable] public class EarthTrackingEvent : UnityEvent<TrackingState> { }

    public struct GeoiteAnchor
    {
        public ARGeospatialAnchor anchor;
        public Geoite geoite;
        public GeoiteAnchor(ARGeospatialAnchor a, Geoite b)
        {
            this.anchor = a;
            this.geoite = b;
        }
    }

    /// <summary>
    /// Controls the GeoitePop example.
    /// </summary>
    [RequireComponent(typeof(ARSessionOrigin), typeof(ARAnchorManager))]
    public class GeoiteController : MonoBehaviour
    {
        [Serializable]
        public class GeoitePrefabEntry
        {
            public string ObjectCode;
            public GameObject Prefab;
        }

        [SerializeField]
        private List<GeoitePrefabEntry> GeoitePrefabList; // Populated via Inspector

        private Dictionary<string, GameObject> _GeoitePrefabDict;



        public string object_code; // e.g., "red", "blue", "cat", "rocket"

       


        /// <summary>
        /// The ARCoreExtensions component used in this scene.
        /// </summary>
        public ARCoreExtensions ARCoreExtensions;

        /// <summary>
        /// The ARSession used in the example.
        /// </summary>
        public ARSession SessionCore;

        /// <summary>
        /// A bool to determine if ARTracking is currently active
        /// </summary>
        private bool _trackingIsActive = false;

        /// <summary>
        /// Assign the Unity Camera in the Editor
        /// </summary>
        public Camera CameraUnity;

        /// <summary>
        /// An object that contains logic to communicate with the server
        /// that manages a database of Geoites
        /// </summary>
        private GeoitesNetworking _network;

        /// <summary>
        /// The model used to visualize anchors.
        /// </summary>
        public GameObject AnchorVisObjectPrefab;

        /// <summary>
        /// The last created anchors.
        /// </summary>
        private List<GeoiteAnchor> _anchors;

        /// <summary>
        /// An event called when the list of Anchors is changed
        /// </summary>
        public GeoiteCountEvent GeoiteAnchorCountChanged;

        /// <summary>
        /// How long to wait between each MapTile Check
        /// </summary>
        static float MAP_TILE_MONITORING_CHECK_INTERVAL = 1.0f;

        /// <summary>
        /// The MapTile that is currently being monitored on the network
        /// </summary>
        private Mercator.MapTile? _monitoringMapTile = null;

        /// <summary>
        /// The next time to check if the user has moved to a different map tile
        /// </summary>
        float _nextMapTileCheckTime = float.MaxValue;

        /// <summary>
        /// The active ARSessionOrigin
        /// </summary>
        private ARSessionOrigin _sessionOrigin;

        /// <summary>
        /// The active AREarthManager
        /// </summary>
        private AREarthManager _earthManager;

        
        /// The active ARAnchorManager
     
        private ARAnchorManager _anchorManager;

      
        /// The last recorded EarthManager TrackingState
    
        private TrackingState _lastEarthTrackingState;

        /// <summary>
        /// An event invoked when the EarthManager TrackingState has changed
        /// </summary>
        public EarthTrackingEvent EarthTrackingStateChanged;

        
       
        /// A UI Text object that will receive the Earth status text.
    
        public Text EarthStatusTextUI;


        /// A UI Text object that will receive feedback to the user.
        
        public Text FeedbackTextUI;

      
        /// UI element to display <see cref="ARSessionState"/>.
       
        public Text SessionState;

        /// UI element to display <see cref="ARGeospatialAnchor"/> status.
       
        public Text EarthAnchorText;
        // --------------------------






        public void Awake()
        {
            //ttsManager = FindObjectOfType<TextToSpeechGoogle>();

            _GeoitePrefabDict = new Dictionary<string, GameObject>();
            foreach (var entry in GeoitePrefabList)
            {
                if (!_GeoitePrefabDict.ContainsKey(entry.ObjectCode))
                {
                    _GeoitePrefabDict.Add(entry.ObjectCode, entry.Prefab);
                }
            }


            _anchors = new List<GeoiteAnchor>();

            UpdateFeedbackText("", error: false);

            _sessionOrigin = GetComponent<ARSessionOrigin>();
            if (_sessionOrigin == null)
            {
                Debug.LogError("Failed to find ARSessionOrigin.");
                UpdateFeedbackText("Failed to find ARSessionOrigin", error: true);
            }

            _anchorManager = GetComponent<ARAnchorManager>();
            if (_anchorManager == null)
            {
                Debug.LogError("Failed to find ARAnchorManager.");
                UpdateFeedbackText("Failed to find ARAnchorManager", error: true);
            }

            GameObject earthManagerGO = new GameObject("AREarthManager", typeof(AREarthManager));
            _earthManager = earthManagerGO.GetComponent<AREarthManager>();
            if (_earthManager == null)
            {
                Debug.LogError("Failed to initialize AREarthManager");
                UpdateFeedbackText("Failed to initialize AREarthManage", error: true);
            }

            _network = new GeoitesNetworking();
            _network.FirebaseReadyChangedEvent += FirebaseReadyStateChanged;

            _lastEarthTrackingState = TrackingState.None;
        }

     
        void Start()
        {
            SessionState.gameObject.SetActive(DebugSettings.Shared.DisplayEarthDebug);
            EarthAnchorText.gameObject.SetActive(
                DebugSettings.Shared.DisplayEarthDebug &&
                _earthManager != null);
            FeedbackTextUI.transform.parent.gameObject.SetActive(
                DebugSettings.Shared.DisplayEarthDebug);
        }

       
        private void FirebaseReadyStateChanged(bool firebaseReady)
        {
            // Debug.Log($"FirebaseReadyStateChanged: {firebaseReady}");
            if (!firebaseReady) return;

            // Only SetupGeoiteMonitoring once EarthTracking has started
            if (Application.isEditor)
            {
                this.SetupGeoiteNearbyMonitoring();
                UpdateFeedbackText($"Started geoite monitoring. Total anchors: {_anchors.Count}", false);
            }
        }

    
        public void SetPlatformActive(bool active)
        {
            _trackingIsActive = active;
            _sessionOrigin.enabled = active;
            SessionCore.gameObject.SetActive(active);
            ARCoreExtensions.gameObject.SetActive(active);
        }

       
        public void IsBuildOrPlayChanged(bool isBuildOrPlay, bool shouldAnimate)
        {
            // _listenForTouches = isBuildOrPlay;
        }

       
        public void DeleteAllGeoitesInTheUsersLocalArea()
        {
            // Make sure we can get an accurate CameraEarthPose
            if (_earthManager == null || _earthManager.EarthTrackingState != TrackingState.Tracking) return;

            GeospatialPose geoPose = _earthManager.CameraGeospatialPose;
            double lat = geoPose.Latitude;
            double lng = geoPose.Longitude;

            _network.DeleteGeoitesAroundUser(
                lat, lng,
                (errString) => {
                    Debug.Log("DeleteGeoitesAroundUser Err:" + errString);
                    UpdateFeedbackText($"DeleteGeoitesAroundUser Err:" + errString,
                        error: errString != null);
                    if (errString != null) return;

                    this.DeleteAllBallonAnchors();
                });
        }

      
        void UpdateOrCreateGeoiteChangeFromServer(GeoiteChange geoiteChange, int changeNumber)
        {
            object geoiteID = "";
            if (!geoiteChange.Dict.TryGetValue("geoite_id", out geoiteID))
            {
                Debug.LogWarning("NO Geoite ID");
                UpdateFeedbackText($"NO Geoite ID. Total anchors: {_anchors.Count}", error: true);
                return;
            }

            object lat = "";
            object lng = "";
            if (!geoiteChange.Dict.TryGetValue("latitude", out lat) ||
                !geoiteChange.Dict.TryGetValue("longitude", out lng)) return;
            Mercator.GeoCoordinate geoiteChangeCoord = new Mercator.GeoCoordinate((double)lat, (double)lng);

            string bIDStr = (string)geoiteID;
            foreach (GeoiteAnchor ba in _anchors)
            {
                // Check if the current user just created this geoite
                bool createdByUser = ba.geoite.Data.user_id == _network.CurrentUserID;
                bool currentUserProbablyJustCreated =
                    (createdByUser &&
                     ba.geoite.Data.geoite_id.Length < 1 &&
                     ba.geoite.Data.coordinate.GetDistanceTo(geoiteChangeCoord) < 0.01);

                if (bIDStr == ba.geoite.Data.geoite_id || currentUserProbablyJustCreated)
                {
                    // Geoite FOUND!
                    if (geoiteChange.ChangeType == Firebase.Firestore.DocumentChange.Type.Added)
                    {
                        if (!createdByUser)
                        {
                            UpdateFeedbackText($"Geoite Added NOT from curUser. Total anchors: {_anchors.Count}", error: true);
                        }
                        // Geoite ADDED! But we already have it :\
                        // This could happen when the user moves between MapTiles
                    }
                    else if (geoiteChange.ChangeType == Firebase.Firestore.DocumentChange.Type.Modified)
                    {
                        // Debug.Log("Geoite Updated");
                        ba.geoite.Data.UpdateWithDict(geoiteChange.Dict, _network.CurrentUserID);
                    }
                    else if (geoiteChange.ChangeType == Firebase.Firestore.DocumentChange.Type.Removed)
                    {
                        // Debug.Log("geoite Deleted");
                        Destroy(ba.anchor.gameObject);
                        _anchors.Remove(ba);
                    }

                    return;
                }
            }

        
            Debug.Log("Creating Geoite Anchor");
            StartCoroutine(CreateGeoiteAnchorCoroutine(geoiteChange.Dict, changeNumber));
        }

        
        public IEnumerator CreateGeoiteAnchorCoroutine(Dictionary<string, object> geoiteDict, int changeNumber)
        {
            yield return new WaitForSeconds((float)changeNumber * 0.01f);

            Debug.Log("Creating Geoite Anchor");
            GeoiteData newGeoiteDat = new GeoiteData(geoiteDict);

            // Debug.Log("Earth.TrackingState: " + Earth.TrackingState);
            yield return null; // Wait for one frame

            double lat = newGeoiteDat.latitude;
            double lng = newGeoiteDat.longitude;
            double alt = newGeoiteDat.altitude;
            Quaternion anchorRot = Quaternion.AngleAxis(0, new Vector3(0.0f, 1.0f, 0.0f));

            ARGeospatialAnchor newAnchor = _anchorManager.AddAnchor(lat, lng, alt, anchorRot);

            if (newAnchor != null || Application.isEditor)
            {
                yield return null; // Wait for one frame
                GeoiteAnchor newBA = CreateNewGeoiteAnchor(newAnchor, newGeoiteDat);
                //newBA.geoite.PerformInflate();
            }
            else
            {
                Debug.LogWarning("Anchor not created successfully");
                UpdateFeedbackText($"Anchor not created successfully. Total anchors: {_anchors.Count}", error: true);
            }
            UpdateFeedbackText($"Created geoite. Total anchors: {_anchors.Count}", false);
        }

        
        void SetupGeoiteNearbyMonitoring()
        {
            if (_earthManager == null || _earthManager.EarthTrackingState != TrackingState.Tracking) return;

            Debug.Log("SetupGeoiteNearbyMonitoring");

            GeospatialPose geoPose = _earthManager.CameraGeospatialPose;

            double lat = geoPose.Latitude;
            double lng = geoPose.Longitude;

            this.SetupGeoiteNearbyMonitoring(lat, lng);
        }

     
        void SetupGeoiteNearbyMonitoring(double lat, double lng)
        {

            Mercator.MapTile userTile = Mercator.GetTileAtLatLng(lat, lng, GeoitesNetworking.MAP_TILE_MONITORING_ZOOM);
            List<Mercator.MapTile> userTiles = Mercator.GetSurroundingTileList(userTile, includeCenterTile: true);
            this.DeleteAllBallonAnchorsOutsideMapTiles(userTiles);

            this._nextMapTileCheckTime = Time.time + MAP_TILE_MONITORING_CHECK_INTERVAL;
            this._monitoringMapTile =
                _network.MonitorGeoitesNearby(lat, lng,
                (List<GeoiteChange> geoiteChangeList) => {
                    
                    if (this._monitoringMapTile == null) return;

                    // UpdateFeedbackText($"Received {geoiteChangeList.Count} geoite changes. Total anchors: {_anchors.Count}", false);
                    int changeNumber = 1;
                    geoiteChangeList.ForEach((GeoiteChange geoiteChange) => {
                        UpdateOrCreateGeoiteChangeFromServer(geoiteChange, changeNumber);
                        ++changeNumber;
                    });
                });
        }

       
        void StopGeoiteNearbyMonitoring()
        {
            this._monitoringMapTile = null;
            this._nextMapTileCheckTime = float.MaxValue;
            this._network.StopMonitoringGeoitesNearby();
            // this.DeleteAllBallonAnchors();
        }

       
        void UpdateMonitoringMapTile()
        {

            if (_earthManager == null ||
                _earthManager.EarthTrackingState != TrackingState.Tracking ||
                this._monitoringMapTile == null ||
                Time.time > this._nextMapTileCheckTime) return;
            // - - - - - - - - - -

            GeospatialPose geoPose = _earthManager.CameraGeospatialPose;
            double lat = geoPose.Latitude;
            double lng = geoPose.Longitude;

            Mercator.MapTile userTile = Mercator.GetTileAtLatLng(lat, lng, GeoitesNetworking.MAP_TILE_MONITORING_ZOOM);
            if (!userTile.Equals((Mercator.MapTile)this._monitoringMapTile))
            {
                // User is in a DIFFERENT MapTile. Restart monitoring!
                UpdateFeedbackText($"User moved to new MapTile. Restarting Monitoring!", error: true);

             
                this.StopGeoiteNearbyMonitoring();

                this.SetupGeoiteNearbyMonitoring(lat, lng);
            }
            else
            {
                this._nextMapTileCheckTime = Time.time + MAP_TILE_MONITORING_CHECK_INTERVAL;
            }
        }

                                   
        public void DeleteAllBallonAnchors()
        {
            for (int i = 0; i < _anchors.Count; i++)
            {
                Destroy(_anchors[i].anchor.gameObject);
            }
            _anchors.Clear();
            this.GeoiteAnchorCountChanged.Invoke(_anchors.Count);
        }

       
        /// <param name="mapTiles">A list of MapTiles to check if Geoites are inside of</param>
        public void DeleteAllBallonAnchorsOutsideMapTiles(List<Mercator.MapTile> mapTiles)
        {
            int geoitesDeleted = 0;
            for (int i = 0; i < _anchors.Count; i++)
            {
                GeoiteAnchor newBA = _anchors[i];
                // Get the MapTile the current geoite resides inside
                Mercator.MapTile geoiteMapTile = Mercator.GetTileAtLatLng(
                    newBA.geoite.Data.coordinate,
                    GeoitesNetworking.MAP_TILE_MONITORING_ZOOM);

                // Check if any MapTile from the mapTiles list is the same as the GeoiteMaptTile
                bool geoiteIsInsideAMapTile = false;
                for (int j = 0; j < mapTiles.Count; j++)
                {
                    if (!geoiteMapTile.Equals(mapTiles[j])) continue; // Keep lookin'
                    geoiteIsInsideAMapTile = true;
                    break;
                }

                // If the geoite is inside a map tile, move onto the next Geoite...
                if (geoiteIsInsideAMapTile) continue;
                // If the Geoite resides in none of the MapTiles provided, Delete it.
                Destroy(newBA.anchor.gameObject);
                _anchors.RemoveAt(i);
                // Causing the next for-loop iteration 
                // to have the use the same index as this iteration
                i = i - 1;
                ++geoitesDeleted;
            }
            if (geoitesDeleted > 0)
            {
                this.GeoiteAnchorCountChanged.Invoke(_anchors.Count);
            }
            UpdateFeedbackText($"Deleted {geoitesDeleted} geoites outside MapTiles", error: true);
        }

        [Obsolete]
        public void PlaceAnchorAtCurrentPosition()
        {
            this.PlaceAnchorAtCurrentPosition(distAheadOfCamera: 3.0);
        }

       
        /// <param name="distAheadOfCamera">The distance ahead of the camera to place a Geoite anchor (default: 3m)</param>

        [Obsolete]
        public void PlaceAnchorAtCurrentPosition(double distAheadOfCamera = 3.0)
        {
            if (_earthManager == null) return;

            TrackingState trackingState = _earthManager.EarthTrackingState;
            if (trackingState != TrackingState.Tracking)
            {
                UpdateFeedbackText(
                    "Failed to create anchor. EarthManager not tracking.", error: true);
                return;
            }



            GeospatialPose geoPose = _earthManager.CameraGeospatialPose;

            Mercator.GeoCoordinate geoCoord = new Mercator.GeoCoordinate(geoPose.Latitude, geoPose.Longitude, geoPose.Altitude);
            Mercator.GeoCoordinate geoCoordAhead = geoCoord.CalculateDerivedPosition(distAheadOfCamera, geoPose.Heading);

            distAheadOfCamera = geoCoord.GetDistanceTo(geoCoordAhead);

            GeoiteData newBDat = new GeoiteData();
            newBDat.object_code = object_code; // Unique ID for the prefab
            newBDat.user_id = _network.CurrentUserID;
            newBDat.latitude = geoCoordAhead.latitude;
            newBDat.longitude = geoCoordAhead.longitude;
            newBDat.altitude = geoPose.Altitude - Geoite.ESTIMATED_CAM_HEIGHT_FROM_FLOOR;
            newBDat.geoite_string_length = UnityEngine.Random.Range(0.9f, 1.5f);

            Quaternion anchorRot = Quaternion.AngleAxis(0, new Vector3(0.0f, 1.0f, 0.0f));


            ARGeospatialAnchor newAnchor = _anchorManager.AddAnchor(
                
                newBDat.latitude,
                newBDat.longitude,
                newBDat.altitude,
                anchorRot);

            if (newAnchor != null || Application.isEditor)
            {
                GeoiteAnchor newBA = CreateNewGeoiteAnchor(newAnchor, newBDat);
                
                _network.CreateGeoite(newBDat,
                (GeoiteData bDat) => {

                    if (bDat == null || bDat.geoite_id == null || bDat.geoite_id.Length < 1)
                    {
                        Debug.LogError("Firestore error when creating Geoite");
                        UpdateFeedbackText($"Firestore Geoite error! Local geoite was still created...",
                                        error: true);
                    }
                    else
                    {
                        UpdateFeedbackText($"NEW Geoite sent to server! Total geoites: {_anchors.Count}",
                                        error: false);
                    }
                });

             
            }
            else
            {
                UpdateFeedbackText("Failed to create anchor. Internal error.", error: true);
            }
        }

       
        /// <param name="arAnchor">The GoogleARCore.Anchor that should already be created</param>
        /// <param name="geoiteData">The GeoiteData to use for the new GeoiteAnchor</param>
        private GeoiteAnchor CreateNewGeoiteAnchor(ARGeospatialAnchor arAnchor, GeoiteData geoiteData)
        {
            GameObject prefabToUse = AnchorVisObjectPrefab; // fallback/default

            if (!string.IsNullOrEmpty(geoiteData.object_code) &&
                _GeoitePrefabDict.TryGetValue(geoiteData.object_code, out GameObject specificPrefab))
            {
                prefabToUse = specificPrefab;
            }

            GameObject geoiteGO = Instantiate(prefabToUse);
            Geoite geoite = geoiteGO.GetComponentInChildren<Geoite>();
            //geoite.geoiteWasPopped.AddListener(this.GeoiteWasPopped);
            geoite.SetGeoiteData(geoiteData);

          

            GeoiteAnchor newBA = new GeoiteAnchor(arAnchor, geoite);
            geoiteGO.SetActive(false);
            if (!Application.isEditor)
            {
                geoiteGO.transform.SetParent(newBA.anchor.transform, false);
            }
            geoiteGO.transform.localPosition = Vector3.zero;
            geoiteGO.transform.localScale = Vector3.one;
            geoiteGO.SetActive(true);
            this._anchors.Add(newBA);
            this.GeoiteAnchorCountChanged.Invoke(_anchors.Count);

           

            return newBA;
        }


        void UpdateBallonAnchorYPositionsAndFade(bool adjustFade)
        {
            if (_anchors.Count < 1) return;

            Vector3 camPosWorld = this.CameraUnity.transform.position;
            _anchors.ForEach((GeoiteAnchor ba) => {
                float xzDist = Vector2.Distance(
                    new Vector2(camPosWorld.x, camPosWorld.z),
                    new Vector2(ba.anchor.transform.position.x, ba.anchor.transform.position.z));

                ba.geoite.UpdateGeoiteCamYPosFadeAndDistToCamera(camPosWorld.y, xzDist, camPosWorld, adjustFade);
            });
        }

       

        public void GeoiteWasPopped(Geoite b)
        {

            _network.PopGeoite(b.Data,
            (GeoiteData bDat) => {
                Debug.Log("Geoite was popped network returned: ");
                Debug.Log(bDat);
                if (bDat == null)
                {
                    UpdateFeedbackText($"Geoite pop error", error: true);
                }
                else
                {
                    UpdateFeedbackText($"Geoite pop was sent to the server!", false);
                }
            });
        }

        [Obsolete]
        public void Update()
        {
            if (!_trackingIsActive) return;

            SessionState.text = "ARSession State: " + ARSession.state;
#if UNITY_IOS
           
#else
            // UpdateApplicationLifecycle();
            UpdateEarthStatusText();
            UpdateGeoiteMonitoringState();
            UpdateBallonAnchorYPositionsAndFade(DebugSettings.Shared.FadeFarGeoites);
            UpdateGeospatialAnchorStatus();
#endif // UNITY_IOS

            bool isTracking = _earthManager.EarthTrackingState == TrackingState.Tracking
                              && ARSession.state == ARSessionState.SessionTracking;
            bool lastStateWasTracking = _lastEarthTrackingState == TrackingState.Tracking;
            if (isTracking != lastStateWasTracking)
            {
                _lastEarthTrackingState = _earthManager.EarthTrackingState;
                EarthTrackingStateChanged.Invoke(_lastEarthTrackingState);
            }
        }

        private void UpdateGeoiteMonitoringState()
        {

            if (_earthManager != null &&
                _earthManager.EarthTrackingState == TrackingState.Tracking)
            {
                // Earth IS tracking...
                if (_network.FirebaseReady && !_network.GeoitesNearbyMonitoringActive)
                {
                    // Start monitoring for geoites!
                    this.SetupGeoiteNearbyMonitoring();
                    UpdateFeedbackText($"Started geoite monitoring. Total anchors: {_anchors.Count}", false);
                }
                else
                {
                    // No need to check for a new MapTile if we JUST started monitoring
                    UpdateMonitoringMapTile();
                }

            }
            else
            {
                // Earth is NOT tracking...
                if (_network.GeoitesNearbyMonitoringActive && !Application.isEditor)
                {
                    // ... but we are still monitoring for geoites. Let's stop.
                    this.StopGeoiteNearbyMonitoring();
                    UpdateFeedbackText($"Stopped geoite monitoring... :. Total anchors: {_anchors.Count}", error: true);
                }
            }
        }


        [Obsolete]
        private void UpdateEarthStatusText()
        {
            if (ARSession.state != ARSessionState.SessionTracking ||
                EarthStatusTextUI == null || _earthManager == null)
            {
                EarthStatusTextUI.text = "[ERROR] ARSession.state == " + ARSession.state;
                return;
            }

   

            FeatureSupported geospatialIsSupported = _earthManager.IsGeospatialModeSupported(
                ARCoreExtensions.ARCoreExtensionsConfig.GeospatialMode);
            if (geospatialIsSupported == FeatureSupported.Unknown)
            {
                EarthStatusTextUI.text = "[ERROR] Geospatial supported is unknown";
                return;
            }
            else if (geospatialIsSupported == FeatureSupported.Unsupported)
            {
                EarthStatusTextUI.text = string.Format(
                    "[ERROR] GeospatialMode {0} is unsupported on this device.",
                    ARCoreExtensions.ARCoreExtensionsConfig.GeospatialMode);
                return;
            }

            EarthState earthState = _earthManager.EarthState;
            if (earthState != EarthState.Enabled)
            {
                EarthStatusTextUI.text = "[ERROR] EarthState: " + earthState;
                return;
            }

            TrackingState trackingState = _earthManager.EarthTrackingState;
            if (trackingState != TrackingState.Tracking)
            {
                EarthStatusTextUI.text = "[ERROR] EarthTrackingState: " + trackingState;
                return;
            }

         

            switch (trackingState)
            {
                case TrackingState.Tracking:
                    GeospatialPose geoPose = _earthManager.CameraGeospatialPose;
                    EarthStatusTextUI.text = string.Format(
                        "Earth Tracking State:   - TRACKING -\n" +
                        "LAT/LNG: {0:0.00000}, {1:0.00000} (acc: {2:0.000})\n" +
                        "ALTITUDE: {3:0.0}m (acc: {4:0.0}m)\n" +
                        "HEADING:{5:0.0}º (acc: {6:0.0}º)",
                        geoPose.Latitude, geoPose.Longitude,
                        geoPose.HorizontalAccuracy,
                        geoPose.Altitude, geoPose.VerticalAccuracy,
                        geoPose.Heading, geoPose.HeadingAccuracy);
                    break;
                case TrackingState.Limited:
                    EarthStatusTextUI.text = "Earth Tracking State:   - LIMITED -";
                    break;
                case TrackingState.None:
                    EarthStatusTextUI.text = "Earth Tracking State:   - NONE -";
                    break;
            }
        }


        private void UpdateGeospatialAnchorStatus()
        {
            int total = _anchors.Count;
            int none = 0;
            int limited = 0;
            int tracking = 0;

            foreach (GeoiteAnchor ba in _anchors)
            {
                switch (ba.anchor.trackingState)
                {
                    case TrackingState.None:
                        none++;
                        break;
                    case TrackingState.Limited:
                        limited++;
                        break;
                    case TrackingState.Tracking:
                        tracking++;
                        break;
                }
            }

            EarthAnchorText.text = string.Format(
                "EarthAnchor: {1}{0}" +
                " None: {2}{0}" +
                " Limited: {3}{0}" +
                " Tracking: {4}",
                Environment.NewLine, total, none, limited, tracking);
        }

        
        /// <param name="message">Message string to show.</param>
        /// <param name="error">If true, signifies an error and colors the feedback text red,
       
        private void UpdateFeedbackText(string message, bool error)
        {
            if (error)
            {
                FeedbackTextUI.color = Color.red;
            }
            else
            {
                FeedbackTextUI.color = Color.green;
            }

            FeedbackTextUI.text = message;
        }

      
        public static Google.XR.ARCoreExtensions.ARCoreExtensions GetARCoreExtensions()
        {
            var extensionsGO = GameObject.Find("ARCore Extensions");
            return extensionsGO?.GetComponent<Google.XR.ARCoreExtensions.ARCoreExtensions>();
        }
                  
        public void DebugSettingsChanged(DebugSettings debugSettings)
        {
            // Debug.Log("DebugSettingsChanged");
            FeedbackTextUI.transform.parent.gameObject.SetActive(
                debugSettings.DisplayEarthDebug);
            SessionState.gameObject.SetActive(
                debugSettings.DisplayEarthDebug);
            EarthAnchorText.gameObject.SetActive(
                debugSettings.DisplayEarthDebug);
        }
    }
}