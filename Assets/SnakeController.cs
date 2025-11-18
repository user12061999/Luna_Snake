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

    public LayerMask wallMask;
    public LayerMask groundMask;
    public LayerMask segmentMask;

    private List<Transform> segments = new List<Transform>(); // [0] = head, [last] = tail
    private Vector2 moveDir = Vector2.right;
    private bool isBusy = false;
    private bool moveSucceeded = false;

    void Start()
    {
        SpawnInitialSnake();
    }

    void Update()
    {
        if (isBusy) return;

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

        // 1. Di chuyển 1 bước (có animation + ăn apple + grow)
        yield return StartCoroutine(MoveOneStepAnimated());

        if (!moveSucceeded)
        {
            isBusy = false;
            yield break;
        }

        // 2. Gravity rơi xuống (nếu cần)
        yield return StartCoroutine(ApplyGravityAnimated());

        isBusy = false;
    }

    // =========================================================
    // MOVE ONE STEP (ANIMATED) + ĂN APPLE → GROW SAU HEAD
    // =========================================================
    IEnumerator MoveOneStepAnimated()
{
    moveSucceeded = false;

    Vector3 dir3 = new Vector3(moveDir.x, moveDir.y, 0f);

    // 1. Block bởi tường + chính cơ thể
    LayerMask blockMask = wallMask | segmentMask;
    if (Physics2D.Raycast(segments[0].position, dir3, stepDistance, blockMask))
        yield break;

    // 2. Check apple phía trước
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

    // ---- LƯU VỊ TRÍ TRƯỚC KHI MOVE ----
    List<Vector3> startPos = GetPositions();
    int oldCount = startPos.Count;

    // ---- TARGET KHI KHÔNG GROW (chuỗi chuẩn) ----
    List<Vector3> targetPos = new List<Vector3>(startPos);

    targetPos[0] = startPos[0] + dir3 * stepDistance; // head mới
    for (int i = 1; i < oldCount; i++)
        targetPos[i] = startPos[i - 1];               // mỗi đoạn tới vị trí đoạn trước

    // 3. Animate move (với số segment hiện tại)
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

    // Snap
    for (int i = 0; i < segments.Count; i++)
        segments[i].position = targetPos[i];

    // 4. Nếu KHÔNG có apple -> xong luôn
    if (!willGrow)
    {
        moveSucceeded = true;
        UpdateSegmentRotations();
        yield break;
    }

    // 5. Có apple -> GROW SAU HEAD
    Destroy(appleToEat.gameObject);

    // Tính lại vị trí CHUỖI MỚI (dài hơn 1)
    // newPositions[0] = head mới
    // newPositions[1] = head cũ
    // newPositions[2..] = tất cả startPos[1..] (body, tail giữ nguyên)
    List<Vector3> newPositions = new List<Vector3>(oldCount + 1);
    newPositions.Add(startPos[0] + dir3 * stepDistance); // head mới
    newPositions.Add(startPos[0]);                       // body mới sau head
    for (int i = 1; i < oldCount; i++)
        newPositions.Add(startPos[i]);                   // phần còn lại giữ nguyên

    // Tạo body mới và chèn vào segments[1]
    Transform newBody = Instantiate(
        bodyPrefab,
        newPositions[1],
        Quaternion.identity,
        segmentsParent
    );
    segments.Insert(1, newBody);

    // Áp vị trí newPositions cho toàn bộ segments (giờ đã dài hơn 1)
    for (int i = 0; i < segments.Count; i++)
        segments[i].position = newPositions[i];

    moveSucceeded = true;
    UpdateSegmentRotations();
}


    // =========================================================
    // GRAVITY ANIMATED
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
        }
    }

    bool ShouldFallOneUnit()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            Transform seg = segments[i];

            // nếu có ground hoặc wall ngay dưới → không rơi
            if (Physics2D.Raycast(
                seg.position,
                Vector2.down,
                stepDistance * 0.9f,
                groundMask | wallMask))
            {
                return false;
            }
        }
        return true;
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
}
