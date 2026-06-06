using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VortexGame.Input
{
    public sealed class AbilityButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private Image ringImage;

        public Action<bool> HoldChanged;
        public Action Clicked;

        public void Initialize(Image ring)
        {
            ringImage = ring;
        }

        public void SetCooldownFill(float value)
        {
            if (ringImage != null)
            {
                ringImage.fillAmount = 1f - Mathf.Clamp01(value);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            HoldChanged?.Invoke(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            HoldChanged?.Invoke(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke();
        }
    }
}

