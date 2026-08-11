using UnityEngine;

/// <summary>
/// 相机平滑跟随目标（玩家）
/// 挂到 Main Camera 上
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    [SerializeField] private Transform target;

    [Header("跟随参数")]
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

    [Header("地图边界")]
    [SerializeField] private bool clampToBounds = true;
    [SerializeField] private Vector2 mapCenter = Vector2.zero;
    [SerializeField] private Vector2 mapHalfSize = new Vector2(800, 928); // 50×58 tiles × 32px = 1600×1856

    private Vector3 _velocity = Vector3.zero;

    void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;
        Vector3 smoothedPos = Vector3.SmoothDamp(transform.position, desiredPos, ref _velocity, smoothSpeed);

        if (clampToBounds)
        {
            // 获取相机半宽高
            Camera cam = GetComponent<Camera>();
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            smoothedPos.x = Mathf.Clamp(smoothedPos.x, mapCenter.x - mapHalfSize.x + halfWidth, mapCenter.x + mapHalfSize.x - halfWidth);
            smoothedPos.y = Mathf.Clamp(smoothedPos.y, mapCenter.y - mapHalfSize.y + halfHeight, mapCenter.y + mapHalfSize.y - halfHeight);
        }

        transform.position = smoothedPos;
    }

    /// <summary>
    /// 立即跳转到目标位置（场景切换/传送用）
    /// </summary>
    public void SnapToTarget()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
}
