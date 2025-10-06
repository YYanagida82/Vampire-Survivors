using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform target; // 注視対象
    public Vector3 offset; // 注視対象からのオフセット

    void Update()
    {
        transform.position = target.position + offset;
    }
}
