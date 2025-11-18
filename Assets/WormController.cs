using System.Collections.Generic;
using UnityEngine;

public class WormController : MonoBehaviour
{
    [Header("Refs")]
    public LevelManager level;
    public GameObject headPrefab;
    public GameObject bodyPrefab;

    [Header("Snake Config")]
    public int initialLength = 3; // head + body + tail (đều dùng body sprite trừ head)

    private List<Vector2Int> cells = new List<Vector2Int>();   // 0 = head
    private List<Transform> segments = new List<Transform>();  // head + bodies

    void Start()
    {
        InitSnake();
    }

    void Update()
    {
        // Mỗi lần bấm 1 phím = 1 bước
        if (Input.GetKeyDown(KeyCode.W))
        {
            TryMove(Vector2Int.up);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            TryMove(Vector2Int.down);
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            TryMove(Vector2Int.left);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            TryMove(Vector2Int.right);
        }
    }

    void InitSnake()
    {
        cells.Clear();
        segments.Clear();

        // Head ở startPos, body/tail kéo sang trái
        Vector2Int headCell = level.startPos;
        cells.Add(headCell);
        for (int i = 1; i < initialLength; i++)
        {
            cells.Add(headCell + Vector2Int.left * i);
        }

        // Tạo segment tương ứng
        EnsureSegmentsMatchCells();
        SyncVisuals();
    }

    void TryMove(Vector2Int direction)
    {
        Vector2Int newHead = cells[0] + direction;

        // 1. Chết nếu ra ngoài map
        if (!level.InBounds(newHead))
        {
            Die("Rơi khỏi map");
            return;
        }

        // 2. Không cho đi xuyên tường #
        if (level.IsWall(newHead))
        {
            // Đơn giản là bỏ qua input, rắn không di chuyển
            return;
        }

        // 3. Không cho đi vào thân (nhưng không chết, chỉ block)
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i] == newHead)
            {
                return; // block bước này
            }
        }

        bool grew = false;

        // 4. Apple
        if (level.IsApple(newHead))
        {
            grew = true;
            level.EatApple(newHead);
        }

        // 5. Cập nhật list ô
        cells.Insert(0, newHead);

        if (!grew)
        {
            cells.RemoveAt(cells.Count - 1);
        }

        EnsureSegmentsMatchCells();
        SyncVisuals();

        // 6. Gravity
        ApplyGravity();

        // 7. Win
        if (level.IsPortal(cells[0]) && level.AllApplesEaten())
        {
            Debug.Log("WIN LEVEL!");
        }
    }


    void ApplyGravity()
    {
        while (CanFallOneStep())
        {
            for (int i = 0; i < cells.Count; i++)
            {
                cells[i] += Vector2Int.down;
            }

            // Chỉ chết nếu rơi ra ngoài map
            for (int i = 0; i < cells.Count; i++)
            {
                if (!level.InBounds(cells[i]))
                {
                    Die("Rơi khỏi map");
                    return;
                }
            }
        }

        SyncVisuals();
    }


    /// <summary>
    /// Trả về true nếu KHÔNG có vật cản dưới BẤT KỲ phần nào của rắn
    /// => toàn bộ rắn rơi xuống 1 ô.
    /// </summary>
    bool CanFallOneStep()
    {
        // Nếu có ÍT NHẤT một ô có khối cứng bên dưới -> KHÔNG rơi.
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int below = cells[i] + Vector2Int.down;
            if (level.IsSolid(below))
            {
                return false; // có nền dưới 1 phần -> đủ để giữ rắn
            }
        }

        // Không có ô nào có nền cứng dưới -> rơi
        return true;
    }

    void EnsureSegmentsMatchCells()
    {
        // Nếu thiếu segment → tạo thêm
        while (segments.Count < cells.Count)
        {
            GameObject prefabToUse = (segments.Count == 0) ? headPrefab : bodyPrefab;
            var seg = Instantiate(prefabToUse, Vector3.zero, Quaternion.identity);
            segments.Add(seg.transform);
        }

        // Nếu thừa segment (hiếm khi xảy ra) → xóa bớt
        while (segments.Count > cells.Count)
        {
            var last = segments[segments.Count - 1];
            Destroy(last.gameObject);
            segments.RemoveAt(segments.Count - 1);
        }
    }

    void SyncVisuals()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            segments[i].position = level.GridToWorld(cells[i]);
        }
    }

    void Die(string reason)
    {
        Debug.Log("DEAD: " + reason);
        // TODO: Restart level, show UI, v.v.
        // Tạm thời disable script
        enabled = false;
    }
}
