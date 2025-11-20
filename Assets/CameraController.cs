using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Target Group")]
    public Transform targetGroup;

    [Header("Anchor Settings (0-1, like UI anchors)")]
    [Range(0f, 1f)] public float anchorMinX = 0f;
    [Range(0f, 1f)] public float anchorMinY = 0f;
    [Range(0f, 1f)] public float anchorMaxX = 1f;
    [Range(0f, 1f)] public float anchorMaxY = 1f;

    [Header("Padding (world units)")]
    public float padding = 0.5f;

    [Header("Auto Update")]
    public bool autoUpdateInEditor = true;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        UpdateCameraView(); // Delay đảm bảo mọi thứ đã spawn
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && autoUpdateInEditor)
        {
            UpdateCameraView();
        }
#endif
    }

    public void UpdateCameraView()
    {
        if (targetGroup == null || targetGroup.childCount == 0) return;

        Vector3 min = targetGroup.GetChild(0).position;
        Vector3 max = targetGroup.GetChild(0).position;

        for (int i = 1; i < targetGroup.childCount; i++)
        {
            Vector3 pos = targetGroup.GetChild(i).position;
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }

        // Áp dụng padding
        min -= Vector3.one * padding;
        max += Vector3.one * padding;

        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min;

        AdjustCamera(center, size);
    }

    private void AdjustCamera(Vector3 center, Vector3 size)
    {
        Vector2 anchorMin = new Vector2(anchorMinX, anchorMinY);
        Vector2 anchorMax = new Vector2(anchorMaxX, anchorMaxY);
        Vector2 anchorCenter = (anchorMin + anchorMax) * 0.5f;

        transform.position = new Vector3(center.x, center.y, transform.position.z);

        float width = size.x;
        float height = size.y;

        float screenAspect = (float)Screen.width / Screen.height;
        float viewWidthPercent = Mathf.Max(0.01f, anchorMax.x - anchorMin.x);
        float viewHeightPercent = Mathf.Max(0.01f, anchorMax.y - anchorMin.y);

        float scaleHeight = height / viewHeightPercent;
        float scaleWidth = (width / screenAspect) / viewWidthPercent;

        cam.orthographicSize = Mathf.Max(scaleHeight, scaleWidth) * 0.5f;

        float zDistance = Mathf.Abs(cam.transform.position.z - center.z);
        Vector3 screenCenter = cam.ViewportToWorldPoint(new Vector3(anchorCenter.x, anchorCenter.y, zDistance));
        Vector3 offset = center - screenCenter;
        transform.position += new Vector3(offset.x, offset.y, 0f);
    }
}
