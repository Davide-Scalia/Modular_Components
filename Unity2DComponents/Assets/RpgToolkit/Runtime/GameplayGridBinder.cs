using UnityEngine;

namespace RpgToolkit
{
    public sealed class GameplayGridBinder : MonoBehaviour
    {
        [SerializeField] private GameplayGrid2D grid;
        [SerializeField] private GridMover2D[] actors;

        private void Start()
        {
            if (grid == null) { Debug.LogError("Gameplay grid binding requires a scene grid.", this); return; }
            foreach (GridMover2D actor in actors)
                if (actor != null) actor.Bind(grid);
        }
    }
}
