using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static OrientGrid;
using static UnityEngine.Mathf;

public class OrientGridController : MonoBehaviour
{
    public GameObject SolidCube;
    public GameObject EmptyCube;

    void Start()
    {
        var grid = new OrientGrid(new Vector3Int(10, 10, 10), 1.01f);
        RandomPatternsTest(grid);
        StartCoroutine(DisplayCubes(grid.GetCells()));
    }

    void RandomPatternsTest(OrientGrid grid)
    {
        var lPattern = new Vector3Int[]
        {
            new Vector3Int(0,0,0),
            new Vector3Int(1,0,0),
            new Vector3Int(2,0,0),
            new Vector3Int(0,1,0),
            new Vector3Int(0,2,0),
            new Vector3Int(0,3,0)
        };

        int tryCount = 0;
        int tilesCount = 0;
        while (tryCount++ < 200)
        {
            if (grid.TryPlacePattern(tilesCount + 1, lPattern, RandomIndex(), RandomRotation()))
                tilesCount++;
        }

        Debug.Log($"{tilesCount} patterns added.");

        Vector3Int RandomIndex()
        {
            int x = Random.Range(0, grid.GridSize.x);
            int y = Random.Range(0, grid.GridSize.y);
            int z = Random.Range(0, grid.GridSize.z);
            return new Vector3Int(x, y, z);
        }

        Quaternion RandomRotation()
        {
            int x = Random.Range(0, 4) * 90;
            int y = Random.Range(0, 4) * 90;
            int z = Random.Range(0, 4) * 90;
            return Quaternion.Euler(x, y, z);
        }
    }

    void PatternTest(OrientGrid grid)
    {
        var lPattern = new Vector3Int[]
        {
            new Vector3Int(0,0,0),
            new Vector3Int(1,0,0),
            new Vector3Int(2,0,0),
            new Vector3Int(0,1,0),
            new Vector3Int(0,2,0),
            new Vector3Int(0,3,0)
        };

        var anchor = new Vector3Int(2, 8, 0);
        var rotation = Quaternion.Euler(0, 0, -90);

        if (!grid.TryPlacePattern(1, lPattern, anchor, rotation))
            Debug.Log("Pattern outside grid bounds.");
    }

    void SingleCellTest(OrientGrid grid)
    {
        var index = new Vector3Int(2, 0, 0);
        var anchor = new Vector3Int(5, 2, 0);
        var rotation = Quaternion.Euler(0, 0, -190);

        if (grid.TryOrientIndex(index, anchor, rotation, out var rotated))
        {
            var cell = grid.GetCell(rotated);
            cell.Tile = 1;
        }
        else
            Debug.Log("Oriented cell outside grid bounds.");
    }

    IEnumerator DisplayCubes(IEnumerable<Cell> cells)
    {
        var materials = new Dictionary<int, Material>();

        foreach (var cell in cells)
        {
            var prefab = cell.IsEmpty ? EmptyCube : SolidCube;
            var cube = Instantiate(prefab, cell.GetCenter(), Quaternion.identity, transform);

            if (!cell.IsEmpty)
                cube.GetComponent<MeshRenderer>().sharedMaterial = GetMaterial(cell.Tile);
        }

        yield return new WaitForSeconds(0);

        Material GetMaterial(int tile)
        {
            if (!materials.TryGetValue(tile, out var material))
            {
                material = new Material(SolidCube.GetComponent<MeshRenderer>().sharedMaterial);
                material.color = Random.ColorHSV();
                materials.Add(tile, material);
            }

            return material;
        }
    }
}

class OrientGrid
{
    public Vector3Int GridSize;
    Cell[,,] _grid;
    float _cellSize;

    public OrientGrid(Vector3Int gridSize, float cellSize)
    {
        GridSize = gridSize;
        _cellSize = cellSize;
        _grid = new Cell[gridSize.x, gridSize.y, gridSize.z];

        for (int z = 0; z < gridSize.z; z++)
            for (int y = 0; y < gridSize.y; y++)
                for (int x = 0; x < gridSize.x; x++)
                {
                    _grid[x, y, z] = new Cell(this)
                    {
                        Index = new Vector3Int(x, y, z)
                    };
                }
    }

    public bool TryPlacePattern(int tile, IEnumerable<Vector3Int> pattern, Vector3Int anchor, Quaternion rotation)
    {
        var indices = new List<Vector3Int>();

        foreach (var index in pattern)
        {
            if (!TryOrientIndex(index, anchor, rotation, out var worldIndex))
                return false;

            indices.Add(worldIndex);
        }

        if (indices.Any(i => !GetCell(i).IsEmpty)) return false;

        foreach (var index in indices)
            GetCell(index).Tile = tile;

        return true;
    }

    public bool TryOrientIndex(Vector3Int localIndex, Vector3Int anchor, Quaternion rotation, out Vector3Int worldIndex)
    {
        var rotated = rotation * localIndex;
        worldIndex = anchor + ToInt(rotated);
        return CheckBounds(worldIndex);
    }

    bool CheckBounds(Vector3Int index)
    {
        if (index.x < 0) return false;
        if (index.y < 0) return false;
        if (index.z < 0) return false;
        if (index.x >= GridSize.x) return false;
        if (index.y >= GridSize.y) return false;
        if (index.z >= GridSize.z) return false;
        return true;
    }

    public IEnumerable<Cell> GetCells()
    {
        for (int z = 0; z < GridSize.z; z++)
            for (int y = 0; y < GridSize.y; y++)
                for (int x = 0; x < GridSize.x; x++)
                    yield return _grid[x, y, z];
    }

    public Cell GetCell(Vector3Int index) => _grid[index.x, index.y, index.z];

    Vector3Int ToInt(Vector3 v) => new Vector3Int(RoundToInt(v.x), RoundToInt(v.y), RoundToInt(v.z));

    public class Cell
    {
        public Vector3Int Index;
        public int Tile;
        OrientGrid _grid;

        public bool IsEmpty => Tile == 0;

        public Cell(OrientGrid grid)
        {
            _grid = grid;
        }

        public Vector3 GetCenter() => (Vector3)Index * _grid._cellSize;
    }
}

