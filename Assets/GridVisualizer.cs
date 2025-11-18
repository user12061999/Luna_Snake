using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class GridVisualizer : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1f;
    public Vector3 origin = Vector3.zero;

    [Header("Gizmos")]
    public Color gridColor = Color.gray;
    public Color originColor = Color.yellow;

    [Header("Labels")]
    public bool drawLabels = true;
    public int labelFontSize = 10;
    public Color labelColor = Color.white;

#if UNITY_EDITOR
    private GUIStyle _labelStyle;
#endif

    private void OnDrawGizmos()
    {
        Gizmos.color = gridColor;

#if UNITY_EDITOR
        // chuẩn bị style cho text
        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(EditorStyles.label);
            _labelStyle.alignment = TextAnchor.MiddleCenter; // căn giữa
        }
        _labelStyle.fontSize = labelFontSize;
        _labelStyle.normal.textColor = labelColor;
#endif

        // Vẽ từng ô
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 cellCenter = GetCellCenterWorld(x, y);
                Vector3 halfSize = Vector3.one * (cellSize * 0.5f);

                // Vẽ hình vuông
                Gizmos.DrawWireCube(cellCenter, halfSize * 2f);

#if UNITY_EDITOR
                if (drawLabels)
                {
                    Handles.Label(cellCenter, $"({x},{y})", _labelStyle);
                }
#endif
            }
        }

        // Vẽ origin
        Gizmos.color = originColor;
        Gizmos.DrawSphere(origin, cellSize * 0.15f);

#if UNITY_EDITOR
        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(EditorStyles.label);
            _labelStyle.alignment = TextAnchor.MiddleCenter;
        }
        _labelStyle.fontSize = labelFontSize;
        _labelStyle.normal.textColor = labelColor;
        Handles.Label(origin + Vector3.up * (cellSize * 0.5f), "Origin (0,0)", _labelStyle);
#endif
    }

    public Vector3 GetCellCenterWorld(int x, int y)
    {
        return origin + new Vector3(
            x * cellSize + cellSize * 0.5f,
            y * cellSize + cellSize * 0.5f,
            0f
        );
    }
}
