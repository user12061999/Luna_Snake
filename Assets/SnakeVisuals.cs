using UnityEngine;

public class SnakeVisuals : MonoBehaviour
{
    private SnakeController controller;

    void Awake()
    {
        controller = GetComponent<SnakeController>();
    }

    public void UpdateSegmentRotations()
    {
        var segments = controller.segments;
        if (segments == null || segments.Count == 0) return;

        // HEAD: xoay theo moveDir
        if (controller.moveDir.sqrMagnitude > 0.0001f)
        {
            float headAngle = Mathf.Atan2(controller.moveDir.y, controller.moveDir.x) * Mathf.Rad2Deg;
            segments[0].rotation = Quaternion.Euler(0, 0, headAngle);
        }

        // TAIL: xoay theo hướng từ segment trước -> tail
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

        // BODY: update sprite theo hướng BÒ (tail -> head)
        for (int i = 1; i < segments.Count - 1; i++)
        {
            Transform seg = segments[i];
            var renderer = seg.GetComponent<SnakeSegmentRenderer>();
            if (renderer == null) continue;

            // vector từ segment hiện tại -> segment phía HEAD
            Vector2 toHead = (Vector2)(segments[i - 1].position - seg.position);
            // vector từ segment hiện tại -> segment phía TAIL
            Vector2 toTail = (Vector2)(segments[i + 1].position - seg.position);

            renderer.UpdateSprite(toHead, toTail);
        }
    }


    void OnDrawGizmosSelected()
    {
        if (controller == null || controller.segments == null || controller.segments.Count == 0)
        {
            controller = GetComponent<SnakeController>();
            if (controller == null || controller.segments == null || controller.segments.Count == 0) return;
        }

        Gizmos.color = Color.red;
        float r = controller.stepDistance * 0.45f;
        foreach (var seg in controller.segments)
        {
            if (seg != null)
                Gizmos.DrawWireSphere(seg.position, r);
        }
    }
}
