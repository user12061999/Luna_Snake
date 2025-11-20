using UnityEngine;

public class SnakeSegmentRenderer : MonoBehaviour
{
    [Header("Sprite Variants")]
    public SnakeSegmentSprites sprites;

    private SpriteRenderer rend;
    private Transform cachedTransform;

    void Awake()
    {
        rend = GetComponent<SpriteRenderer>();
        cachedTransform = transform;
    }

    public void UpdateSprite(Vector2 dirFrom, Vector2 dirTo)
    {
        if (rend == null || sprites == null) return;

        SegmentSpriteVariant variant = null;

        if (IsStraight(dirFrom, dirTo))
        {
            if (Mathf.Abs(dirFrom.x) > 0.1f)
                variant = sprites.straightHorizontal;
            else
                variant = sprites.straightVertical;
        }
        else
        {
            variant = GetCurveVariant(dirFrom, dirTo);
        }

        if (variant != null)
        {
            rend.sprite = GetSpriteFromVariant(variant, cachedTransform.eulerAngles.z);
        }
    }

    bool IsStraight(Vector2 a, Vector2 b)
    {
        return (a == b || a == -b);
    }

    SegmentSpriteVariant GetCurveVariant(Vector2 from, Vector2 to)
    {
        from = from.normalized;
        to = to.normalized;

        if ((from == Vector2.left && to == Vector2.up) || (from == Vector2.down && to == Vector2.right))
            return sprites.curveBL;

        if ((from == Vector2.right && to == Vector2.up) || (from == Vector2.down && to == Vector2.left))
            return sprites.curveBR;

        if ((from == Vector2.left && to == Vector2.down) || (from == Vector2.up && to == Vector2.right))
            return sprites.curveTL;

        if ((from == Vector2.right && to == Vector2.down) || (from == Vector2.up && to == Vector2.left))
            return sprites.curveTR;

        return null;
    }

    Sprite GetSpriteFromVariant(SegmentSpriteVariant variant, float rotationZ)
    {
        float z = rotationZ % 360f;
        if (z < 0f) z += 360f;
        bool lightOnTop = (z >= 0f && z < 180f);
        return lightOnTop ? variant.lightOnTop : variant.darkOnTop;
    }
}

[System.Serializable]
public class SnakeSegmentSprites
{
    public SegmentSpriteVariant straightHorizontal;
    public SegmentSpriteVariant straightVertical;
    public SegmentSpriteVariant curveTR;
    public SegmentSpriteVariant curveTL;
    public SegmentSpriteVariant curveBR;
    public SegmentSpriteVariant curveBL;
}

[System.Serializable]
public class SegmentSpriteVariant
{
    public Sprite lightOnTop;
    public Sprite darkOnTop;
}
