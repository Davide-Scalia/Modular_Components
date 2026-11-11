using UnityEngine;

namespace ModularGrid
{
    public sealed class GridPlacementController : MonoBehaviour
    {
        [SerializeField] private GridManager grid;
        [SerializeField] private GridPointerPicker pointerPicker;
        [SerializeField] private GridFootprint activeFootprint;
        [SerializeField] private GridInputRouter inputRouter;

        private GridFootprint preview;
        private Renderer[] previewRenderers;
        private Collider[] previewColliders;
        private Collider2D[] previewColliders2D;
        private MaterialPropertyBlock previewBlock;
        private GridCoordinate hoveredCell;
        private bool canPlace;
        private bool placeRequested;
        private bool rotateRequested;

        public bool CanPlaceAtHoveredCell => canPlace;

        private void Awake()
        {
            previewBlock = new MaterialPropertyBlock();
            if (inputRouter == null)
                inputRouter = GetComponent<GridInputRouter>();
        }

        private void OnEnable()
        {
            if (inputRouter == null)
                inputRouter = GetComponent<GridInputRouter>();
            if (inputRouter != null)
            {
                inputRouter.PlaceRequested += RequestPlace;
                inputRouter.RotateRequested += RequestRotate;
            }
        }

        private void OnDisable()
        {
            if (inputRouter != null)
            {
                inputRouter.PlaceRequested -= RequestPlace;
                inputRouter.RotateRequested -= RequestRotate;
            }
        }

        private void Update()
        {
            if (grid == null || pointerPicker == null || activeFootprint == null || inputRouter == null)
                return;

            EnsurePreview();
            if (preview == null)
                return;

            if (inputRouter.PointerOverUI)
            {
                canPlace = false;
                placeRequested = false;
                rotateRequested = false;
                preview.gameObject.SetActive(false);
                return;
            }

            if (rotateRequested)
            {
                preview.Rotate(grid.Shape);
                rotateRequested = false;
            }

            if (!pointerPicker.TryGetHoveredCell(inputRouter.PointerPosition, out GridCell cell))
            {
                canPlace = false;
                placeRequested = false;
                preview.gameObject.SetActive(false);
                return;
            }

            hoveredCell = cell.Coordinate;
            preview.gameObject.SetActive(true);
            preview.transform.position = grid.GetFootprintPivotWorld(preview, hoveredCell);
            preview.ApplyRotation(grid.Shape, grid.Use2DPhysics);
            canPlace = grid.CanPlace(preview, hoveredCell);
            ShowValidity(canPlace);

            if (placeRequested && canPlace && grid.TryPlace(preview, hoveredCell))
            {
                SetPreviewColliders(true);
                ClearValidity();
                preview = null;
                EnsurePreview();
            }
            placeRequested = false;
        }

        public void BeginPlacement(GridFootprint footprint)
        {
            if (preview != null)
                Destroy(preview.gameObject);

            activeFootprint = footprint;
            preview = null;
            EnsurePreview();
        }

        private void RequestPlace() => placeRequested = true;
        private void RequestRotate() => rotateRequested = true;

        private void EnsurePreview()
        {
            if (preview != null || activeFootprint == null)
                return;

            preview = Instantiate(activeFootprint);
            preview.name = $"{activeFootprint.name} Preview";
            previewRenderers = preview.GetComponentsInChildren<Renderer>();
            previewColliders = preview.GetComponentsInChildren<Collider>();
            previewColliders2D = preview.GetComponentsInChildren<Collider2D>();
            SetPreviewColliders(false);
        }

        private void SetPreviewColliders(bool enabled)
        {
            if (previewColliders != null)
            {
                foreach (Collider collider in previewColliders)
                    collider.enabled = enabled;
            }

            if (previewColliders2D != null)
            {
                foreach (Collider2D collider in previewColliders2D)
                    collider.enabled = enabled;
            }
        }

        private void ShowValidity(bool valid)
        {
            Color color = valid ? new Color(0.3f, 1f, 0.3f, 0.65f) : new Color(1f, 0.3f, 0.3f, 0.65f);
            foreach (Renderer renderer in previewRenderers)
            {
                renderer.GetPropertyBlock(previewBlock);
                previewBlock.SetColor("_BaseColor", color);
                previewBlock.SetColor("_Color", color);
                renderer.SetPropertyBlock(previewBlock);
            }
        }

        private void ClearValidity()
        {
            if (previewRenderers == null)
                return;

            foreach (Renderer renderer in previewRenderers)
                renderer.SetPropertyBlock(null);
        }
    }
}
