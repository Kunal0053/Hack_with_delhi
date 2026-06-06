using UnityEngine;

namespace VortexGame.Core
{
    public sealed class CameraFollow : MonoBehaviour
    {
        public Transform Target;
        public Vector3 Offset = new Vector3(0f, 18f, -3f);

        private void LateUpdate()
        {
            if (Target == null)
            {
                return;
            }

            Vector3 desired = Target.position + Offset;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-4f * Time.deltaTime));
            transform.LookAt(Target.position + Vector3.up * 0.5f);
        }
    }
}
