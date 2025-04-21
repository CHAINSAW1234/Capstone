using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace FireEvacuation
{
#pragma warning disable 0618 // Suppress obsolete warning for XRBaseController

    public class Section2 : MonoBehaviour
    {
        [Header("UI 설정")]
        public TMP_Text subtitleText;
        public float textDelay = 5f;

        [Header("후처리 효과")]
        public Volume globalVolume;
        private Vignette vignette;

        [Header("문 설정")]
        public GameObject doorObject; // 문 오브젝트
        public Collider doorHandle1; // 문 손잡이 1
        public Collider doorHandle2; // 문 손잡이 2

        [Header("트리거 설정")]
        public GameObject frontDoorTrigger; // 문 앞 트리거
        public GameObject backDoorTrigger; // 문 반대편 트리거

        [Header("안내도 설정")]
        public GameObject evacuationMap; // 탈출 경로 안내도 오브젝트

        [Header("버튼 설정")]
        public GameObject fireAlarmButton; // 화재 경보 버튼 오브젝트
        public Transform buttonTriggerTransform; // 버튼 트리거 Transform
        public float pressDistance = 0.05f; // 버튼 눌림 거리
        public float returnSpeed = 5f; // 버튼 복귀 속도

        [Header("Player Head Transform")]
        public Transform headTransform; // Reference to the player's head Transform (e.g., XR camera or head rig)

        [Header("사운드 설정")]
        public bool playSoundOnButtonPress = true; // 버튼 눌림 시 사운드 재생 여부
        public int soundGroupIndex = 0; // 재생할 SoundGroup 인덱스
        public int soundClipIndex = 0; // 재생할 Clip 인덱스
        public bool loopSound = false; // 사운드 루프 여부

        // 문 관련 변수
        private Rigidbody doorRb;
        private HingeJoint hingeJoint;
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable doorGrabInteractable;

        // 버튼 관련 변수
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable buttonInteractable;
        private Rigidbody buttonRb;
        private BoxCollider buttonCollider;
        private BoxCollider buttonTriggerCollider;
        private Vector3 buttonInitialPosition;
        private bool isButtonPressed = false;
        private bool isButtonTriggerActivated = false;

        // 트리거 상태 변수
        private bool hasReachedFrontDoorTrigger = false;
        private bool hasReachedBackDoorTrigger = false;
        private bool hasHighlightedMap = false;

        // UnityEvent로 버튼 눌림 이벤트 처리
        [SerializeField] public UnityEvent onFireAlarmActivated;

        private void Start()
        {
            // 문 초기화
            SetupDoor();

            // 버튼 초기화
            SetupButton();

            // 후처리 초기화
            InitPostProcessing();

            // 트리거 초기화
            SetupTriggers();

            // Find the player's head Transform if not assigned
            if (headTransform == null)
            {
                headTransform = Camera.main?.transform; // Fallback to Main Camera (common in XR setups)
                if (headTransform == null)
                {
                    Debug.LogError("Head Transform (Main Camera) not found! Please assign it manually in the Inspector.");
                }
                else
                {
                    Debug.Log("Head Transform assigned: " + headTransform.name);
                }
            }
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

            // XR Grab Interactable 설정
            doorGrabInteractable = doorObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (doorGrabInteractable == null)
            {
                doorGrabInteractable = doorObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            }
            doorGrabInteractable.colliders.Clear();
            if (doorHandle1 != null) doorGrabInteractable.colliders.Add(doorHandle1);
            if (doorHandle2 != null) doorGrabInteractable.colliders.Add(doorHandle2);
            doorGrabInteractable.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.VelocityTracking;
            doorGrabInteractable.trackPosition = true;
            doorGrabInteractable.trackRotation = true;
            doorGrabInteractable.throwOnDetach = true;

            Debug.Log("✅ 문 설정 완료.");
        }

        void SetupButton()
        {
            if (fireAlarmButton == null)
            {
                Debug.LogError("화재 경보 버튼 오브젝트가 지정되지 않았습니다!");
                return;
            }

            // XRSimpleInteractable 설정
            buttonInteractable = fireAlarmButton.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            if (buttonInteractable == null)
            {
                buttonInteractable = fireAlarmButton.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            }

            // Rigidbody 설정
            buttonRb = fireAlarmButton.GetComponent<Rigidbody>();
            if (buttonRb == null)
            {
                buttonRb = fireAlarmButton.AddComponent<Rigidbody>();
            }
            buttonRb.isKinematic = true;
            buttonRb.useGravity = false;

            // BoxCollider 설정
            buttonCollider = fireAlarmButton.GetComponent<BoxCollider>();
            if (buttonCollider == null)
            {
                buttonCollider = fireAlarmButton.AddComponent<BoxCollider>();
            }

            // Trigger 설정
            if (buttonTriggerTransform != null)
            {
                buttonTriggerCollider = buttonTriggerTransform.GetComponent<BoxCollider>();
                if (buttonTriggerCollider == null)
                {
                    buttonTriggerCollider = buttonTriggerTransform.gameObject.AddComponent<BoxCollider>();
                }
                buttonTriggerCollider.isTrigger = true;
            }
            else
            {
                Debug.LogWarning("버튼 트리거 Transform이 지정되지 않았습니다!");
            }

            // 버튼 초기 위치 저장
            buttonInitialPosition = fireAlarmButton.transform.localPosition;

            // 이벤트 설정
            buttonInteractable.hoverEntered.AddListener(OnButtonHoverEnter);
            buttonInteractable.hoverExited.AddListener(OnButtonHoverExit);

            Debug.Log("✅ 화재 경보 버튼 설정 완료.");
        }

        void InitPostProcessing()
        {
            if (globalVolume != null && globalVolume.profile.TryGet(out vignette))
            {
                vignette.active = true;
                vignette.intensity.Override(0.5f);
            }
            else
            {
                Debug.LogError("Global Volume이 지정되지 않았거나 Vignette 설정을 찾을 수 없습니다!");
            }
        }

        void SetupTriggers()
        {
            if (frontDoorTrigger == null || backDoorTrigger == null)
            {
                Debug.LogError("문 앞/뒤 트리거가 지정되지 않았습니다!");
                return;
            }

            if (evacuationMap == null)
            {
                Debug.LogError("탈출 경로 안내도 오브젝트가 지정되지 않았습니다!");
                return;
            }
        }

        void OnButtonHoverEnter(HoverEnterEventArgs args)
        {
            isButtonPressed = true;
        }

        void OnButtonHoverExit(HoverExitEventArgs args)
        {
            isButtonPressed = false;
            isButtonTriggerActivated = false;
        }

        void Update()
        {
            // 버튼 위치 업데이트
            if (isButtonPressed)
            {
                Vector3 targetPosition = buttonInitialPosition + Vector3.down * pressDistance;
                fireAlarmButton.transform.localPosition = Vector3.Lerp(fireAlarmButton.transform.localPosition, targetPosition, Time.deltaTime * returnSpeed);
                CheckButtonTriggerCollision();
            }
            else
            {
                fireAlarmButton.transform.localPosition = Vector3.Lerp(fireAlarmButton.transform.localPosition, buttonInitialPosition, Time.deltaTime * returnSpeed);
            }

            // 트리거 충돌 확인 (플레이어가 트리거에 들어왔는지 확인)
            CheckTriggerCollision();
        }

        void CheckButtonTriggerCollision()
        {
            if (buttonTriggerCollider == null) return;

            if (buttonCollider.bounds.Intersects(buttonTriggerCollider.bounds))
            {
                if (!isButtonTriggerActivated)
                {
                    onFireAlarmActivated?.Invoke();
                    isButtonTriggerActivated = true;
                    if (subtitleText != null)
                    {
                        subtitleText.text = "화재 경보가 활성화되었습니다!";
                    }

                    // 버튼 눌림 시 사운드 재생
                    if (playSoundOnButtonPress && SoundManager.Instance != null)
                    {
                        SoundManager.Instance.Play(soundGroupIndex, soundClipIndex, loopSound);
                    }
                }
            }
        }

        void CheckTriggerCollision()
        {
            // 플레이어의 머리 위치를 기준으로 트리거 충돌 확인
            if (headTransform == null) return;

            // 문 앞 트리거 확인
            if (!hasReachedFrontDoorTrigger)
            {
                Collider frontTriggerCollider = frontDoorTrigger.GetComponent<Collider>();
                if (frontTriggerCollider != null && frontTriggerCollider.bounds.Contains(headTransform.position))
                {
                    hasReachedFrontDoorTrigger = true;
                    StartCoroutine(FrontDoorSequence());
                }
            }

            // 문 반대편 트리거 확인
            if (!hasReachedBackDoorTrigger)
            {
                Collider backTriggerCollider = backDoorTrigger.GetComponent<Collider>();
                if (backTriggerCollider != null && backTriggerCollider.bounds.Contains(headTransform.position))
                {
                    hasReachedBackDoorTrigger = true;
                    StartCoroutine(BackDoorSequence());
                }
            }
        }

        IEnumerator FrontDoorSequence()
        {
            ShowSubtitle("잠깐! 문을 열기 전에 문 손잡이의 온도를 확인하는 게 중요해요.");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("문 손잡이가 뜨겁다면 반대편에 불이 가까이 있다는 신호예요!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("지금은 괜찮은 것 같네요. 문 손잡이를 잡고 문을 열어보세요.");
            yield return new WaitForSeconds(textDelay);
        }

        IEnumerator BackDoorSequence()
        {
            ShowSubtitle("잘했어요! 이제 탈출구로 이동해봅시다!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("탈출하기 전에, 건물에 탈출 경로 안내도가 있다면 꼭 확인해야 해요.");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("평소 자주 사용하는 건물이라면 미리 경로를 숙지해두는 게 좋아요!");
            yield return new WaitForSeconds(textDelay);

            // 안내도 하이라이트
            HighlightEvacuationMap();
            ShowSubtitle("앞에 있는 안내도를 확인해서 탈출 경로를 파악해보세요.");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("그리고 화재를 처음 발견했다면, 화재 경보 버튼을 눌러 주변에 위험을 알려야 해요.");
            yield return new WaitForSeconds(textDelay);
        }

        void ShowSubtitle(string message)
        {
            if (subtitleText != null)
            {
                subtitleText.color = Color.black;
                subtitleText.text = message;
            }
        }

        void HighlightEvacuationMap()
        {
            if (evacuationMap != null && !hasHighlightedMap)
            {
                Renderer mapRenderer = evacuationMap.GetComponent<Renderer>();
                if (mapRenderer != null)
                {
                    mapRenderer.material.color = Color.yellow;
                    hasHighlightedMap = true;
                }
            }
        }
    }
}