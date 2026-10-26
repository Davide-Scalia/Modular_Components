using System.Collections.Generic;
using UnityEngine;

namespace ModularComponents.Driving2D
{
    /// <summary>Arcade top-down driving motor and impact response. It does not read player input.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CarController2D : MonoBehaviour
    {
        [Header("Driving")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField, Min(0f)] private float forwardAcceleration = 18f;
        [SerializeField, Min(0f)] private float reverseAcceleration = 10f;
        [SerializeField, Min(0f)] private float brakeDeceleration = 28f;
        [SerializeField, Min(0f)] private float coastDeceleration = 2f;
        [SerializeField, Min(0f)] private float maxForwardSpeed = 14f;
        [SerializeField, Min(0f)] private float maxReverseSpeed = 6f;

        [Header("Steering and Drift")]
        [SerializeField, Min(0f)] private float steeringRate = 150f;
        [SerializeField, Min(0.01f)] private float speedForFullSteering = 12f;
        [SerializeField, Range(0f, 1f)] private float steeringAtLowSpeed = 0.1f;
        [SerializeField, Min(0f), Tooltip("Higher values remove sideways velocity more quickly. Zero allows full drift.")]
        private float lateralGrip = 4f;

        [Header("Impact Response")]
        [SerializeField, Range(0f, 1f), Tooltip("Fraction of inward impact speed sent back out along the collision normal.")]
        private float bounceRestitution = 0.7f;
        [SerializeField, Range(0f, 1f), Tooltip("Fraction of total velocity retained after a bounce.")]
        private float impactSpeedRetention = 0.72f;
        [SerializeField, Min(0f)] private float minimumImpactSpeed = 0.25f;

        private readonly List<ContactNormal> activeContactNormals = new List<ContactNormal>();
        private readonly ContactPoint2D[] contactBuffer = new ContactPoint2D[8];
        private CarControlInput2D input;
        private Vector2 velocityBeforePhysics;

        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;
        public Rigidbody2D Body => body;

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.gravityScale = 0f;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }

        private void Awake()
        {
            if (body == null)
                body = GetComponent<Rigidbody2D>();

            if (body == null)
            {
                Debug.LogError("CarController2D requires a Rigidbody2D on the physics body.", this);
                enabled = false;
            }
        }

        public void SetInput(CarControlInput2D value)
        {
            input = new CarControlInput2D(value.Steering, value.Throttle);
        }

        private void FixedUpdate()
        {
            if (body == null)
                return;

            float deltaTime = Time.fixedDeltaTime;
            Vector2 forward = transform.up;
            Vector2 right = transform.right;
            Vector2 velocity = body.linearVelocity;
            float forwardSpeed = Vector2.Dot(velocity, forward);
            float lateralSpeed = Vector2.Dot(velocity, right);

            float nextForwardSpeed = UpdateForwardSpeed(forwardSpeed, deltaTime);
            float nextLateralSpeed = lateralSpeed * Mathf.Exp(-lateralGrip * deltaTime);
            velocity = forward * nextForwardSpeed + right * nextLateralSpeed;

            // Remove only inward velocity while touching an obstacle; outward bounce velocity is preserved.
            foreach (ContactNormal contact in activeContactNormals)
            {
                float intoSurface = Vector2.Dot(velocity, contact.Normal);
                if (intoSurface < 0f)
                    velocity -= contact.Normal * intoSurface;
            }

            body.linearVelocity = velocity;

            float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / speedForFullSteering);
            float steeringFactor = Mathf.Lerp(steeringAtLowSpeed, 1f, speedFactor);
            float travelDirection = forwardSpeed < -0.1f ? -1f : 1f;
            float rotationDelta = -input.Steering * steeringRate * steeringFactor * travelDirection * deltaTime;
            body.MoveRotation(body.rotation + rotationDelta);

            // Captured before the physics step so collision callbacks can calculate the incoming impact.
            velocityBeforePhysics = body.linearVelocity;
        }

        private float UpdateForwardSpeed(float speed, float deltaTime)
        {
            float throttle = input.Throttle;
            if (Mathf.Abs(throttle) < 0.01f)
                speed = Mathf.MoveTowards(speed, 0f, coastDeceleration * deltaTime);
            else if (speed * throttle < 0f)
                speed = Mathf.MoveTowards(speed, 0f, brakeDeceleration * Mathf.Abs(throttle) * deltaTime);
            else
                speed += throttle * (throttle > 0f ? forwardAcceleration : reverseAcceleration) * deltaTime;

            return Mathf.Clamp(speed, -maxReverseSpeed, maxForwardSpeed);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            RefreshContactNormals(collision);
            ApplyImpactBounce(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
            => RefreshContactNormals(collision);

        private void OnCollisionExit2D(Collision2D collision)
            => RemoveContactNormals(collision.gameObject);

        private void RefreshContactNormals(Collision2D collision)
        {
            RemoveContactNormals(collision.gameObject);
            int contactCount = collision.GetContacts(contactBuffer);
            for (int i = 0; i < contactCount; i++)
            {
                ContactPoint2D point = contactBuffer[i];
                Vector2 normal = point.normal;
                Vector2 towardCar = body.worldCenterOfMass - point.point;
                if (Vector2.Dot(normal, towardCar) < 0f)
                    normal = -normal;
                if (normal.sqrMagnitude > 0.0001f)
                    activeContactNormals.Add(new ContactNormal(collision.gameObject, normal.normalized));
            }
        }

        private void ApplyImpactBounce(Collision2D collision)
        {
            Rigidbody2D otherBody = collision.gameObject.GetComponentInParent<Rigidbody2D>();
            Vector2 obstacleVelocity = otherBody != null ? otherBody.linearVelocity : Vector2.zero;
            Vector2 incomingVelocity = velocityBeforePhysics - obstacleVelocity;
            float strongestClosingSpeed = 0f;
            Vector2 impactNormal = Vector2.zero;

            int contactCount = collision.GetContacts(contactBuffer);
            for (int i = 0; i < contactCount; i++)
            {
                ContactPoint2D point = contactBuffer[i];
                Vector2 normal = point.normal;
                if (Vector2.Dot(normal, body.worldCenterOfMass - point.point) < 0f)
                    normal = -normal;
                if (normal.sqrMagnitude < 0.0001f)
                    continue;

                normal.Normalize();
                float closingSpeed = Vector2.Dot(incomingVelocity, normal);
                if (closingSpeed < strongestClosingSpeed)
                {
                    strongestClosingSpeed = closingSpeed;
                    impactNormal = normal;
                }
            }

            if (impactNormal == Vector2.zero || strongestClosingSpeed >= -minimumImpactSpeed)
                return;

            Vector2 tangentVelocity = incomingVelocity - impactNormal * strongestClosingSpeed;
            Vector2 bouncedRelativeVelocity = tangentVelocity - impactNormal * (strongestClosingSpeed * bounceRestitution);
            body.linearVelocity = obstacleVelocity + bouncedRelativeVelocity * impactSpeedRetention;
        }

        private void RemoveContactNormals(GameObject source)
        {
            for (int i = activeContactNormals.Count - 1; i >= 0; i--)
                if (activeContactNormals[i].Source == source)
                    activeContactNormals.RemoveAt(i);
        }

        private readonly struct ContactNormal
        {
            public readonly GameObject Source;
            public readonly Vector2 Normal;

            public ContactNormal(GameObject source, Vector2 normal)
            {
                Source = source;
                Normal = normal;
            }
        }
    }
}
