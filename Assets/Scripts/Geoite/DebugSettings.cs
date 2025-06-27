namespace Ite.Rupp.Arunity
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;

    [Serializable] public class DebugChangedEvent : UnityEvent <DebugSettings> { }

    [ExecuteInEditMode]
    public class DebugSettings : MonoBehaviour
    {
        // ------------------------------------
        public static DebugSettings Shared { get; private set; }
        // ------------------------------------

        [SerializeField] private DebugChangedEvent _debugChangedEvent;

        [SerializeField] private bool _allDebugOff = false;
        [SerializeField] private bool _displayEarthDebug = true;
        [SerializeField] private bool _displayGeoiteDebug = false;
        [SerializeField] private bool _fadeFarGeoites = false;

        public bool DisplayEarthDebug { get 
            { return !_allDebugOff && _displayEarthDebug; } 
        }
        public bool DisplayGeoiteDebug { get 
            { return !_allDebugOff && _displayGeoiteDebug; }
        }
        public bool FadeFarGeoites { get 
            { return !_allDebugOff && _fadeFarGeoites; }
        }

        // ------------------------------------

        void Awake() {
            if (Shared != null && Shared != this) 
            { 
                Destroy(this); 
            } 
            else 
            { 
                Shared = this; 
            } 
        }

        void OnValidate()
        {
            // if (!Application.isPlaying) {
            //     Debug.LogWarning("DEBUG Changes won't be made unless the app is running");
            // }
            _debugChangedEvent.Invoke(this);
        }
    }
}