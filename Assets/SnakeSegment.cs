using UnityEngine;

public enum SnakeSegmentType
{
    Head,
    Body,
    Tail
}

public class SnakeSegment : MonoBehaviour
{
    public SnakeSegmentType type;

    // Optional: để thay sprite theo loại
    public SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }
}