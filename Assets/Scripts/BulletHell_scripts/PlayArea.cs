using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PlayArea : MonoBehaviour
{
    public static PlayArea Instance { get; private set; }

    [SerializeField] private bool square = true; // true: height always matches width
    [SerializeField] private Vector2 areaSize = new Vector2(30f, 30f); // width (x) and height (y) in world units
    [SerializeField] private Color barColor = Color.black; // color of the bars outside the play area

    // The size actually used, with the Square option applied.
    public Vector2 Size => square ? new Vector2(areaSize.x, areaSize.x) : areaSize;
    public Vector2 Center => transform.position; // the area is centered on the camera
    public Vector2 Min => Center - Size / 2f;
    public Vector2 Max => Center + Size / 2f;

    private Camera cam;
    private Camera barsCam;
    private int lastWidth;
    private int lastHeight;
    private Vector2 lastSize;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        CreateBarsCamera();
        ApplyViewport();
    }

    // The size of whatever the camera draws to: the screen normally, or a texture inside the visual novel.
    private int TargetWidth => cam.targetTexture != null ? cam.targetTexture.width : Screen.width;
    private int TargetHeight => cam.targetTexture != null ? cam.targetTexture.height : Screen.height;

    void Update()
    {
        // aligns if the window is resized or the size is changed in the inspector during play mode.
        if (TargetWidth != lastWidth || TargetHeight != lastHeight || Size != lastSize)
            ApplyViewport();
    }

    // Used by AmoraBattle: draw the arena into a texture instead of the screen.
    // The texture is already the arena's shape, so the black bars aren't needed.
    public void RenderToTexture(RenderTexture texture)
    {
        cam.targetTexture = texture;
        if (barsCam != null)
            barsCam.gameObject.SetActive(texture == null);
        ApplyViewport();
    }

    private void CreateBarsCamera()
    {
        GameObject barsObject = new GameObject("PlayArea Bars Camera");
        barsObject.transform.SetParent(transform, false);

        barsCam = barsObject.AddComponent<Camera>();
        barsCam.orthographic = true;
        barsCam.clearFlags = CameraClearFlags.SolidColor;
        barsCam.backgroundColor = barColor;
        barsCam.cullingMask = 0; // render no objects, just the background color
        barsCam.depth = cam.depth - 1; // lower depth/priority = drawn first, underneath
        barsCam.rect = new Rect(0f, 0f, 1f, 1f); // always the full screen
    }

    private void ApplyViewport()
    {
        lastWidth = TargetWidth;
        lastHeight = TargetHeight;
        lastSize = Size;

        Vector2 size = Size;
        if (size.x <= 0f || size.y <= 0f) return; // ignore invalid sizes while typing in the Inspector

        float screenAspect = (float)TargetWidth / TargetHeight;
        float areaAspect = size.x / size.y; // 1 = square, above 1 = wide, below 1 = tall

        if (screenAspect >= areaAspect)
        {
            // screen is wider than the area. (full height) bars on the left and right.
            float width = areaAspect / screenAspect;
            cam.rect = new Rect((1f - width) / 2f, 0f, width, 1f);
        }
        else
        {
            // screen is taller than the area. (full width) bars on the top and bottom.
            float height = screenAspect / areaAspect;
            cam.rect = new Rect(0f, (1f - height) / 2f, 1f, height);
        }

        cam.orthographicSize = size.y / 2f;
    }

    // keeps a point inside the area, 'padding' units away from the edges.
    public Vector2 Clamp(Vector2 point, float padding)
    {
        return new Vector2(
            Mathf.Clamp(point.x, Min.x + padding, Max.x - padding),
            Mathf.Clamp(point.y, Min.y + padding, Max.y - padding));
    }

    // true if a point is inside the area, allowing 'margin' extra units past each edge.
    public bool Contains(Vector2 point, float margin = 0f)
    {
        return point.x >= Min.x - margin && point.x <= Max.x + margin
            && point.y >= Min.y - margin && point.y <= Max.y + margin;
    }

    // draws the area in the scene view so you can see the boundary while building levels.
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector2 size = Size;
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
    }
}