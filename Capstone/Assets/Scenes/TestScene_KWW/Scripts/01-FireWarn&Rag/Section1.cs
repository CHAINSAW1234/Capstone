using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace FireEvacuation
{
#pragma warning disable 0618 // XRBaseController에 대한 폐기 경고 억제

    public class Section1 : MonoBehaviour
    {
        [Header("UI 설정")]
        public TMP_Text subtitleText; // 자막 텍스트 UI
        public float textDelay = 5f; // 자막 표시 지연 시간 (초)

        [Header("사운드 설정")]
        public int sirenGroupIndex = 0; // 사운드 그룹 인덱스 (Inspector에서 설정)
        public int sirenClipIndex = 0; // 재생할 클립 인덱스

        [Header("후처리 효과")]
        public Volume globalVolume; // 전역 볼륨 (후처리 효과)
        private Vignette vignette; // 비네트 효과

        [Header("Rag 설정")]
        public GameObject ragObject; // Inspector에서 할당할 Rag 오브젝트
        public GameObject waterObject; // Inspector에서 할당할 Water 오브젝트
        public Transform headTransform; // XR Rig의 Main Camera 참조
        public float protectionDistance = 0.2f; // 머리와의 보호 감지 거리

        [Header("문 설정")]
        public GameObject doorObject; // 문 오브젝트
        public Collider doorHandle1; // 문 손잡이 1
        public Collider doorHandle2; // 문 손잡이 2
        public GameObject frontDoorTrigger; // 문 앞 트리거
        public GameObject backDoorTrigger; // 문 반대편 트리거

        // 타이머 관련 변수
        public float waterSearchTime = 15f; // 물 찾기 시간 (사용자 설정 가능)
        private float searchTimer = 0f;
        private bool isSearchingForWater = false;
        private float wetTimeRequired = 5f; // 헝겊을 적시는 데 필요한 시간
        private float wetTimer = 0f;
        private bool isWetting = false;

        // UnityEvent로 변경하여 Inspector에서 설정 가능
        [SerializeField] public UnityEvent onProtectionActivated; // 호흡 보호 활성화 이벤트
        [SerializeField] public UnityEvent onProtectionDeactivated; // 호흡 보호 비활성화 이벤트

        // Rag 관련 변수
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
        private Renderer cubeRenderer;
        private bool isProtecting = false;

        // 문 관련 변수
        private Rigidbody doorRb;
        private HingeJoint hingeJoint;
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable doorGrabInteractable;

        // 시나리오 진행 상태 추적
        private bool hasFireRecognized = false; // 화재 인지 완료 여부
        private bool hasRagGrabbed = false; // 헝겊 그랩 완료 여부
        private static bool hasRagWetted = false; // 헝겊 물에 적심 완료 여부 (정적 변수로 유지)
        private bool hasProtectionActivated = false; // 호흡 보호 활성화 완료 여부
        private bool hasShownProtectionMessage = false; // 호흡 보호 메시지 출력 여부
        private bool hasReachedFrontDoor = false; // 문 앞 트리거 도달 여부
        private bool hasReachedBackDoor = false; // 문 반대편 트리거 도달 여부

        private void Start()
        {
            // Rag 초기화
            InitializeRag();

            // 후처리 초기화
            InitPostProcessing();

            // 문 초기화
            SetupDoor();

            // 시퀀스 시작
            StartCoroutine(DrillSequence());
        }

        void InitializeRag()
        {
            if (ragObject == null)
            {
                Debug.LogError("Rag 오브젝트가 지정되지 않았습니다!");
                return;
            }

            grabInteractable = ragObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grabInteractable == null)
            {
                Debug.LogError("Rag 오브젝트에 XRGrabInteractable 컴포넌트가 없습니다!");
            }
            else
            {
                // 그랩 이벤트에 메서드 연결
                grabInteractable.selectEntered.AddListener(OnRagGrabbed);
            }

            cubeRenderer = ragObject.GetComponent<Renderer>();
            if (cubeRenderer == null)
            {
                Debug.LogError("Rag 오브젝트에 Renderer 컴포넌트가 없습니다!");
            }
            else
            {
                cubeRenderer.material = new Material(cubeRenderer.material); // Material 인스턴스화
                cubeRenderer.material.color = Color.red; // 초기 색상 (빨간색)
            }

            if (waterObject == null)
            {
                Debug.LogError("Water 오브젝트가 지정되지 않았습니다!");
                return;
            }
            else
            {
                // Water 오브젝트에 "Water" 태그가 있는지 확인
                if (!waterObject.CompareTag("Water"))
                {
                    Debug.LogWarning("Water 오브젝트에 'Water' 태그가 없습니다. 태그를 추가합니다.");
                    waterObject.tag = "Water";
                }
            }

            if (headTransform == null)
            {
                headTransform = GameObject.Find("Main Camera")?.transform; // XR Rig의 카메라
                if (headTransform == null)
                {
                    Debug.LogError("Main Camera를 찾을 수 없습니다! Inspector에서 Head Transform을 수동으로 지정해주세요.");
                }
            }
        }

        void InitPostProcessing()
        {
            if (globalVolume != null && globalVolume.profile.TryGet(out vignette))
            {
                vignette.active = false;
                vignette.intensity.Override(0f);
            }
            else
            {
                Debug.LogError("Global Volume이 지정되지 않았거나 Vignette 설정을 찾을 수 없습니다!");
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
            doorGrabInteractable.enabled = false; // 초기에는 문 상호작용 비활성화

            Debug.Log("✅ 문 설정 완료.");
        }

        IEnumerator DrillSequence()
        {
            ShowSubtitle("화재 대피 훈련에 오신 것을 환영합니다!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("이 훈련에서는 아파트 화재 상황에서 안전하게 대피하는 방법을 배웁니다.");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("단계별로 따라오며 안전한 대피 방법을 익혀보세요!");
            yield return new WaitForSeconds(textDelay);

            // 사이렌 재생 및 비네팅 효과
            try
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayOneShot(sirenGroupIndex, sirenClipIndex);
                    Debug.Log($"사이렌 사운드 재생 시도: Group Index {sirenGroupIndex}, Clip Index {sirenClipIndex}");
                }
                else
                {
                    Debug.LogError("SoundManager.Instance가 null입니다!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("사이렌 사운드 재생 중 오류 발생: " + e.Message);
            }

            yield return EnableVignetteEffect();

            yield return FlashingSubtitle("건물에 화재가 발생했습니다! 경보를 들어보세요!", 0.6f);
            hasFireRecognized = true; // 화재 인지 완료

            ShowSubtitle("먼저 호흡을 보호하기 위해 헝겊을 찾아야 합니다!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("주변에서 헝겊으로 사용할 물건을 찾아보세요!");
            yield return new WaitForSeconds(textDelay);

            // Vignette 효과 활성화 (호흡 보호 비활성화 상태)
            if (vignette != null)
            {
                vignette.active = true;
                vignette.intensity.Override(0.5f);
            }
        }

        void OnRagGrabbed(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
        {
            if (!hasFireRecognized)
            {
                ShowSubtitle("먼저 화재를 인지하세요! 경보를 듣고 상황을 파악하세요!");
                return;
            }

            if (!hasRagGrabbed)
            {
                hasRagGrabbed = true;
                ShowSubtitle("잘했어요! 이제 헝겊을 물에 적셔야 합니다!");
                isSearchingForWater = true;
                searchTimer = waterSearchTime;
            }
        }

        void Update()
        {
            // 물 찾기 타이머 처리
            if (isSearchingForWater)
            {
                searchTimer -= Time.deltaTime;
                if (searchTimer <= 0)
                {
                    HighlightWater();
                    isSearchingForWater = false;
                }
            }

            // 헝겊 적시기 타이머 처리
            if (isWetting)
            {
                Debug.Log($"적시는 중: wetTimer = {wetTimer}, hasRagWetted = {hasRagWetted}");
                wetTimer += Time.deltaTime;
                if (wetTimer >= wetTimeRequired && !hasRagWetted)
                {
                    WetRag();
                    isWetting = false;
                }
            }

            // 호흡 보호 상태 확인
            if (headTransform != null && ragObject != null)
            {
                float distanceToHead = Vector3.Distance(ragObject.transform.position, headTransform.position);
                if (!isProtecting && distanceToHead <= protectionDistance)
                {
                    Debug.Log($"거리 확인: hasRagWetted = {hasRagWetted}");
                    if (!hasRagGrabbed)
                    {
                        ShowSubtitle("먼저 헝겊을 집어주세요!");
                        return;
                    }
                    if (!hasRagWetted)
                    {
                        ShowSubtitle("먼저 헝겊을 물에 적셔야 합니다!");
                        return;
                    }
                    SetProtectionState(true);
                }
                else if (isProtecting && distanceToHead > protectionDistance)
                {
                    SetProtectionState(false);
                }
            }

            // 문 트리거 충돌 확인
            CheckTriggerCollision();
        }

        void CheckTriggerCollision()
        {
            if (headTransform == null) return;

            // 문 앞 트리거 확인
            if (!hasReachedFrontDoor)
            {
                Collider frontTriggerCollider = frontDoorTrigger.GetComponent<Collider>();
                if (frontTriggerCollider != null && frontTriggerCollider.bounds.Contains(headTransform.position))
                {
                    if (!hasRagGrabbed)
                    {
                        ShowSubtitle("먼저 헝겊을 집어주세요!");
                        return;
                    }
                    if (!hasRagWetted)
                    {
                        ShowSubtitle("먼저 헝겊을 물에 적셔야 합니다!");
                        return;
                    }
                    if (!hasProtectionActivated)
                    {
                        ShowSubtitle("먼저 호흡 보호를 위해 젖은 헝겊을 입에 대세요!");
                        return;
                    }
                    hasReachedFrontDoor = true;
                    StartCoroutine(FrontDoorSequence());
                }
            }

            // 문 반대편 트리거 확인
            if (!hasReachedBackDoor)
            {
                Collider backTriggerCollider = backDoorTrigger.GetComponent<Collider>();
                if (backTriggerCollider != null && backTriggerCollider.bounds.Contains(headTransform.position))
                {
                    if (!hasReachedFrontDoor)
                    {
                        ShowSubtitle("먼저 문 손잡이의 온도를 확인하고 문을 열어야 합니다!");
                        return;
                    }
                    hasReachedBackDoor = true;
                    StartCoroutine(BackDoorSequence());
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            Debug.Log($"OnTriggerEnter 호출됨: {other.gameObject.name}");
            if (other.gameObject == waterObject && !hasRagWetted)
            {
                if (!hasRagGrabbed)
                {
                    ShowSubtitle("먼저 헝겊을 집어주세요!");
                    return;
                }
                Debug.Log("물 감지됨, 헝겊 적시기 시작.");
                isSearchingForWater = false;
                isWetting = true;
                wetTimer = 0f;
                ShowSubtitle("잘했어요! 5초 동안 헝겊을 물에 적셔주세요!");
            }
        }

        void OnTriggerExit(Collider other)
        {
            Debug.Log($"OnTriggerExit 호출됨: {other.gameObject.name}");
            if (other.gameObject == waterObject && !hasRagWetted)
            {
                Debug.Log("물에서 벗어남, 적시기 중단.");
                isWetting = false;
                ShowSubtitle("헝겊을 5초 이상 물에 적셔야 합니다!");
            }
        }

        void ShowSubtitle(string message)
        {
            if (subtitleText != null)
            {
                subtitleText.color = Color.black;
                subtitleText.text = message;
            }
        }

        IEnumerator FlashingSubtitle(string text, float flashSpeed = 0.5f)
        {
            float timer = 0f;
            float duration = textDelay;
            bool isVisible = true;

            if (subtitleText != null)
            {
                subtitleText.text = text;
            }

            while (timer < duration)
            {
                isVisible = !isVisible;
                if (subtitleText != null)
                {
                    subtitleText.color = isVisible ? Color.red : new Color(1f, 0.4f, 0.4f);
                }

                timer += flashSpeed;
                yield return new WaitForSeconds(flashSpeed);
            }

            if (subtitleText != null)
            {
                subtitleText.color = Color.black;
                subtitleText.text = text;
            }
        }

        IEnumerator EnableVignetteEffect()
        {
            if (vignette == null) yield break;

            float duration = 1.5f;
            float timer = 0f;
            float start = vignette.intensity.value;
            float target = 0.45f;

            vignette.active = true;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                vignette.intensity.value = Mathf.Lerp(start, target, t);
                yield return null;
            }

            vignette.intensity.Override(target);
        }

        IEnumerator FrontDoorSequence()
        {
            ShowSubtitle("문 손잡이의 온도를 먼저 확인해야 합니다!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("문 손잡이가 뜨겁다면 반대편에 불이 있을 수 있습니다!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("지금은 안전합니다! 문 손잡이를 잡고 문을 열어보세요!");
            yield return new WaitForSeconds(textDelay);

            // 문 상호작용 활성화
            if (doorGrabInteractable != null)
            {
                doorGrabInteractable.enabled = true;
                Debug.Log("✅ 문 상호작용 활성화.");
            }
        }

        IEnumerator BackDoorSequence()
        {
            ShowSubtitle("잘했어요! 문을 통과했습니다!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("이제 건물 밖으로 대피해볼까요.");
            yield return new WaitForSeconds(textDelay);
        }

        void WetRag()
        {
            hasRagWetted = true;
            if (cubeRenderer != null)
            {
                cubeRenderer.material.color = Color.green;
            }
            Debug.Log($"헝겊 적시기 완료. hasRagWetted: {hasRagWetted}");
            ShowSubtitle("잘했어요! 이제 젖은 헝겊을 입 주변에 대보세요!");
        }

        void HighlightWater()
        {
            if (waterObject != null)
            {
                Renderer waterRenderer = waterObject.GetComponent<Renderer>();
                if (waterRenderer != null)
                {
                    waterRenderer.material.color = Color.yellow;
                }
            }
            ShowSubtitle("물이 있는 곳을 찾아 헝겊을 적셔보세요!");
        }

        void SetProtectionState(bool state)
        {
            if (isProtecting == state)
            {
                return;
            }

            isProtecting = state;
            if (isProtecting)
            {
                if (!hasShownProtectionMessage) // 메시지가 아직 출력되지 않았을 때만 표시
                {
                    ShowSubtitle("잘했어요! 호흡 보호가 활성화되었습니다!");
                    hasShownProtectionMessage = true; // 메시지 출력 플래그 설정
                }
                if (!hasProtectionActivated)
                {
                    onProtectionActivated?.Invoke();
                    hasProtectionActivated = true;
                    StartCoroutine(ShowNextSubtitleAfterDelay());
                }
                if (vignette != null)
                {
                    vignette.intensity.Override(0f);
                }
            }
            else
            {
                if (vignette != null)
                {
                    vignette.intensity.Override(0.5f);
                }
            }
        }

        private IEnumerator ShowNextSubtitleAfterDelay()
        {
            yield return new WaitForSeconds(5f);
            ShowSubtitle("다음은 탈출을 위해 출입문을 찾아보세요!");
        }

        public void SetWaterSearchTime(float time)
        {
            waterSearchTime = time;
            searchTimer = waterSearchTime;
        }
    }
}