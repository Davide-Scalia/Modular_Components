using UnityEngine;

namespace ModularGrid
{
    /// <summary>Highlights the grid tile under the pointer independently of placement behavior.</summary>
    public sealed class GridTileHover : MonoBehaviour
    {
        [SerializeField] private GridPointerPicker pointerPicker;
        [SerializeField] private GridInputRouter inputRouter;
        [SerializeField] private Color hoverColor = new Color(1f, 0.85f, 0.2f, 1f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private GridCell currentCell;
        private Renderer currentRenderer;
        private MaterialPropertyBlock originalBlock;
        private MaterialPropertyBlock hoverBlock;

        private void Awake()
        {
            originalBlock = new MaterialPropertyBlock();
            hoverBlock = new MaterialPropertyBlock();
            if (inputRouter == null)
                inputRouter = FindObjectOfType<GridInputRouter>();
        }

        private void Update()
        {
            if (inputRouter == null || inputRouter.PointerOverUI)
            {
                ClearCurrent();
                return;
            }

            GridCell nextCell = pointerPicker != null &&
                                pointerPicker.TryGetHoveredCell(inputRouter.PointerPosition, out GridCell cell) ? cell : null;
            if (nextCell == currentCell)
                return;

            ClearCurrent();
            currentCell = nextCell;
            if (currentCell == null)
                return;

            currentRenderer = currentCell.GetComponentInChildren<Renderer>();
            if (currentRenderer == null)
                return;

            currentRenderer.GetPropertyBlock(originalBlock);
            currentRenderer.GetPropertyBlock(hoverBlock);
            hoverBlock.SetColor(BaseColorId, hoverColor);
            hoverBlock.SetColor(ColorId, hoverColor);
            currentRenderer.SetPropertyBlock(hoverBlock);
        }

        private void OnDisable() => ClearCurrent();

        private void ClearCurrent()
        {
            if (currentRenderer != null)
                currentRenderer.SetPropertyBlock(originalBlock);
            currentCell = null;
            currentRenderer = null;
        }
    }
}
