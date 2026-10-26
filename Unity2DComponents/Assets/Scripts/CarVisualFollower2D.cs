using UnityEngine;

namespace ModularComponents.Driving2D
{
    /// <summary>Follows a car's physics body from a separate visual hierarchy.</summary>
    public sealed class CarVisualFollower2D : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D sourceBody;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float visualRotationOffset;

        private void Reset()
        {
            visualRoot = transform;
        }

        private void Awake()
        {
            if (visualRoot == null)
                visualRoot = transform;
        }

        private void LateUpdate()
        {
            if (sourceBody == null || visualRoot == null)
                return;

            // The visual is a sibling of the rotating Rigidbody2D, so it never inherits that rotation.
            visualRoot.position = sourceBody.transform.position;
            visualRoot.rotation = sourceBody.transform.rotation * Quaternion.Euler(0f, 0f, visualRotationOffset);
        }
    }
}
