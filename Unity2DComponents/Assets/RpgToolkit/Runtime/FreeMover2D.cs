using UnityEngine;

namespace RpgToolkit
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CharacterIntent))]
    public sealed class FreeMover2D : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float speed = 4f;
        private Rigidbody2D body;
        private CharacterIntent intent;
        private Vector2 direction;

        public void Configure(float movementSpeed) => speed = Mathf.Max(0.01f, movementSpeed);

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            intent = GetComponent<CharacterIntent>();
        }

        private void OnEnable() => intent.DirectionChanged += SetDirection;
        private void OnDisable()
        {
            intent.DirectionChanged -= SetDirection;
            direction = Vector2.zero;
        }

        private void SetDirection(Vector2 value) => direction = Vector2.ClampMagnitude(value, 1f);
        private void FixedUpdate() => body.MovePosition(body.position + direction * speed * Time.fixedDeltaTime);
    }
}
