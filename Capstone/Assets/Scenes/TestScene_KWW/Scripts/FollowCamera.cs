using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform targetCamera; // ���� ī�޶� (Main Camera)

    void Update()
    {
        if (targetCamera != null)
        {
            // ī�޶� ��ġ�� ���󰡸� �ణ �տ� ��ġ
            transform.position = targetCamera.position + targetCamera.forward * 1f;
            transform.rotation = targetCamera.rotation; // ī�޶� ȸ���� ����ȭ
        }
    }
}