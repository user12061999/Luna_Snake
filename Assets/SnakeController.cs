using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeController : MonoBehaviour
{
    [Header("Segments Prefabs")]
    public Transform headPrefab;
    public Transform bodyPrefab;
    public Transform tailPrefab;
    public Transform segmentsParent;

    [Header("Move Settings")]
    public float stepDistance = 1f;
    public float moveDuration = 0.12f;
    public float fallDuration = 0.08f;

    [Header("Physics Masks")]
    public LayerMask wallMask;      // tường / block
    public LayerMask groundMask;    // nền đứng được
    public LayerMask segmentMask;   // layer của các segment rắn
    public LayerMask spikeMask;     // layer Spike
    public LayerMask finishMask;    // layer GateFinish

    private List<Transform> segments = new List<Transform>(); // [0] = head, [last] = tail
    private Vector2 moveDir = Vector2.right;
    private bool isBusy = false;
    private bool moveSucceeded = false;
    private bool isDead = false;
    private bool isWin = false;

    void Start()
    {
        SpawnInitialSnake();
    }

    void Update()
    {
        if (isBusy || isDead || isWin) return;

        Vector2 inputDir = Vector2.zero;

        if (Input.GetKeyDown(KeyCode.W)) inputDir = Vector2.up;
        else if (Input.GetKeyDown(KeyCode.S)) inputDir = Vector2.down;
        else if (Input.GetKeyDown(KeyCode.A)) inputDir = Vector2.left;
        else if (Input.GetKeyDown(KeyCode.D)) inputDir = Vector2.right;

        if (inputDir != Vector2.zero)
        {
            moveDir = inputDir.normalized;
            StartCoroutine(PerformStepWithGravity());
        }
    }

    // =========================================================
    // INIT
    // =========================================================
    void SpawnInitialSnake()
    {
        segments.Clear();

        Vector3 pos = transform.position;

        // Head
        Transform head = Instantiate(headPrefab, pos, Quaternion.identity, segmentsParent);
        segments.Add(head);

        // Body
        Transform body = Instantiate(
            bodyPrefab,
            pos - new Vector3(stepDistance, 0f, 0f),
            Quaternion.identity,
            segmentsParent);
        segments.Add(body);

        // Tail
        Transform tail = Instantiate(
            tailPrefab,
            pos - new Vector3(stepDistance * 2f, 0f, 0f),
            Quaternion.identity,
            segmentsParent);
        segments.Add(tail);

        UpdateSegmentRotations();
    }

    // =========================================================
    // STEP + GRAVITY
    // =========================================================
    IEnumerator PerformStepWithGravity()
    {
        isBusy = true;

        yield return StartCoroutine(MoveOneStepAnimated());

        if (!moveSucceeded || isDead || isWin)
        {
            isBusy = false;
            yield break;
        }

        yield return StartCoroutine(ApplyGravityAnimated());

        isBusy = false;
    }

    // =========================================================
    // MOVE ONE STEP (ANIMATED) + APPLE → GROW SAU HEAD
    // =========================================================
    IEnumerator MoveOneStepAnimated()
    {
        moveSucceeded = false;

        Vector3 dir3 = new Vector3(moveDir.x, moveDir.y, 0f);

        // Block bởi tường + chính cơ thể
        LayerMask blockMask = wallMask | groundMask | segmentMask | spikeMask;
        if (Physics2D.Raycast(segments[0].position, dir3, stepDistance, blockMask))
            yield break;

        // Check apple phía trước
        Collider2D appleToEat = null;
        RaycastHit2D[] hits = Physics2D.RaycastAll(segments[0].position, dir3, stepDistance);
        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.CompareTag("Apple"))
            {
                appleToEat = hit.collider;
                break;
            }
        }
        bool willGrow = (appleToEat != null);

        // --------- Lưu vị trí trước khi move ---------
        List<Vector3> startPos = GetPositions();
        int oldCount = startPos.Count;

        // Target positions cho chuỗi cũ (chưa grow)
        List<Vector3> targetPos = new List<Vector3>(startPos);
        targetPos[0] = startPos[0] + dir3 * stepDistance;  // head mới
        for (int i = 1; i < oldCount; i++)
            targetPos[i] = startPos[i - 1];                // mỗi đoạn tới vị trí của đoạn trước

        // --------- Animate move ---------
        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / moveDuration);

            for (int i = 0; i < segments.Count; i++)
                segments[i].position = Vector3.Lerp(startPos[i], targetPos[i], a);

            UpdateSegmentRotations();
            yield return null;
        }

        // snap
        for (int i = 0; i < segments.Count; i++)
            segments[i].position = targetPos[i];

        // Sau khi di chuyển, check Spike / Gate
        if (CheckSpikeHit() || CheckFinish())
        {
            moveSucceeded = false;
            yield break;
        }

        // --------- Nếu không có apple -> xong ---------
        if (!willGrow)
        {
            moveSucceeded = true;
            UpdateSegmentRotations();
            yield break;
        }

        // --------- Có apple -> GROW NGAY SAU HEAD ---------
        Destroy(appleToEat.gameObject);

        // Chuỗi vị trí mới, dài hơn 1:
        // new[0] = head mới
        // new[1] = head cũ
        // new[2..] = các đoạn cũ giữ nguyên
        List<Vector3> newPositions = new List<Vector3>(oldCount + 1);
        newPositions.Add(startPos[0] + dir3 * stepDistance); // head mới
        newPositions.Add(startPos[0]);                       // body mới sau head
        for (int i = 1; i < oldCount; i++)
            newPositions.Add(startPos[i]);                   // phần còn lại

        // Tạo body mới, chèn vào segments[1]
        Transform newBody = Instantiate(
            bodyPrefab,
            newPositions[1],
            Quaternion.identity,
            segmentsParent
        );
        segments.Insert(1, newBody);

        // Áp vị trí mới cho toàn bộ segments
        for (int i = 0; i < segments.Count; i++)
            segments[i].position = newPositions[i];

        if (CheckSpikeHit() || CheckFinish())
        {
            moveSucceeded = false;
            yield break;
        }

        moveSucceeded = true;
        UpdateSegmentRotations();
    }

    // =========================================================
    // GRAVITY (ANIMATED)
    // =========================================================
    IEnumerator ApplyGravityAnimated()
    {
        while (ShouldFallOneUnit())
        {
            List<Vector3> startPos = GetPositions();
            List<Vector3> targetPos = new List<Vector3>(startPos.Count);
            for (int i = 0; i < startPos.Count; i++)
                targetPos.Add(startPos[i] + Vector3.down * stepDistance);

            float t = 0f;
            while (t < fallDuration)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / fallDuration);

                for (int i = 0; i < segments.Count; i++)
                    segments[i].position = Vector3.Lerp(startPos[i], targetPos[i], a);

                UpdateSegmentRotations();
                yield return null;
            }

            for (int i = 0; i < segments.Count; i++)
                segments[i].position = targetPos[i];

            if (CheckSpikeHit() || CheckFinish())
            {
                yield break;
            }
        }
    }


    bool ShouldFallOneUnit()
    {
        LayerMask supportMask = groundMask | wallMask | finishMask;

        for (int i = 0; i < segments.Count; i++)
        {
            if (Physics2D.Raycast(
                    segments[i].position,
                    Vector2.down,
                    stepDistance * 0.9f,
                    supportMask))
            {
                return false;
            }
        }
        return true;
    }



    // =========================================================
    // SPIKE & FINISH
    // =========================================================
    bool CheckSpikeHit()
    {
        // GameOver nếu: có ÍT NHẤT 1 spike làm điểm tựa
        // và KHÔNG có bất kỳ điểm tựa an toàn nào (ground/wall/finish) cho cả con rắn
        float checkDist = stepDistance * 0.9f;

        LayerMask safeSupportMask = groundMask | wallMask | finishMask;
        LayerMask combinedMask = safeSupportMask | spikeMask;

        bool hasAnySpikeSupport = false;
        bool hasAnySafeSupport = false;

        for (int i = 0; i < segments.Count; i++)
        {
            Transform seg = segments[i];

            // RaycastAll xuống dưới để xem dưới mỗi segment có gì
            RaycastHit2D[] hits = Physics2D.RaycastAll(
                seg.position,
                Vector2.down,
                checkDist,
                combinedMask
            );

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;

                int hitLayerMask = 1 << hit.collider.gameObject.layer;

                if ((spikeMask & hitLayerMask) != 0)
                    hasAnySpikeSupport = true;

                if ((safeSupportMask & hitLayerMask) != 0)
                    hasAnySafeSupport = true;
            }
        }

        // Có spike nhưng KHÔNG có bất kỳ điểm tựa an toàn nào -> chết
        if (hasAnySpikeSupport && !hasAnySafeSupport)
        {
            GameOver();
            return true;
        }

        return false;
    }




    bool CheckFinish()
    {
        // chỉ cần head chạm GateFinish là win
        float radius = stepDistance * 0.45f;
        Transform head = segments[0];

        Collider2D hit = Physics2D.OverlapCircle(head.position, radius, finishMask);
        if (hit != null || (hit != null && hit.CompareTag("GateFinish")))
        {
            WinGame();
            return true;
        }
        return false;
    }

    void GameOver()
    {
        if (isDead || isWin) return;
        isDead = true;
        Debug.Log("GAME OVER (Spike)");
        // TODO: gọi UI / reload level
    }

    void WinGame()
    {
        if (isWin || isDead) return;
        isWin = true;
        Debug.Log("WIN! (GateFinish)");
        // TODO: chuyển level / hiện màn thắng
    }

    // =========================================================
    // UTILS
    // =========================================================
    List<Vector3> GetPositions()
    {
        List<Vector3> result = new List<Vector3>(segments.Count);
        for (int i = 0; i < segments.Count; i++)
            result.Add(segments[i].position);
        return result;
    }

    void UpdateSegmentRotations()
    {
        if (segments.Count == 0) return;

        // xoay HEAD theo hướng moveDir
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            float headAngle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            segments[0].rotation = Quaternion.Euler(0, 0, headAngle);
        }

        // xoay TAIL theo hướng từ segment trước -> tail
        if (segments.Count >= 2)
        {
            Transform tail = segments[segments.Count - 1];
            Transform beforeTail = segments[segments.Count - 2];

            Vector3 dir = (tail.position - beforeTail.position).normalized;
            if (dir.sqrMagnitude > 0.0001f)
            {
                float tailAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                tail.rotation = Quaternion.Euler(0, 0, tailAngle);
            }
        }
    }

    // tiện debug vùng OverlapCircle
    void OnDrawGizmosSelected()
    {
        if (segments == null || segments.Count == 0) return;

        Gizmos.color = Color.red;
        float r = stepDistance * 0.45f;
        foreach (var seg in segments)
        {
            if (seg != null)
                Gizmos.DrawWireSphere(seg.position, r);
        }
    }
}
