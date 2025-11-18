using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Settings")]
    public float cellSize = 1f;          // kích thước 1 ô
    public Vector3 origin = Vector3.zero; // gốc lưới (0,0,0)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Grid (ô) -> World
    public Vector3 CellToWorld(Vector2Int cell)
    {
        return origin + new Vector3(cell.x * cellSize, cell.y * cellSize, 0f);
    }

    // World -> Grid (ô gần nhất)
    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        Vector3 offset = worldPos - origin;
        int x = Mathf.RoundToInt(offset.x / cellSize);
        int y = Mathf.RoundToInt(offset.y / cellSize);
        return new Vector2Int(x, y);
    }
}