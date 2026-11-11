using System;
using UnityEngine;

namespace RpgToolkit
{
    // Input is a source of intent. Movement components never read devices.
    public sealed class CharacterIntent : MonoBehaviour
    {
        public event Action<Vector2> DirectionChanged;
        public event Action<Vector2> DestinationRequested;
        public event Action<Vector2Int> CellStepRequested;

        public void Move(Vector2 direction) => DirectionChanged?.Invoke(direction);
        public void GoTo(Vector2 destination) => DestinationRequested?.Invoke(destination);
        public void Step(Vector2Int direction) => CellStepRequested?.Invoke(direction);

        private void OnDisable() => DirectionChanged?.Invoke(Vector2.zero);
    }

}
