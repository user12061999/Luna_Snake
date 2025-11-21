using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushableRock : MonoBehaviour
{
    public float fallDuration = 0.08f;
    public float stepDistance = 1f;
    public LayerMask groundMask;
    public LayerMask blockMask; // walls, segments, other rocks...

    private bool isFalling = false;
    private bool isMoving = false;

    void Update()
    {
        if (!isMoving && !isFalling)
        {
            if (ShouldFallOneUnit())
            {
                StartCoroutine(FallOneUnit());
            }
        }
    }

    bool ShouldFallOneUnit()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, stepDistance * 0.9f, groundMask | blockMask);
        return hit.collider == null;
    }

    IEnumerator FallOneUnit()
    {
        isFalling = true;

        Vector3 start = transform.position;
        Vector3 target = start + Vector3.down * stepDistance;

        float t = 0f;
        while (t < fallDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / fallDuration);
            transform.position = Vector3.Lerp(start, target, a);
            yield return null;
        }

        transform.position = target;
        isFalling = false;
    }

    public IEnumerator TryPush(Vector2 dir)
    {
        if (isMoving || isFalling) yield break;

        Vector3 dir3 = new Vector3(dir.x, dir.y, 0f);

        // ✅ Tính offset raycast dựa trên collider thực tế
        float halfExtent = GetColliderHalfExtent(dir3);

        // Điểm xuất phát raycast: lệch ra ngoài collider 1 chút theo hướng đẩy
        Vector3 origin = transform.position + dir3 * halfExtent;
        float distance = stepDistance - halfExtent;

        RaycastHit2D hit = Physics2D.Raycast(origin, dir3, distance, blockMask);

        Debug.DrawRay(origin, dir3 * distance, Color.yellow, 0.1f);

        if (hit.collider != null)
        {
            Debug.Log("Rock bị chặn bởi: " + hit.collider.name);
            yield break;
        }

        isMoving = true;

        Vector3 start = transform.position;
        Vector3 target = start + dir3 * stepDistance;

        float t = 0f;
        while (t < fallDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / fallDuration);
            transform.position = Vector3.Lerp(start, target, a);
            yield return null;
        }

        transform.position = target;
        isMoving = false;
    }
    float GetColliderHalfExtent(Vector3 direction)
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col is BoxCollider2D box)
        {
            Vector2 size = box.size;
            Vector2 scale = transform.lossyScale;

            // Scale kích thước theo transform (phòng trường hợp object bị scale)
            Vector2 worldSize = new Vector2(size.x * scale.x, size.y * scale.y);

            // Trả về half-extent theo hướng đẩy
            if (Mathf.Abs(direction.x) > 0.1f)
                return worldSize.x / 2f + 0.01f;
            else
                return worldSize.y / 2f + 0.01f;
        }

        // fallback mặc định
        return 0.5f;
    }

}
