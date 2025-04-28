using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform targetCamera; // 따라갈 카메라 (Main Camera)
    public float targetDistance = 0.5f; // 자막 텍스트 UI
    void Update()
    {
        if (targetCamera != null)
        {
            // 카메라 위치를 따라가며 약간 앞에 위치
            transform.position = targetCamera.position + targetCamera.forward * targetDistance;
            transform.rotation = targetCamera.rotation; // 카메라 회전과 동기화
        }
    }
}