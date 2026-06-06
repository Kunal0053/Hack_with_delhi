using UnityEngine;
using UnityEngine.EventSystems;

namespace VortexGame.Input
{
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform handle;
        [SerializeField] private RectTransform area;
        [SerializeField] private float radius = 90f;

        public Vector2 Value { get; private set; }

        public void Initialize(RectTransform dragArea, RectTransform handleRect, float movementRadius)
        {
            area = dragArea;
            handle = handleRect;
            radius = movementRadius;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                return;
            }

            Value = Vector2.ClampMagnitude(localPoint / radius, 1f);
            handle.anchoredPosition = Value * radius;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Value = Vector2.zero;
            handle.anchoredPosition = Vector2.zero;
        }
    }
}

