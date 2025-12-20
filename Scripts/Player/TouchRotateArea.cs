using UnityEngine;
using UnityEngine.EventSystems;

public class TouchRotateArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{

    [HideInInspector] public Vector2 TouchPosition;
    [HideInInspector] public Vector2 LastTouchPosition;
    [HideInInspector] public int TouchID;
    [HideInInspector] public bool IsTouching;

    public float TouchSensivity = 0.5f;

    
    void Update()
    {
        if (!IsTouching)
        {
            TouchPosition = Vector2.zero;
        }
    }

    // Touch start.
    public void OnPointerDown(PointerEventData eventData)
    {
        IsTouching = true;
        TouchID = eventData.pointerId;
        LastTouchPosition = eventData.position;
    }

    // Touch end
    public void OnPointerUp(PointerEventData eventData)
    {
        IsTouching = false;
        TouchPosition = Vector2.zero;
    }

    // Touch dragging
    public void OnDrag(PointerEventData eventData)
    {
        // If the touch comes from the same finger.
        if (eventData.pointerId == TouchID)
        {
            // Determines the movement direction using the offset between two positions.
            Vector2 CurrentPosition = eventData.position;
            TouchPosition = (CurrentPosition - LastTouchPosition) * TouchSensivity;
            LastTouchPosition = CurrentPosition;
        }
    }

    






}
