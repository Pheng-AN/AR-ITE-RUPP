namespace Ite.Rupp.Arunity
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using UnityEngine;

    using Firebase;
    using Firebase.Firestore;
    using Firebase.Auth;
    // ----------------------------

    public struct GeoiteChange 
    {
        public Dictionary<string, object> Dict;
        public Firebase.Firestore.DocumentChange.Type ChangeType;
    }

    public class GeoitesNetworking
    {
        static public int MAP_TILE_MONITORING_ZOOM = 14;
        static public double GEOITE_POP_COOLDOWN_SECS = 2.7;
        
        private bool _firebaseReady = false;
        private ListenerRegistration _geoitesNearbyListener = null;
        public Action<bool> FirebaseReadyChangedEvent;

        public bool FirebaseReady { get { return _firebaseReady; } }
        public bool GeoitesNearbyMonitoringActive { get { return _geoitesNearbyListener != null; } }
        // public bool GeoitesNearbyMonitoringActive { get { return false; } }

        private FirebaseApp _firebaseApp;
        private FirebaseFirestore _firebaseDB;
        private FirebaseAuth _firebaseAuth;

        private string _currentUserID = "";
        public string CurrentUserID { get { return _currentUserID; } }

        public GeoitesNetworking() 
        {
            InitFirebase();
        }
        
        /// <summary>
        /// https://firebase.google.com/docs/unity/setup?hl=en&authuser=0#confirm-google-play-version
        /// Initialise Firebase
        /// </summary>
        private void InitFirebase() 
        {
            Debug.Log("InitFirebase");
            Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
                var dependencyStatus = task.Result;
                if (dependencyStatus == Firebase.DependencyStatus.Available)
                {
                    // Create and hold a reference to your FirebaseApp,
                    // where app is a Firebase.FirebaseApp property of your application class.
                    _firebaseApp = Firebase.FirebaseApp.DefaultInstance;
                    _firebaseDB = FirebaseFirestore.DefaultInstance;
                    _firebaseAuth = FirebaseAuth.DefaultInstance;
                    // _FirebaseFunctions = FirebaseFunctions.DefaultInstance;

                    // Set a flag here to indicate whether Firebase is ready to use by your app.
                    this._firebaseReady = true;
                    CreateOrLoginToAnonymousUser();

                } else {
                    UnityEngine.Debug.LogError(System.String.Format(
                    "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                    // Firebase Unity SDK is not safe to use here.
                }
            });
        }

        // ============================================================
        // ------------------------------------------------------------
        // ============================================================

        //     #    #     # ####### #     #    #       #######  #####  ### #     # 
        //    # #   ##    # #     # ##    #    #       #     # #     #  #  ##    # 
        //   #   #  # #   # #     # # #   #    #       #     # #        #  # #   # 
        //  #     # #  #  # #     # #  #  #    #       #     # #  ####  #  #  #  # 
        //  ####### #   # # #     # #   # #    #       #     # #     #  #  #   # # 
        //  #     # #    ## #     # #    ##    #       #     # #     #  #  #    ## 
        //  #     # #     # ####### #     #    ####### #######  #####  ### #     # 
                                                                        
        /// <summary>
        /// Anonymously login to Firebase to get a UserID.
        /// A simple method to associate Geoites with a user without user registration.
        /// </summary>
        private void CreateOrLoginToAnonymousUser() 
        {
            Debug.Log("CreateOrLoginToAnonymousUser");
            _firebaseAuth.SignInAnonymouslyAsync().ContinueWith(task => {
                if (task.IsCanceled) 
                {
                    Debug.LogError("SignInAnonymouslyAsync was canceled.");
                    return;
                }
                if (task.IsFaulted) 
                {
                    Debug.LogError("SignInAnonymouslyAsync encountered an error: " + task.Exception);
                    return;
                }

                Firebase.Auth.FirebaseUser newUser = task.Result;
                Debug.LogFormat("User signed in successfully: {0} ({1})",
                    newUser.DisplayName, newUser.UserId);

                _currentUserID = newUser.UserId;
                // PlayerPrefs.SetString(FIREBASE_USERID_KEY, newUser.UserId);
                // PlayerPrefs.Save();

                Debug.Log("FirebaseReady!");
                this.FirebaseReadyChangedEvent.Invoke(this._firebaseReady);
            });
        }

        // ============================================================
        // ------------------------------------------------------------
        // ============================================================

        /// <summary>
        /// Create a Geoite on the global CloudBallons server
        /// </summary>
        /// <param name="callback">Callback to return the response result. Passes null if there was an error.</param>
        public void CreateGeoite(GeoiteData geoiteData,
                                  Action<GeoiteData> callback)
        {
                                    
            CreateGeoiteOnFirestore(geoiteData, callback);
        }

        /// <summary>
        /// Pop a Geoite
        /// </summary>
        /// <param name="geoiteDat">The Geoite data</param>
        /// <param name="callback">Callback to return the response result.</param>
        public void PopGeoite(GeoiteData geoiteDat, Action<GeoiteData> callback)
        {
            PopGeoiteOnFirestore(geoiteDat, callback);
        }

        // ============================================================
        // ------------------------------------------------------------
        // ============================================================

        //     #    ######  ######     ######     #    #       #       ####### ####### #     # 
        //    # #   #     # #     #    #     #   # #   #       #       #     # #     # ##    # 
        //   #   #  #     # #     #    #     #  #   #  #       #       #     # #     # # #   # 
        //  #     # #     # #     #    ######  #     # #       #       #     # #     # #  #  # 
        //  ####### #     # #     #    #     # ####### #       #       #     # #     # #   # # 
        //  #     # #     # #     #    #     # #     # #       #       #     # #     # #    ## 
        //  #     # ######  ######     ######  #     # ####### ####### ####### ####### #     # 
                                                                                    
        /// <summary>
        /// When the user creates a Geoite, create it on Firestore database
        ///     user_id- should already be set
        ///     latitude- should already be set
        ///     longitude- should already be set
        ///     altitude- should already be set
        ///     Geoite_string_length- should already be set
        /// </summary>
        /// <param name="geoiteDat">The Geoite data</param>
        /// <param name="callback">Callback to return the response result.</param>
        private void CreateGeoiteOnFirestore(
            GeoiteData geoiteData,
            Action<GeoiteData> callback)
            {

            CollectionReference geoitesRef = _firebaseDB.Collection("geoites");

            geoiteData.tile_z12_xy_hash = Mercator.GetTileAtLatLng(geoiteData.latitude, geoiteData.longitude, 12).XYHash;
            geoiteData.tile_z13_xy_hash = Mercator.GetTileAtLatLng(geoiteData.latitude, geoiteData.longitude, 13).XYHash;
            geoiteData.tile_z14_xy_hash = Mercator.GetTileAtLatLng(geoiteData.latitude, geoiteData.longitude, 14).XYHash;
            geoiteData.tile_z15_xy_hash = Mercator.GetTileAtLatLng(geoiteData.latitude, geoiteData.longitude, 15).XYHash;
            geoiteData.tile_z16_xy_hash = Mercator.GetTileAtLatLng(geoiteData.latitude, geoiteData.longitude, 16).XYHash;

            // geohash is unused
            geoiteData.created = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            geoiteData.num_popped = 0;
            geoiteData.popped_until = 0;
            geoiteData.color = "#ffffff";

            geoitesRef.AddAsync(geoiteData).ContinueWith(task => {
                if (task != null && task.Result != null && task.Result.Id != null)
                {
                    geoiteData.geoite_id = task.Result.Id;
                }
                callback(geoiteData);
            });
        }

        // ============================================================
        // ------------------------------------------------------------
        // ============================================================

        //  ######  ####### ######     ######     #    #       #       ####### ####### #     # 
        //  #     # #     # #     #    #     #   # #   #       #       #     # #     # ##    # 
        //  #     # #     # #     #    #     #  #   #  #       #       #     # #     # # #   # 
        //  ######  #     # ######     ######  #     # #       #       #     # #     # #  #  # 
        //  #       #     # #          #     # ####### #       #       #     # #     # #   # # 
        //  #       #     # #          #     # #     # #       #       #     # #     # #    ## 
        //  #       ####### #          ######  #     # ####### ####### ####### ####### #     # 


        /// <summary>
        /// When the user pops a Geoite, update the number of pops and the popped_until date
        /// </summary>
        /// <param name="geoiteDat">The Geoite data</param>
        /// <param name="callback">Callback to return the response result.</param>
        private void PopGeoiteOnFirestore(
            GeoiteData geoiteData,
            Action<GeoiteData> callback)
            {

            string curUserID = this._currentUserID;
            int newNumPopped = geoiteData.num_popped + 1;
            long newPoppedUntil = DateTimeOffset.Now
                .AddSeconds(GEOITE_POP_COOLDOWN_SECS)
                .ToUnixTimeMilliseconds();

            if (geoiteData == null || geoiteData.geoite_id == null || geoiteData.geoite_id.Length < 1)
            {
                geoiteData.CurrentUserPoppedGeoite(newPoppedUntil, curUserID);
                callback(null);
                Debug.LogError("Cannot pop a null geoiteData or geoite_id");
                return;
            }

            DocumentReference geoiteRef = _firebaseDB.Collection("geoites").Document(geoiteData.geoite_id);
            if (geoiteRef == null)
            {
                geoiteData.CurrentUserPoppedGeoite(newPoppedUntil, curUserID);                
                callback(null);
                Debug.LogError("Geoite ref is null");
                return;
            }

            _firebaseDB.RunTransactionAsync(transaction =>
            {
                return transaction.GetSnapshotAsync(geoiteRef)
                .ContinueWith((Task<DocumentSnapshot> snapshotTask) =>
                {
                    DocumentSnapshot snapshot = snapshotTask.Result;
                    newNumPopped = snapshot.GetValue<int>("num_popped") + 1;
                    newPoppedUntil = DateTimeOffset.Now
                        .AddSeconds(GEOITE_POP_COOLDOWN_SECS)
                        .ToUnixTimeMilliseconds();
                    Dictionary<string, object> updates = new Dictionary<string, object>
                    {
                        { "num_popped", newNumPopped },
                        { "popped_until", newPoppedUntil },
                        { "last_user_pop", curUserID }
                    };
                    transaction.Update(geoiteRef, updates);
                });
            }).ContinueWith((Task transactionResultTask) =>
            {

                if (transactionResultTask.IsCompleted && 
                    !transactionResultTask.IsCanceled && 
                    !transactionResultTask.IsFaulted)
                {
                    Debug.Log("Geoite popped");
                    geoiteData.CurrentUserPoppedGeoite(newPoppedUntil, curUserID, newNumPopped);
                    callback(geoiteData);
                }
                else
                {
                    // Set popped_until locally regardless
                    geoiteData.CurrentUserPoppedGeoite(newPoppedUntil, curUserID);
                    Debug.Log("Geoite pop error");
                    callback(null);
                } 
            });
        }                                          

        // ============================================================
        // ------------------------------------------------------------
        // ============================================================

        //  ######  #######    #    #       ####### ### #     # ####### 
        //  #     # #         # #   #          #     #  ##   ## #       
        //  #     # #        #   #  #          #     #  # # # # #       
        //  ######  #####   #     # #          #     #  #  #  # #####   
        //  #   #   #       ####### #          #     #  #     # #       
        //  #    #  #       #     # #          #     #  #     # #       
        //  #     # ####### #     # #######    #    ### #     # ####### 
                                                             

        /// <summary>
        /// Stop listening for changes to the database and nullify the reference to the listener
        /// </summary>
        public void StopMonitoringGeoitesNearby()
        {
            if (_geoitesNearbyListener == null) return;
            _geoitesNearbyListener.Stop();
            _geoitesNearbyListener = null;
        }

        /// <summary>
        /// Get a Firestore query for 9 map tiles corresponding to the user map tile
        /// </summary>
        Query GetGeoitesAroundUserQuery(Mercator.MapTile userMapTile)
        {
            List<string> tileHashList = Mercator.GetSurroundingTileXYHashList(userMapTile, includeCenterTile: true);
            
            CollectionReference geoitesRef = _firebaseDB.Collection("geoites");

            // https://firebase.google.com/docs/firestore/query-data/queries#in_not-in_and_array-contains-any
            // Use the in operator to combine up to 10 equality (==) clauses on the same field with a logical OR. 
            // An in query returns documents where the given field matches any of the comparison values.
            string prop = $"tile_z{MAP_TILE_MONITORING_ZOOM}_xy_hash";
                Debug.Log($"Querying for tile field: {prop}");
                foreach (var hash in tileHashList)
                    Debug.Log($"Querying tile hash: {hash}");

            return geoitesRef.WhereIn(prop, tileHashList);
        }

        /// <summary>
        /// Get realtime updates about Geoites nearby
        /// </summary>
        /// <param name="userLatitude">Callback to return the response result.</param>
        /// <param name="userLongitude">Callback to return the response result.</param>
        /// <param name="geoitesNearbyCallback">Callback to return the response result.</param>
        public Mercator.MapTile MonitorGeoitesNearby(double userLatitude, double userLongitude, 
                                                Action<List<GeoiteChange>> geoiteChangesCallback) 
        {
            StopMonitoringGeoitesNearby();

            // // string uID = _CurrentUserID;
            Mercator.MapTile userMapTile = Mercator.GetTileAtLatLng(userLatitude, userLongitude, MAP_TILE_MONITORING_ZOOM);
            Query query = this.GetGeoitesAroundUserQuery(userMapTile);

            _geoitesNearbyListener = query.Listen(snapshot => {
                // Debug.Log("Geoite change callback");
                List<GeoiteChange> geoiteDicts = new List<GeoiteChange>();

                var changes = snapshot.GetChanges();
                foreach (DocumentChange documentChange in changes)
                {
                    string geoiteID = documentChange.Document.Id;
                    Dictionary<string, object> geoiteDict = documentChange.Document.ToDictionary();
                    geoiteDict["geoite_id"] = geoiteID;
                    
                    GeoiteChange geoiteChange = new GeoiteChange();
                    geoiteChange.Dict = geoiteDict;
                    geoiteChange.ChangeType = documentChange.ChangeType;
                    geoiteDicts.Add(geoiteChange);
                    // Debug.Log($"GeoiteID was CHANGED: {GeoiteID} changeType: {documentChange.ChangeType.ToString()}");
                }
                geoiteChangesCallback(geoiteDicts);
            });

            return userMapTile;
        }

        // ============================================================
        // ------------------------------------------------------------
        // ============================================================

        //  ######  ####### #       ####### ####### #######    ######     #    #       #       ####### ####### #     #  #####  
        //  #     # #       #       #          #    #          #     #   # #   #       #       #     # #     # ##    # #     # 
        //  #     # #       #       #          #    #          #     #  #   #  #       #       #     # #     # # #   # #       
        //  #     # #####   #       #####      #    #####      ######  #     # #       #       #     # #     # #  #  #  #####  
        //  #     # #       #       #          #    #          #     # ####### #       #       #     # #     # #   # #       # 
        //  #     # #       #       #          #    #          #     # #     # #       #       #     # #     # #    ## #     # 
        //  ######  ####### ####### #######    #    #######    ######  #     # ####### ####### ####### ####### #     #  #####                                                                  

        /// <summary>
        /// Used for debugging- delete all the Geoites in the 9 MapTiles that the user inhabits.
        /// A good modification would be to only delete Geoites surrounding the user that they have created.
        /// </summary>
        /// <param name="userLatitude">Callback to return the response result.</param>
        public void DeleteGeoitesAroundUser(double userLatitude, double userLongitude, Action<string> completeCallback)
        {
            Debug.Log($"DeleteGeoitesAroundUser... at {userLatitude}, {userLongitude}");

            Mercator.MapTile userMapTile = Mercator.GetTileAtLatLng(userLatitude, userLongitude, MAP_TILE_MONITORING_ZOOM);
            Query query = this.GetGeoitesAroundUserQuery(userMapTile);

            query.GetSnapshotAsync().ContinueWith(task => {
                QuerySnapshot querySnapshot = task.Result;

                WriteBatch batch = _firebaseDB.StartBatch();
                Debug.Log("Deleting " + querySnapshot.Count + " geoites...");
                foreach (DocumentSnapshot documentSnapshot in querySnapshot.Documents)
                {
                    batch.Delete(documentSnapshot.Reference);
                };

                batch.CommitAsync()
                .ContinueWith(localTask => {
                    bool err = localTask.IsCanceled || localTask.IsFaulted || !localTask.IsCompleted;
                    completeCallback(err ? "Error" : null);
                });
            });   
        }
    }
}
