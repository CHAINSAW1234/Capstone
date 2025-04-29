using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HingeJoint))]
public class AutoDoorSetup : MonoBehaviour
{
    [Header("문 설정")]
    public bool hingeOnRight = true;  // 오른쪽에 경첩이 있으면 true, 왼쪽이면 false
    public float anchorOffset = 0.5f; // 문 중심에서 경첩까지 거리

    private void Reset()
    {
        SetupDoor();
    }

    private void Awake()
    {
        SetupDoor();
    }

    void SetupDoor()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        HingeJoint hinge = GetComponent<HingeJoint>();

        // Rigidbody 세팅
        rb.mass = 10f;
        rb.angularDamping = 5f;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // HingeJoint 세팅
        hinge.axis = new Vector3(0, 1, 0); // Y축으로 회전
        hinge.autoConfigureConnectedAnchor = true;

        // Anchor 위치를 문 오른쪽/왼쪽으로 설정
        float anchorX = hingeOnRight ? anchorOffset : -anchorOffset;
        hinge.anchor = new Vector3(anchorX, 0, 0);

        hinge.useLimits = true;
        JointLimits limits = new JointLimits();
        limits.min = -90f;
        limits.max = 0f;
        limits.bounciness = 0f;
        limits.bounceMinVelocity = 0.2f;
        limits.contactDistance = 0f;
        hinge.limits = limits;

        Debug.Log("✅ 문 자동 세팅 완료: " + gameObject.name);
    }
}
