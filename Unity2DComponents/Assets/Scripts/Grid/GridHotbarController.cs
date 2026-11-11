using UnityEngine;

namespace ModularGrid
{
    /// <summary>Connects hotbar selection to GridPlacementController.</summary>
    [RequireComponent(typeof(GridHotbarView))]
    public sealed class GridHotbarController : MonoBehaviour
    {
        [SerializeField] private GridHotbarCatalog catalog;
        [SerializeField] private GridPlacementController placementController;

        private GridHotbarView view;

        private void Awake()
        {
            view = GetComponent<GridHotbarView>();
            view.Build(catalog != null ? catalog.Items : null);
        }

        private void OnEnable()
        {
            if (view == null)
                view = GetComponent<GridHotbarView>();
            view.SelectionRequested += SelectItem;
        }

        private void OnDisable()
        {
            if (view != null)
                view.SelectionRequested -= SelectItem;
        }

        private void SelectItem(int index)
        {
            if (catalog == null || placementController == null || index < 0 || index >= catalog.Items.Count)
                return;

            GridHotbarItem item = catalog.Items[index];
            if (item == null || item.Footprint == null)
                return;

            placementController.BeginPlacement(item.Footprint);
            view.SetSelected(index);
        }
    }
}
