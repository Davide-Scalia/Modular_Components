using System.Collections.Generic;
using UnityEngine;

namespace RpgToolkit.Spells
{
    [DisallowMultipleComponent]
    public sealed class SpellTrajectoryPreview2D : MonoBehaviour
    {
        [SerializeField] private LineRenderer line;
        [SerializeField, Min(0.001f)] private float step = 0.02f;
        [SerializeField, Min(0f)] private float maximumTime = 5f;
        [SerializeField, Range(2, 256)] private int maximumPoints = 256;
        private readonly List<Vector3> points = new List<Vector3>(256);

        public void Show(SpellDefinition definition, Vector2 origin, Vector2 direction)
        {
            if (line == null) return;
            SpellPreviewSimulator2D.Fill(definition, gameObject, origin, direction, points,
                step, maximumTime, maximumPoints);
            line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++) line.SetPosition(i, points[i]);
            line.enabled = points.Count > 1;
        }

        public void Hide()
        {
            if (line == null) return;
            line.positionCount = 0;
            line.enabled = false;
        }
    }
}
