using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ModularGrid
{
    public enum PlacementStrategy
    {
        Free,
        Adjacent
    }

    public enum GridConnectionDirectionality
    {
        Unidirectional,
        Bidirectional
    }

    public sealed class GridManager : MonoBehaviour
    {
        [SerializeField] private GridShape shape = GridShape.Square;
        [SerializeField, Min(1)] private int width = 10;
        [SerializeField, Min(1)] private int height = 10;
        [SerializeField, Min(0.01f)] private float cellSize = 1f;
        [SerializeField] bool use2DPhysics = true;
        [SerializeField] bool useMeshes = true;
        [SerializeField] private PlacementStrategy placementStrategy = PlacementStrategy.Free;
        [SerializeField, Tooltip("Bidirectional accepts either endpoint role. Unidirectional requires a Start-to-End match.")]
        private GridConnectionDirectionality connectionDirectionality = GridConnectionDirectionality.Bidirectional;

        [SerializeField] GameObject TileBase;

        [SerializeField] private Material tileMaterial;

        private readonly HashSet<GridCoordinate> occupiedCells = new HashSet<GridCoordinate>();
        private readonly GridConnectionResolver connectionResolver = new GridConnectionResolver();
        private Transform generatedTiles;

        public GridShape Shape => shape;
        public bool Use2DPhysics => use2DPhysics;
        public PlacementStrategy Strategy => placementStrategy;
        public GridConnectionDirectionality ConnectionDirectionality => connectionDirectionality;
        public LayerMask TileLayerMask => 1 << gameObject.layer;

        private void Awake()
        {
            GenerateGrid();
        }

        [ContextMenu("Generate Grid")]
        public void GenerateGrid()
        {
            RemoveGeneratedTiles();

            generatedTiles = new GameObject("Generated Grid").transform;
            generatedTiles.SetParent(transform, false);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    CreateCell(new GridCoordinate(x, y));
            }
        }

        public bool Contains(GridCoordinate coordinate) =>
            coordinate.X >= 0 && coordinate.X < width && coordinate.Y >= 0 && coordinate.Y < height;

        public Vector3 CellToWorld(GridCoordinate coordinate)
        {
            Vector2 local = GridLayout.CellToLocal(coordinate, shape, cellSize);
            Vector3 point = use2DPhysics
                ? new Vector3(local.x, local.y, 0f)
                : new Vector3(local.x, 0f, local.y);
            return transform.TransformPoint(point);
        }

        public Vector3 GetFootprintPivotWorld(GridFootprint footprint, GridCoordinate anchor)
        {
            Vector2 localOffset = GridLayout.CellToLocal(footprint.PivotOffset(shape), shape, cellSize);
            Vector3 planeOffset = use2DPhysics
                ? new Vector3(localOffset.x, localOffset.y, 0f)
                : new Vector3(localOffset.x, 0f, localOffset.y);
            return CellToWorld(anchor) + transform.TransformVector(planeOffset);
        }

        public bool CanPlace(GridFootprint footprint, GridCoordinate anchor)
            => CanPlace(footprint, anchor, footprint == null ? 0 : footprint.RotationSteps);

        private bool CanPlace(GridFootprint footprint, GridCoordinate anchor, int rotationSteps)
        {
            if (footprint == null)
                return false;

            HashSet<GridCoordinate> proposedCells = new HashSet<GridCoordinate>();
            List<GridCoordinate> proposedCellList = new List<GridCoordinate>();
            foreach (GridCoordinate cell in footprint.GetOccupiedCells(anchor, shape, rotationSteps))
            {
                if (!Contains(cell) || occupiedCells.Contains(cell) || !proposedCells.Add(cell))
                    return false;
                proposedCellList.Add(cell);
            }

            if (proposedCells.Count == 0)
                return false;

            if (placementStrategy == PlacementStrategy.Adjacent && occupiedCells.Count > 0)
            {
                bool touchesExistingPiece = false;
                foreach (GridCoordinate cell in proposedCells)
                {
                    foreach (GridCoordinate neighbor in GridLayout.GetNeighbors(shape))
                    {
                        if (occupiedCells.Contains(cell + neighbor))
                            touchesExistingPiece = true;
                    }
                }

                if (!touchesExistingPiece)
                    return false;

                GridPieceConnections connections = footprint.GetComponent<GridPieceConnections>();
                if (connectionResolver.RequiresConnection(connections, proposedCellList, shape))
                    return connectionResolver.HasMatchingConnection(footprint, connections, anchor, shape, rotationSteps,
                        connectionDirectionality);
            }

            return true;
        }

        /// <summary>Returns all discrete rotations that can be placed at this anchor in the current grid state.</summary>
        public List<int> GetValidRotationSteps(GridFootprint footprint, GridCoordinate anchor)
        {
            List<int> validRotations = new List<int>();
            if (footprint == null)
                return validRotations;

            int rotationCount = shape == GridShape.Square ? 4 : 6;
            for (int rotation = 0; rotation < rotationCount; rotation++)
                if (CanPlace(footprint, anchor, rotation))
                    validRotations.Add(rotation);
            return validRotations;
        }

        public bool TryPlace(GridFootprint footprint, GridCoordinate anchor)
        {
            if (!CanPlace(footprint, anchor))
                return false;

            foreach (GridCoordinate cell in footprint.GetOccupiedCells(anchor, shape))
                occupiedCells.Add(cell);

            footprint.transform.position = GetFootprintPivotWorld(footprint, anchor);
            footprint.ApplyRotation(shape, use2DPhysics);
            connectionResolver.Register(footprint, footprint.GetComponent<GridPieceConnections>(), anchor);
            return true;
        }

        public void ClearOccupancy()
        {
            occupiedCells.Clear();
            connectionResolver.Clear();
        }

        private void CreateCell(GridCoordinate coordinate)
        {
           


            if (!useMeshes)
            {
                GameObject tile = Instantiate(TileBase);
                tile.name = $"Cell {coordinate.X},{coordinate.Y}";
                tile.layer = gameObject.layer;
                tile.transform.SetParent(generatedTiles, false);
                tile.transform.position = CellToWorld(coordinate);
                if (!use2DPhysics)
                    tile.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                tile.AddComponent<GridCell>().Initialize(coordinate);
   
            }
            else
            {
                GameObject tile = new GameObject($"Cell {coordinate.X},{coordinate.Y}");
                tile.layer = gameObject.layer;
                tile.transform.SetParent(generatedTiles, false);
                tile.transform.position = CellToWorld(coordinate);
                if (!use2DPhysics)
                    tile.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

                Mesh mesh = CreateCellMesh();
                MeshFilter filter = tile.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                MeshRenderer renderer = tile.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = tileMaterial;
                tile.AddComponent<GridCell>().Initialize(coordinate);

                if (use2DPhysics)
                    Add2DCollider(tile, mesh.vertices);
                else
                    tile.AddComponent<MeshCollider>().sharedMesh = mesh;
            }
            

           
        }

        private Mesh CreateCellMesh()
        {
            Vector3[] vertices;
            if (shape == GridShape.Square)
            {
                float half = cellSize * 0.5f;
                vertices = new[]
                {
                    new Vector3(-half, -half), new Vector3(half, -half),
                    new Vector3(half, half), new Vector3(-half, half)
                };
            }
            else
            {
                vertices = new Vector3[6];
                for (int i = 0; i < 6; i++)
                {
                    float radians = Mathf.Deg2Rad * (30f + i * 60f);
                    vertices[i] = new Vector3(Mathf.Cos(radians) * cellSize, Mathf.Sin(radians) * cellSize);
                }
            }

            int[] triangles = new int[(vertices.Length - 2) * 3];
            for (int i = 0; i < vertices.Length - 2; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            Mesh mesh = new Mesh { name = $"{shape} Grid Cell" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }

        private void Add2DCollider(GameObject tile, Vector3[] vertices)
        {
            if (shape == GridShape.Square)
            {
                BoxCollider2D collider = tile.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one * cellSize;
                return;
            }

            PolygonCollider2D hexCollider = tile.AddComponent<PolygonCollider2D>();
            Vector2[] points = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                points[i] = vertices[i];
            hexCollider.points = points;
        }

        private void RemoveGeneratedTiles()
        {
            Transform existing = transform.Find("Generated Grid");
            if (existing == null)
                return;

            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }
    }
}
