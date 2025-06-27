namespace Ite.Rupp.Arunity
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;

    [Serializable] public class TouchEvent : UnityEvent <Vector2> { }

    public class SlingshotTouchResponder : MonoBehaviour, 
        IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public TouchEvent touchDown;
        public TouchEvent touchMoved;
        public TouchEvent touchEnded;

        public void OnPointerDown(PointerEventData eventData)
        {
            // Debug.Log("Mouse Down: " + eventData.pointerCurrentRaycast.gameObject.name);
            touchDown.Invoke(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Debug.Log("Dragging");
            touchMoved.Invoke(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Debug.Log("Mouse Up");
            touchEnded.Invoke(eventData.position);
        }
    
    }
}