using UnityEngine;

namespace VortexGame.Core
{
    public sealed class CaptureZone : MonoBehaviour
    {
        private Material materialInstance;
        private float controlValue;

        public VortexEntity Owner { get; private set; }
        public float Radius { get; private set; }

        public void Initialize(float radius)
        {
            Radius = radius;
            transform.localScale = new Vector3(radius * 2f, 0.1f, radius * 2f);
            materialInstance = GetComponent<Renderer>().material;
            SetVisual(Color.gray);
        }

        public void Tick(float deltaTime, VortexGameManager manager)
        {
            VortexEntity strongest = null;
            float strongestPressure = 0f;

            for (int i = 0; i < manager.Vortexes.Count; i++)
            {
                VortexEntity vortex = manager.Vortexes[i];
                float distance = Vector3.Distance(vortex.transform.position, transform.position);
                if (distance > Radius)
                {
                    continue;
                }

                float pressure = vortex.Energy * Mathf.Clamp01(1f - (distance / Radius));
                if (pressure > strongestPressure)
                {
                    strongestPressure = pressure;
                    strongest = vortex;
                }
            }

            if (strongest == null)
            {
                controlValue = Mathf.MoveTowards(controlValue, 0f, deltaTime * 0.35f);
                if (controlValue <= 0.01f)
                {
                    Owner = null;
                    SetVisual(Color.gray);
                }

                return;
            }

            if (Owner == strongest)
            {
                controlValue = Mathf.MoveTowards(controlValue, 1f, deltaTime * 0.45f);
            }
            else
            {
                controlValue -= deltaTime * 0.5f;
                if (controlValue <= 0f)
                {
                    Owner = strongest;
                    controlValue = 0.1f;
                }
            }

            if (Owner != null)
            {
                Owner.AddZoneScore(deltaTime * controlValue);
                SetVisual(Color.Lerp(Color.gray, Owner.ThemeColor, controlValue));
            }
        }

        private void SetVisual(Color color)
        {
            materialInstance.color = color;
        }
    }
}

