using UnityEngine;

namespace FireEvacuation
{
    public class DoorPower : MonoBehaviour
    {
        [Header("문 설정")]
        public GameObject doorObject; // 문 오브젝트
        public float pushForce = 10f; // 문을 밀 때 적용되는 힘
        public float triggerDistance = 0.3f; // 손 감지 트리거 거리
        public bool isDoorEnabled = false; // 문 상호작용 활성화 여부

        private Rigidbody doorRb;
        private HingeJoint hingeJoint;
        private BoxCollider triggerCollider;
        private bool isOpening = false;

        private void Awake()
        {
            SetupDoor();
        }

        void SetupDoor()
        {
            if (doorObject == null)
            {
                Debug.LogError("문 오브젝트가 지정되지 않았습니다!");
                return;
            }

            // Rigidbody 설정
            doorRb = doorObject.GetComponent<Rigidbody>();
            if (doorRb == null)
            {
                doorRb = doorObject.AddComponent<Rigidbody>();
            }
            doorRb.mass = 1f;
            doorRb.angularDamping = 0.05f;
            doorRb.useGravity = true;
            doorRb.isKinematic = false;
            doorRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // HingeJoint 설정
            hingeJoint = doorObject.GetComponent<HingeJoint>();
            if (hingeJoint == null)
            {
                hingeJoint = doorObject.AddComponent<HingeJoint>();
            }
            hingeJoint.anchor = new Vector3(0, 1, 0.4f);
            hingeJoint.axis = new Vector3(0, 1, 0);
            hingeJoint.useLimits = true;
            JointLimits limits = hingeJoint.limits;
            limits.min = -120f;
            limits.max = 0f;
            limits.bounciness = 0f;
            limits.bounceMinVelocity = 0.2f;
            limits.contactDistance = 0f;
            hingeJoint.limits = limits;

            // 트리거 콜라이더 설정 (손 감지용)
            triggerCollider = doorObject.GetComponent<BoxCollider>();
            if (triggerCollider == null)
            {
                triggerCollider = doorObject.AddComponent<BoxCollider>();
            }
            triggerCollider.size = new Vector3(0.5f, 1f, triggerDistance); // 트리거 범위 조정
            triggerCollider.center = new Vector3(0, 0, 0.2f); // 문 표면 근처로 이동
            triggerCollider.isTrigger = true;

            Debug.Log("✅ 문 설정 완료.");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isDoorEnabled || isOpening)
            {
                return;
            }

            // 손이 트리거에 들어왔는지 확인 (손은 "Hand" 태그로 가정)
            if (other.CompareTag("Hand"))
            {
                OpenDoor();
            }
        }

        void OpenDoor()
        {
            if (doorRb != null && !isOpening)
            {
                isOpening = true;
                // 문을 미는 방향으로 힘 적용 (HingeJoint의 축을 기준으로 회전)
                Vector3 pushDirection = -doorObject.transform.right; // 문을 안쪽으로 밀기
                doorRb.AddForceAtPosition(pushForce * pushDirection, doorObject.transform.position, ForceMode.Impulse);
                Debug.Log("✅ 문이 손으로 밀려 열림.");
            }
        }

        public void EnableDoorInteraction()
        {
            isDoorEnabled = true;
            Debug.Log("✅ 문 상호작용 활성화.");
        }

        public void DisableDoorInteraction()
        {
            isDoorEnabled = false;
            Debug.Log("✅ 문 상호작용 비활성화.");
        }

        private void OnDestroy()
        {
            // 트리거 콜라이더 정리
            if (triggerCollider != null)
            {
                Destroy(triggerCollider);
            }
        }
    }
}