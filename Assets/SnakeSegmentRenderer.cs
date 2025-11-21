using UnityEngine;

public class SnakeSegmentRenderer : MonoBehaviour
{
    [Header("Body Sprite (only 1)")]
    public Sprite bodySprite;   // sprite thân, vẽ mặc định sang PHẢI hoặc TRÁI

    [Tooltip("Nếu sprite gốc vẽ sang TRÁI thì set = 180, vẽ sang LÊN thì = 90, vẽ sang XUỐNG thì = -90.")]
    public float angleOffset = 0f;

    private SpriteRenderer rend;

    void Awake()
    {
        rend = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// toHead: vector từ segment hiện tại -> segment phía HEAD
    /// toTail: vector từ segment hiện tại -> segment phía TAIL
    /// </summary>
    public void UpdateSprite(Vector2 toHead, Vector2 toTail)
    {
        if (rend == null || bodySprite == null) return;

        rend.sprite = bodySprite;

        // luôn ưu tiên hướng BÒ (tail -> head)
        Vector2 dir = toHead != Vector2.zero ? toHead.normalized
            : (-toTail).normalized;

        float angle = GetAngleFromDirection(dir);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    float GetAngleFromDirection(Vector2 dir)
    {
        if (dir == Vector2.zero)
            return angleOffset;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        return angle + angleOffset;
    }
}