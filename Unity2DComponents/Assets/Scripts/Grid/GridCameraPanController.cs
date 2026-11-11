using UnityEngine;

namespace ModularGrid
{
    /// <summary>Pans the view in response to middle-button drag events from GridInputRouter.</summary>
    public sealed class GridCameraPanController : MonoBehaviour
    {
        [SerializeField] private GridInputRouter inputRouter;
        [SerializeField, Min(0f), Tooltip("World units the camera moves per screen pixel dragged.")]
        private float panSensitivity = 0.01f;

        private void Awake()
        {
            if (inputRouter == null)
                inputRouter = FindObjectOfType<GridInputRouter>();
        }

        private void OnEnable()
        {
            if (inputRouter == null)
                inputRouter = FindObjectOfType<GridInputRouter>();
            if (inputRouter != null)
                inputRouter.CameraPanDelta += Pan;
        }

        private void OnDisable()
        {
            if (inputRouter != null)
                inputRouter.CameraPanDelta -= Pan;
        }

        private void Pan(Vector2 screenDelta)
        {
            Vector3 movement = -transform.right * screenDelta.x - transform.up * screenDelta.y;
            transform.position += movement * panSensitivity;
        }
    }
}
