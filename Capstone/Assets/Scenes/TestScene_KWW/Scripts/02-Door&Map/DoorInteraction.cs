using UnityEngine;


[RequireComponent(typeof(Rigidbody), typeof(HingeJoint))]
public class DoorSetup : MonoBehaviour
{
    [Header("Assign Door Handles in Inspector")]
    public Collider doorHandle1;
    public Collider doorHandle2;

    private Rigidbody rb;
    private HingeJoint hingeJoint;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    void Reset()
    {
        SetupDoor();
    }

    void Awake()
    {
        SetupDoor();
    }

    private void SetupDoor()
    {
        // 🔹 Rigidbody 설정
        rb = GetComponent<Rigidbody>();
        rb.mass = 1f;
        rb.angularDamping = 0.05f;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // 🔹 HingeJoint 설정
        hingeJoint = GetComponent<HingeJoint>();
        hingeJoint.anchor = new Vector3(0, 1, 0.4f);
        hingeJoint.axis = new Vector3(0, 1, 0);
        hingeJoint.useLimits = true;

        // Hinge Joint Limits 설정
        JointLimits limits = hingeJoint.limits;
        limits.min = -120f;
        limits.max = 0f;
        limits.bounciness = 0f;
        limits.bounceMinVelocity = 0.2f;
        limits.contactDistance = 0f;
        hingeJoint.limits = limits;

        Debug.Log("✅ Door hinge and Rigidbody configured.");

        // 🔹 XR Grab Interactable 추가 및 설정
        grabInteractable = gameObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }

        // 🔹 할당된 손잡이 Collider를 XR Grab Interactable의 Colliders 리스트에 추가
        grabInteractable.colliders.Clear();
        if (doorHandle1 != null) grabInteractable.colliders.Add(doorHandle1);
        if (doorHandle2 != null) grabInteractable.colliders.Add(doorHandle2);

        // XR Grab Interactable 속성 설정
        grabInteractable.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.VelocityTracking;
        grabInteractable.trackPosition = true;
        grabInteractable.trackRotation = true;
        grabInteractable.throwOnDetach = true;

        Debug.Log("✅ Door is now interactable via XR Grab Interactable.");
    }
}
