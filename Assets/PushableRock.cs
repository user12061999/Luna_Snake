using System.Collections;
using UnityEngine;

public class PushableRock : MonoBehaviour
{
    public float stepDistance = 1f;
    public float fallDuration = 0.08f;
    public LayerMask supportMask;

    private bool isBusy = false;

    public void TryApplyGravity()
    {
        if (isBusy) return;
        if (ShouldFall())
        {
            StartCoroutine(FallContinuously());
        }
    }

    public bool TryPush(Vector2 direction)
    {
        if (isBusy) return false;

        Vector3 dir3 = direction.normalized;
        Vector3 targetPos = transform.position + dir3 * stepDistance;

        if (IsPositionBlocked(targetPos))
            return false;

        StartCoroutine(PushRoutine(dir3));
        return true;
    }

    IEnumerator PushRoutine(Vector3 dir)
    {
        isBusy = true;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + dir * stepDistance;

        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPos, t / 0.12f);
            yield return null;
        }

        transform.position = targetPos;
        isBusy = false;
    }

    IEnumerator FallContinuously()
    {
        isBusy = true;
        while (ShouldFall())
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + Vector3.down * stepDistance;

            float t = 0f;
            while (t < fallDuration)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / fallDuration);
                transform.position = Vector3.Lerp(startPos, targetPos, a);
                yield return null;
            }

            transform.position = targetPos;
            yield return null;
        }
        isBusy = false;
    }

    bool ShouldFall()
    {
        Vector2 rayOrigin = GetBottomPoint();
        rayOrigin += Vector2.down * 0.01f; // 👈 Dịch ra ngoài collider

        float rayLength = stepDistance * 0.9f;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, rayLength, supportMask);

        Debug.DrawRay(rayOrigin, Vector2.down * rayLength, hit ? Color.green : Color.red, 0.5f);

        return !hit;
    }

    bool IsPositionBlocked(Vector3 position)
    {
        // Dùng OverlapCircle tại vị trí đích
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, 0.45f, supportMask);
        foreach (var hit in hits)
        {
            if (hit != null && hit.transform != transform)
                return true;
        }
        return false;
    }

    Vector2 GetBottomPoint()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Vector2 localBottom = new Vector2(box.offset.x, box.offset.y - box.size.y * 0.5f);
            return transform.TransformPoint(localBottom);
        }

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            Vector2 localBottom = circle.offset - Vector2.up * circle.radius;
            return transform.TransformPoint(localBottom);
        }

        // Fallback: giả sử rock cao 1 unit
        return (Vector2)transform.position - Vector2.up * 0.5f;
    }
}