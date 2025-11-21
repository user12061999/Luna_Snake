using System.Collections.Generic;
using Spine.Unity;
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
    public LayerMask wallMask;
    public LayerMask groundMask;
    public LayerMask segmentMask;
    public LayerMask spikeMask;
    public LayerMask finishMask;
    public LayerMask appleMask;

    [Header("Finish Gate")]
    public float gateSuckDurationPerSegment = 0.08f;

    // ---- STATE DÙNG CHUNG CHO CÁC SCRIPT KHÁC ----
    [HideInInspector] public List<Transform> segments = new List<Transform>(); // [0] = head
    [HideInInspector] public Vector2 moveDir = Vector2.right;
    [HideInInspector] public bool isBusy = false;
    [HideInInspector] public bool moveSucceeded = false;
    [HideInInspector] public bool isDead = false;
    [HideInInspector] public bool isWin = false;
    [HideInInspector] public bool isFinishing = false;
    [HideInInspector] public SnakeHeadAnimator headAnim; 

    // ---- THAM CHIẾU CÁC MODULE KHÁC ----
    private SnakeMovement movement;
    private SnakeCollision collision;
    private SnakeAudio audio;
    private SnakeVisuals visuals;

    void Awake()
    {
        movement  = GetComponent<SnakeMovement>();
        collision = GetComponent<SnakeCollision>();
        audio     = GetComponent<SnakeAudio>();
        visuals   = GetComponent<SnakeVisuals>();
    }

    void Start()
    {
        SpawnInitialSnake();
    }

    void Update()
    {
        HandleInput();
        CheckAppleAroundHead();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.W)) OnMoveUp();
        else if (Input.GetKeyDown(KeyCode.S)) OnMoveDown();
        else if (Input.GetKeyDown(KeyCode.A)) OnMoveLeft();
        else if (Input.GetKeyDown(KeyCode.D)) OnMoveRight();
    }

    // ============== INPUT (giữ API cũ) =================
    public void OnMoveUp()    => movement.TryStartMove(Vector2.up);
    public void OnMoveDown()  => movement.TryStartMove(Vector2.down);
    public void OnMoveLeft()  => movement.TryStartMove(Vector2.left);
    public void OnMoveRight() => movement.TryStartMove(Vector2.right);

    // ============== SPAWN BAN ĐẦU ======================
    void SpawnInitialSnake()
    {
        segments.Clear();
        Vector3 pos = transform.position;

        // Head
        Transform head = Instantiate(headPrefab, pos, Quaternion.identity, segmentsParent);
        segments.Add(head);

        headAnim = head.GetComponentInChildren<SnakeHeadAnimator>();
        headAnim?.PlayIdle();

        // Body
        Transform body = Instantiate(bodyPrefab, pos - new Vector3(stepDistance, 0f, 0f), Quaternion.identity, segmentsParent);
        segments.Add(body);

        // Tail
        Transform tail = Instantiate(tailPrefab, pos - new Vector3(stepDistance * 2f, 0f, 0f), Quaternion.identity, segmentsParent);
        segments.Add(tail);

        visuals.UpdateSegmentRotations();
    }

    // ============== CHECK APPLE XUNG QUANH HEAD =========
    void CheckAppleAroundHead()
    {
        if (segments.Count == 0 || headAnim == null) return;

        Vector3[] directions = new Vector3[]
        {
            Vector3.up,
            Vector3.down,
            Vector3.left,
            Vector3.right
        };

        foreach (var dir in directions)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                segments[0].position,
                dir,
                stepDistance * 0.9f,
                appleMask
            );

            if (hit.collider != null && hit.collider.CompareTag("Apple"))
            {
                headAnim.PlayIdleToEat();
                return;
            }
        }
    }
}
