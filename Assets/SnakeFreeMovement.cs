using System.Collections.Generic;
using UnityEngine;

public class SnakeFreeMovement : MonoBehaviour
{
    [Header("Refs")]
    public Transform segmentsParent;
    public GameObject bodyPrefab;

    [Header("Move Settings")]
    public float stepDistance = 1f;
    public LayerMask wallMask;

    // Danh sách segment (0 = head)
    private List<Transform> segments = new List<Transform>();
    // Danh sách vị trí slot
    private List<Vector3> slots = new List<Vector3>();

    void Start()
    {
        segments.Clear();
        slots.Clear();

        // head luôn là index 0
        segments.Add(transform);
        slots.Add(transform.position);

        // tạo 2 body phía sau
        for (int i = 0; i < 2; i++)
        {
            Vector3 pos = transform.position - new Vector3(stepDistance * (i + 1), 0, 0);
            AddSegmentAtEnd(pos);
        }

        ApplySlotsToSegments();
    }

    void Update()
    {
        Vector2 inputDir = Vector2.zero;

        if (Input.GetKeyDown(KeyCode.W)) inputDir = Vector2.up;
        else if (Input.GetKeyDown(KeyCode.S)) inputDir = Vector2.down;
        else if (Input.GetKeyDown(KeyCode.A)) inputDir = Vector2.left;
        else if (Input.GetKeyDown(KeyCode.D)) inputDir = Vector2.right;

        if (inputDir != Vector2.zero)
            TryStep(inputDir);
    }

    void TryStep(Vector2 dir)
    {
        dir.Normalize();
        Vector3 dir3 = new Vector3(dir.x, dir.y, 0);

        // Block tường
        if (Physics2D.Raycast(transform.position, dir3, stepDistance, wallMask))
            return;

        bool grewThisStep = false;

        // Check apple bằng raycast *không mask*
        RaycastHit2D appleHit = Physics2D.Raycast(
            transform.position + dir3 * 0.01f,
            dir3,
            stepDistance
        );

        if (appleHit.collider != null && appleHit.collider.CompareTag("Apple"))
        {
            Destroy(appleHit.collider.gameObject);
            GrowBehindHead();     // tăng độ dài NGAY LẬP TỨC
            grewThisStep = true;
        }

        // Tính vị trí head mới
        Vector3 newHeadPos = transform.position + dir3 * stepDistance;

        // Chèn vị trí head mới
        slots.Insert(0, newHeadPos);

        // Nếu KHÔNG grow → dời tail
        if (!grewThisStep)
            slots.RemoveAt(slots.Count - 1);

        ApplySlotsToSegments();
    }

    private void ApplySlotsToSegments()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            segments[i].position = slots[i];
        }
    }

    private void AddSegmentAtEnd(Vector3 pos)
    {
        GameObject obj = Instantiate(bodyPrefab, pos, Quaternion.identity, segmentsParent);
        Transform t = obj.transform;
        segments.Add(t);
        slots.Add(pos);
    }

    private void GrowBehindHead()
    {
        Vector3 headPos = slots[0];

        // THÊM SLOT NGAY SAU HEAD
        slots.Insert(1, headPos);

        // THÊM BODY NGAY SAU HEAD
        GameObject segObj = Instantiate(bodyPrefab, headPos, Quaternion.identity, segmentsParent);
        Transform segT = segObj.transform;

        // chèn vào segments
        segments.Insert(1, segT);

        ApplySlotsToSegments();
    }
}
