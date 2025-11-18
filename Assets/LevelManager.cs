using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Level Data")]
    public string[] rawLevel = new string[]
    {
        "##########", // y = 0: sàn đáy map
        "#......P.#", // y = 1: đường đi + portal P (đứng ở ô này để win)
        "#..###...#", // y = 2: cột tường đỡ platform phía trên
        "#..S.A...#", // y = 3: S = spawn, A = apple
        "##########", // y = 4: trần (ngăn đi xuyên lên trên)
    };


    // rawLevel[0] là dòng dưới cùng
    public float cellSize = 1f;

    [Header("Prefabs")]
    public GameObject groundPrefab;
    public GameObject applePrefab;
    public GameObject portalPrefab;

    [Header("Runtime info")]
    public Vector2Int startPos;
    public Vector2Int portalPos;

    private Dictionary<Vector2Int, GameObject> apples =
        new Dictionary<Vector2Int, GameObject>();

    public int Width => rawLevel.Length > 0 ? rawLevel[0].Length : 0;
    public int Height => rawLevel.Length;

    void Start()
    {
        BuildLevel();
    }

    public bool InBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < Width &&
               cell.y >= 0 && cell.y < Height;
    }
    public bool IsWall(Vector2Int cell)
    {
        if (!InBounds(cell)) return false;
        char c = GetChar(cell);
        return c == '#'; // chỉ # là tường không đi vào được
    }

    public char GetChar(Vector2Int cell)
    {
        if (!InBounds(cell)) return ' ';
        return rawLevel[cell.y][cell.x];
    }

    /// <summary>
    /// Khối cứng, dùng làm nền chặn rơi.
    /// </summary>
    public bool IsSolid(Vector2Int cell)
    {
        if (!InBounds(cell)) return false;
        char c = GetChar(cell);
        return c == '#' || c == 'A' || c == 'P' || c == 'S';
    }


    /// <summary>
    /// Ô rắn có thể đứng (không phải vực chết).
    /// </summary>
    public bool CanOccupy(Vector2Int cell)
    {
        return InBounds(cell); // chỉ cần còn trong map là được
    }


    public bool IsApple(Vector2Int cell) => apples.ContainsKey(cell);

    public void EatApple(Vector2Int cell)
    {
        if (!apples.ContainsKey(cell)) return;
        Destroy(apples[cell]);
        apples.Remove(cell);
    }

    public bool AllApplesEaten() => apples.Count == 0;

    public bool IsPortal(Vector2Int cell) => cell == portalPos;

    public Vector3 GridToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x * cellSize, cell.y * cellSize, 0f);
    }

    void BuildLevel()
    {
        apples.Clear();

        for (int y = 0; y < Height; y++)
        {
            string line = rawLevel[y];
            for (int x = 0; x < line.Length; x++)
            {
                char c = line[x];
                Vector2Int cell = new Vector2Int(x, y);

                // Khối cứng (nền)
                if (c == '#' || c == 'S' || c == 'A' || c == 'P')
                {
                    Instantiate(groundPrefab, GridToWorld(cell), Quaternion.identity);
                }

                if (c == 'S')
                {
                    startPos = cell;
                }

                if (c == 'A')
                {
                    var apple = Instantiate(applePrefab, GridToWorld(cell), Quaternion.identity);
                    apples[cell] = apple;
                }

                if (c == 'P')
                {
                    Instantiate(portalPrefab, GridToWorld(cell), Quaternion.identity);
                    portalPos = cell;
                }
            }
        }
    }
}
