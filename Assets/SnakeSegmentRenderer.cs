using UnityEngine;

public class SnakeSegmentRenderer : MonoBehaviour
{
    [Header("Body Sprites")]
    public Sprite bodySprite;               // default (thẳng)
    public Sprite bodyTurnCW;              // rẽ theo chiều kim đồng hồ
    public Sprite bodyTurnCCW;             // rẽ ngược chiều kim đồng hồ
    [Tooltip("Góc xoay thêm khi rẽ ngược chiều kim đồng hồ (CCW)")]
    public float ccwExtraRotation = 0f;


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

        // nếu 2 hướng gần như đối nhau -> đi thẳng
        float dot = Vector2.Dot(toHead.normalized, -toTail.normalized);
        float angleBetween = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;

        bool isStraight = Mathf.Abs(angleBetween) < 10f; // gần như 180 độ

        if (isStraight)
        {
            rend.sprite = bodySprite;
            Vector2 dir = toHead != Vector2.zero ? toHead.normalized : (-toTail).normalized;
            float angle = GetAngleFromDirection(dir);
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        else
        {
            // góc rẽ
            Vector2 dirToHead = toHead.normalized;
            Vector2 dirToTail = toTail.normalized;

            // xác định chiều rẽ
            float cross = dirToTail.x * dirToHead.y - dirToTail.y * dirToHead.x;
            Vector2 dir = dirToHead;
            float angle = GetAngleFromDirection(dir);
            if (cross > 0 && bodyTurnCCW != null)
            {
                rend.sprite = bodyTurnCCW;
                 angle = GetAngleFromDirection(dirToHead) + ccwExtraRotation;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            else if (cross < 0 && bodyTurnCW != null)
            {
                rend.sprite = bodyTurnCW;
                 angle = GetAngleFromDirection(dirToHead);
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                rend.sprite = bodySprite;
                 angle = GetAngleFromDirection(dirToHead);
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            // dùng hướng từ tail -> head để xoay
            
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
    float GetAngleFromDirection(Vector2 dir) { if (dir == Vector2.zero) return angleOffset; float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg; return angle + angleOffset; }
}