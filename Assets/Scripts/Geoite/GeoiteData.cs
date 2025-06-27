namespace Ite.Rupp.Arunity
{
    using System;
    using System.Collections.Generic;
    using Firebase.Firestore;

    /// <summary>
    /// An interface for a class to confrom to if it wants update about data
    /// that would make visual updates to the Geoite
    /// </summary>
    public interface GeoiteDataVisualChangesListener
    {
        void VisualChangeGeoitePopped(DateTimeOffset oldPoppedUntil, DateTimeOffset poppedUntil, string poppedByUserID, string curUserID);
    }

    /// <summary>
    /// GeoiteData stores information about a Geoite
    /// </summary>
    [Serializable]
    [FirestoreData]
    public class GeoiteData : IEquatable<GeoiteData>
    {
        [FirestoreProperty] public string object_name { get; set; }
        [FirestoreProperty] public string object_code { get; set; }
        [FirestoreProperty] public string object_description { get; set; }
        [FirestoreProperty] public string image_url { get; set; }
        [FirestoreProperty] public string geoite_id { get; set; }
        [FirestoreProperty] public string user_id { get; set; }
        [FirestoreProperty] public double latitude { get; set; }
        [FirestoreProperty] public double longitude { get; set; }
        [FirestoreProperty] public double altitude { get; set; }
        [FirestoreProperty] public string tile_z12_xy_hash { get; set; }
        [FirestoreProperty] public string tile_z13_xy_hash { get; set; }
        [FirestoreProperty] public string tile_z14_xy_hash { get; set; }
        [FirestoreProperty] public string tile_z15_xy_hash { get; set; }
        [FirestoreProperty] public string tile_z16_xy_hash { get; set; }
        [FirestoreProperty] public string geohash { get; set; }
        [FirestoreProperty] public long created { get; set; }
        [FirestoreProperty] public float geoite_string_length { get; set; }
        [FirestoreProperty] public int num_popped { get; set; }
        [FirestoreProperty] public long popped_until { get; set; }
        [FirestoreProperty] public string last_user_pop { get; set; }
        [FirestoreProperty] public string color { get; set; } // hex

        /// <summary>
        /// The visual changes listener
        /// </summary>
        private GeoiteDataVisualChangesListener _VisualsListener = null;

        /// <summary>
        /// Set the listener for the visual changes
        /// </summary>
        /// <param name="listener">Set a new listener</param>
        public void SetVisualChangesListener(GeoiteDataVisualChangesListener listener)
        {
            _VisualsListener = listener;
        }

        // -------------------------
        // Convenience getters

        /// <summary>
        /// The date when this GeoiteData was created
        /// </summary>
        /// <returns>The created date</returns>
        public DateTimeOffset CreatedDate
        {
            get { return DateTimeOffset.FromUnixTimeMilliseconds(created); }
        }

        /// <summary>
        /// The date when this GeoiteData will inflate next, or will inflate
        /// </summary>
        /// <returns>The popped until date</returns>
        public DateTimeOffset PoppedUntilDate
        {
            get { return DateTimeOffset.FromUnixTimeMilliseconds(popped_until); }
        }

        /// <summary>
        /// Get the Color of this Geoite
        /// </summary>
        /// <returns>The Color</returns>
        public UnityEngine.Color ColorFromHex
        {
            get
            {
                UnityEngine.Color col = UnityEngine.Color.white;
                if (UnityEngine.ColorUtility.TryParseHtmlString("#09FF0064", out col)) return col;
                return UnityEngine.Color.white;
            }
        }

        /// <summary>
        /// Get the GeoCoordinate for this GeoiteData
        /// </summary>
        /// <returns>A Mercator.GeoCoordinate</returns>
        public Mercator.GeoCoordinate coordinate
        {
            get { return new Mercator.GeoCoordinate(latitude, longitude); }
        }

        // =====================================

        /// <summary>
        /// Default constructor
        /// </summary>
        public GeoiteData()
        {
            geoite_id = "";
            user_id = "";
            object_name = "";
            object_code = "";
            object_description = "";
            image_url = "";
            created = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// GeoiteData constructor
        /// </summary>
        /// <param name="geoiteDict">The Geoite data dictionary</param>
        public GeoiteData(Dictionary<string, object> geoiteDict)
        {
            UpdatePropertiesWithDict(geoiteDict);
        }

        // =====================================
        // IEquatable<GeoiteDatas>

        /// <summary>
        /// Get a HashCode for this GeoiteData
        /// </summary>
        /// <returns>A hash code</returns>
        public override int GetHashCode() => geoite_id.GetHashCode();

        /// <summary>
        /// Check if this GeoiteData is equal to another
        /// </summary>
        /// <param name="obj">The GeoiteData to compare to</param>
        /// <returns>True if the passed GeoiteData is deemed to be equal to this GeoiteData</returns>
        public override bool Equals(object obj) => this.Equals(obj as GeoiteData);

        /// <summary>
        /// Check if this GeoiteData is equal to another
        /// </summary>
        /// <param name="GeoiteDat">The GeoiteData to compare to</param>
        /// <returns>True if the passed GeoiteData is deemed to be equal to this GeoiteData</returns>
        public bool Equals(GeoiteData b)
        {
            if (b is null) return false;
            // If run-time types are not exactly the same, return false.
            if (this.GetType() != b.GetType()) return false;

            // Optimization for a common success case.
            if (System.Object.ReferenceEquals(this, b)) return true;

            if (b.geoite_id == null || b.geoite_id.Length < 1 ||
                this.geoite_id == null || this.geoite_id.Length < 1)
            {
                return false;
            }

            return b.geoite_id == this.geoite_id;
        }
        // -------------------------

        /// <summary>
        /// Should be called the the current user pops a Geoite
        /// </summary>
        /// <param name="newPoppedUntil">The datetime when the Geoite will re-inflate</param>
        /// <param name="newLastUserPopped">The user that popped this Geoite</param>
        /// <param name="newNumPopped">How many times this Geoite has been popped</param>
        public void CurrentUserPoppedGeoite(long newPoppedUntil, string newLastUserPopped, int? newNumPopped = null)
        {
            DateTimeOffset oldPoppedUntil = this.PoppedUntilDate;
            popped_until = newPoppedUntil;
            last_user_pop = newLastUserPopped;

            DateTimeOffset newPoppedUntilDate = this.PoppedUntilDate;
            if (newNumPopped != null) num_popped = (int)newNumPopped;

            _VisualsListener.VisualChangeGeoitePopped(oldPoppedUntil, newPoppedUntilDate, this.last_user_pop, newLastUserPopped);
        }

        /// <summary>
        /// Updates this GeoiteData with a GeoiteDict - should be called from the Geoite class
        /// </summary>
        /// <param name="dict">The Geoite data dictionary</param>
        /// <param name="curUserID">ID of the current user</param>
        public void UpdateWithDict(Dictionary<string, object> dict, string curUserID)
        {
            DateTimeOffset oldPoppedUntil = this.PoppedUntilDate;
            UpdatePropertiesWithDict(dict);

            if (popped_until < 1) return; // Geoite has never been popped
            DateTimeOffset newPoppedUntil = this.PoppedUntilDate;
            DateTimeOffset now = DateTimeOffset.Now;

            _VisualsListener.VisualChangeGeoitePopped(oldPoppedUntil, newPoppedUntil, this.last_user_pop, curUserID);
        }
        // -------------------------

        /// <summary>
        /// Update the properties on this GeoiteData using the data in the dictionary
        /// that was passed in
        /// </summary>
        /// <param name="dict">The Geoite data dictionary</param>
        void UpdatePropertiesWithDict(Dictionary<string, object> dict)
        {
            // PrintDict(dict);
            object val = null;
            if (dict.TryGetValue("object_name", out val)) this.object_name = (string)val;
            if (dict.TryGetValue("object_code", out val)) this.object_code = (string)val;
            if (dict.TryGetValue("object_description", out val)) this.object_description = (string)val;
            if (dict.TryGetValue("image_url", out val)) this.image_url = (string)val;
            if (dict.TryGetValue("geoite_id", out val)) this.geoite_id = (string)val;
            if (dict.TryGetValue("altitude", out val)) this.altitude = Convert.ToDouble(val);
            if (dict.TryGetValue("user_id", out val)) this.user_id = (string)val;
            if (dict.TryGetValue("last_user_pop", out val)) this.last_user_pop = (string)val;
            if (dict.TryGetValue("latitude", out val)) this.latitude = (double)val;
            if (dict.TryGetValue("longitude", out val)) this.longitude = (double)val;
            if (dict.TryGetValue("tile_z12_xy_hash", out val)) this.tile_z12_xy_hash = (string)val;
            if (dict.TryGetValue("tile_z13_xy_hash", out val)) this.tile_z13_xy_hash = (string)val;
            if (dict.TryGetValue("tile_z14_xy_hash", out val)) this.tile_z14_xy_hash = (string)val;
            if (dict.TryGetValue("tile_z15_xy_hash", out val)) this.tile_z15_xy_hash = (string)val;
            if (dict.TryGetValue("tile_z16_xy_hash", out val)) this.tile_z16_xy_hash = (string)val;
            if (dict.TryGetValue("geohash", out val)) this.geohash = (string)val;
            if (dict.TryGetValue("created", out val)) this.created = (long)val;
            if (dict.TryGetValue("geoite_string_length", out val)) this.geoite_string_length = (float)Convert.ToDouble(val);
            if (dict.TryGetValue("num_popped", out val)) this.num_popped = Convert.ToInt32(val);
            if (dict.TryGetValue("popped_until", out val)) this.popped_until = Convert.ToInt64(val);
            if (dict.TryGetValue("color", out val)) this.color = (string)val;
        }

        /// <summary>
        /// Print the contents of a dictionary
        /// </summary>
        /// <param name="geoiteDict">The Geoite data dictionary</param>
        void PrintDict(Dictionary<string, object> geoiteDict)
        {
            foreach (KeyValuePair<string, object> kvp in geoiteDict)
            {
                UnityEngine.Debug.Log($"Key = {kvp.Key}, Value = {kvp.Value}");
            }
        }

        /// <summary>
        /// A description of the contents of this class
        /// </summary>
        /// <returns>A description string</returns>
        public override string ToString()
        {
            return $"geoite_id: {geoite_id}, user_id: {user_id}, last_user_pop: {last_user_pop}, " +
                   $"latitude: {latitude}, longitude: {longitude}, altitude: {altitude}, " +
                   $"tile_z16_xy_hash: {tile_z16_xy_hash}, geohash: {geohash}, created: {created}, " +
                   $"num_popped: {num_popped}, popped_until: {popped_until}, color: {color}, " +
                   $"object_code: {object_code}, " + $"object_description: {object_description}, " + $"object_name: {object_name}," + $"image_url: {image_url}, " ;
        }
    }

}