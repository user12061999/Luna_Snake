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
    private int skipGravityFrames = 0;
    Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
    }
    void Update()
    {
        if (skipGravityFrames > 0)
        {
            skipGravityFrames--;
            return;
        }

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
        Vector3 direction = Vector3.down;
        float halfExtent = GetColliderHalfExtent(direction);

        // điểm bắt đầu raycast: ngay dưới mép collider
        Vector3 origin = transform.position + direction * halfExtent;

        // ray chỉ cần kiểm tra 1 bước
        float rayLength = stepDistance;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, rayLength, groundMask | blockMask);

        Debug.DrawRay(origin, direction * rayLength, Color.red, 0.1f);

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
        Vector3 origin = transform.position + dir3*1.01f * halfExtent;
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

// ✅ Đợi 1 frame trước khi gravity kiểm tra lại
        if (dir == Vector2.up)
        {
            ScheduleFallAfter(0.08f + 0.01f); // rắn rơi trước
        }
    }
    float GetColliderHalfExtent(Vector3 direction)
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col is BoxCollider2D box)
        {
            Vector2 size = box.size;
            Vector2 scale = transform.lossyScale;
            Vector2 worldSize = new Vector2(size.x * scale.x, size.y * scale.y);

            direction = direction.normalized;

            // xác định hướng nào có extent
            float extent = 0.5f;
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                extent = worldSize.x / 2f;
            }
            else
            {
                extent = worldSize.y / 2f;
            }

            return extent + 0.01f; // cộng nhỏ tránh ray trúng chính collider
        }

        return 0.5f;
    }

    public void ScheduleFallAfter(float delaySeconds)
    {
        StartCoroutine(DelayAndFall(delaySeconds));
    }

    IEnumerator DelayAndFall(float delay)
    {
        yield return new WaitForSeconds(delay);

        while (!isMoving && !isFalling && ShouldFallOneUnit())
        {
            yield return StartCoroutine(FallOneUnit());
        }
    }
}
