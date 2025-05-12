using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace FireEvacuation
{
#pragma warning disable 0618 // XRBaseController에 대한 폐지 경고 억제

    public class Section1 : MonoBehaviour
    {
        [Header("훈련 모드 설정")]
        [SerializeField] private bool isPracticeMode = true; // 연습/평가 모드 토글 (Inspector에서 설정 가능)
        public GameObject[] highlightObjects; // 하이라이트할 물체들 (사용하지 않음, 호환성 유지)

        [Header("아웃라인 설정")]
        public Color outlineColor = Color.red; // 아웃라인 색상 (기본값: 빨간색)
        public float outlineWidth = 2f; // 아웃라인 두께
        public GameObject outlineRagObject; // 아웃라인 적용할 Rag 오브젝트
        public GameObject[] outlineWaterObjects = new GameObject[2]; // 아웃라인 적용할 Water 오브젝트 배열 (2개)
        public GameObject outlineDoorObject; // 아웃라인 적용할 문 오브젝트
        public GameObject outlineDoorHandleObject; // 아웃라인 적용할 문 오브젝트

        [Header("화살표 설정")]
        public GameObject WaterArrow1; // 첫 번째 화살표 오브젝트
        public GameObject WaterArrow2; // 두 번째 화살표 오브젝트
        public GameObject DoorArrow; // 두 번째 화살표 오브젝트
        public GameObject NextSectionArrow; // 두 번째 화살표 오브젝트

        [Header("UI 설정")]
        public TMP_Text subtitleText; // 자막 텍스트 UI
        public float textDelay = 5f; // 자막 표시 지연 시간 (초)

        [Header("사운드 설정")]
        public int sirenGroupIndex = 0; // 사운드 그룹 인덱스
        public int sirenClipIndex = 0; // 재생할 클립 인덱스

        [Header("후처리 효과")]
        public Volume globalVolume; // 전역 볼륨 (후처리 효과)
        private Vignette vignette; // 비네트 효과

        [Header("Rag 설정")]
        public GameObject ragObject; // Rag 오브젝트
        public GameObject[] waterObjects = new GameObject[2]; // Water 오브젝트 배열 (2개)
        public Transform headTransform; // XR Rig의 Main Camera
        public float protectionDistance = 0.2f; // 머리와의 보호 감지 거리

        [Header("문 설정")]
        public GameObject doorObject; // 문 오브젝트
        public Collider doorHandle1; // 문 손잡이 1
        public Collider doorHandle2; // 문 손잡이 2
        public GameObject frontDoorTrigger; // 문 앞 트리거
        public GameObject backDoorTrigger; // 문 반대편 트리거

        [Header("타이머 설정")]
        public float waterSearchTime = 15f; // 물 찾기 시간
        private float searchTimer = 0f;
        private bool isSearchingForWater = false;
        private float wetTimeRequired = 5f; // 헝겊을 적시는 데 필요한 시간
        private float wetTimer = 0f;
        private bool isWetting = false;

        [Header("이벤트 설정")]
        [SerializeField] public UnityEvent onProtectionActivated;
        [SerializeField] public UnityEvent onProtectionDeactivated;

        // Rag 관련 변수
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
        private Renderer cubeRenderer;
        private bool isProtecting = false;

        // 문 관련 변수
        private Rigidbody doorRb;
        private HingeJoint hingeJoint;
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable doorGrabInteractable;

        // 시나리오 진행 상태
        private bool hasFireRecognized = false;
        private bool hasRagGrabbed = false;
        private static bool hasRagWetted = false;
        private bool hasProtectionActivated = false;
        private bool hasShownProtectionMessage = false;
        private bool hasReachedFrontDoor = false;
        private bool hasReachedBackDoor = false;

        private bool isHandleTouched = false;
        private float handleTouchTime = 0f;
        private float requiredTouchTime = 3f; // 온도 확인에 필요한 최소 접촉 시간
        private bool isHandleSafe = false;

        [Header("Haptic 설정")]
        public XRBaseController leftHandController;
        public XRBaseController rightHandController;
        public float vibrationIntensity = 0.5f;
        public float vibrationDuration = 0.1f;

        private void Start()
        {
            hasRagWetted = false;
            InitializeRag();
            InitPostProcessing();
            SetupDoor();

            // 화살표 초기 비활성화
            if (WaterArrow1 != null) WaterArrow1.SetActive(false);
            if (WaterArrow2 != null) WaterArrow2.SetActive(false);
            if (DoorArrow != null) DoorArrow.SetActive(false);
            if (NextSectionArrow != null) NextSectionArrow.SetActive(false);

            if (!isPracticeMode && subtitleText != null)
            {
                subtitleText.gameObject.SetActive(false);
            }

            StartCoroutine(DrillSequence());
        }

        void InitializeRag()
        {
            if (ragObject == null)
            {
                return;
            }

            grabInteractable = ragObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grabInteractable == null)
            {
                //Debug.LogError("Rag 오브젝트에 XRGrabInteractable 컴포넌트가 없습니다!");
            }
            else
            {
                grabInteractable.selectEntered.AddListener(OnRagGrabbed);
            }

            cubeRenderer = ragObject.GetComponent<Renderer>();
            if (cubeRenderer == null)
            {
                //Debug.LogError("Rag 오브젝트에 Renderer 컴포넌트가 없습니다!");
            }
            else
            {
                cubeRenderer.material = new Material(cubeRenderer.material);
                cubeRenderer.material.color = Color.red;
            }

            Collider ragCollider = ragObject.GetComponent<Collider>();
            if (ragCollider == null)
            {
                ragCollider = ragObject.AddComponent<BoxCollider>();
                //Debug.LogWarning("Rag 오브젝트에 콜라이더가 없어 추가했습니다.");
            }

            Rigidbody ragRb = ragObject.GetComponent<Rigidbody>();
            if (ragRb == null)
            {
                ragRb = ragObject.AddComponent<Rigidbody>();
                ragRb.useGravity = true;
                ragRb.isKinematic = false;
                ragRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                //Debug.LogWarning("Rag 오브젝트에 Rigidbody가 없어 추가했습니다.");
            }

            for (int i = 0; i < waterObjects.Length; i++)
            {
                if (waterObjects[i] == null)
                {
                    //Debug.LogError($"Water 오브젝트 {i}가 지정되지 않았습니다!");
                    continue;
                }

                Collider waterCollider = waterObjects[i].GetComponent<Collider>();
                if (waterCollider == null)
                {
                    waterCollider = waterObjects[i].AddComponent<BoxCollider>();
                    waterCollider.isTrigger = true;
                    //Debug.LogWarning($"Water 오브젝트 {i}에 콜라이더가 없어 추가했습니다.");
                }
                else if (!waterCollider.isTrigger)
                {
                    waterCollider.isTrigger = true;
                    //Debug.LogWarning($"Water 오브젝트 {i}의 콜라이더가 트리거로 설정되지 않았습니다. 트리거로 설정했습니다.");
                }

                if (!waterObjects[i].CompareTag("Water"))
                {
                    //Debug.LogWarning($"Water 오브젝트 {i}에 'Water' 태그가 없습니다. 태그를 추가합니다.");
                    waterObjects[i].tag = "Water";
                }
            }

            if (headTransform == null)
            {
                headTransform = GameObject.Find("Main Camera")?.transform;
                if (headTransform == null)
                {
                    //Debug.LogError("Main Camera를 찾을 수 없습니다! Inspector에서 Head Transform을 수동으로 지정해주세요.");
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
                //Debug.LogError("Global Volume이 지정되지 않았거나 Vignette 설정을 찾을 수 없습니다!");
            }
        }

        void SetupDoor()
        {
            if (doorObject == null)
            {
                //Debug.LogError("문 오브젝트가 지정되지 않았습니다!");
                return;
            }

            doorRb = doorObject.GetComponent<Rigidbody>();
            if (doorRb == null)
            {
                doorRb = doorObject.AddComponent<Rigidbody>();
            }
            doorRb.mass = 1f;
            doorRb.angularDamping = 0.05f;
            doorRb.useGravity = true;
            doorRb.isKinematic = true;
            doorRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

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
            doorGrabInteractable.enabled = false;

            //Debug.Log("✅ 문 설정 완료.");
        }

        IEnumerator DrillSequence()
        {
            if (isPracticeMode)
            {
                ShowSubtitle("화재 대피 훈련에 오신 것을 환영합니다!");
                yield return new WaitForSeconds(textDelay);

                ShowSubtitle("이 훈련은 아파트 화재 상황에서 안전한 대피 방법을 익히는 과정입니다.");
                yield return new WaitForSeconds(textDelay);

                ShowSubtitle("단계별로 따라오며 안전한 대피 방법을 익혀보세요!");
                yield return new WaitForSeconds(textDelay);
            }

            int maxRetries = 50;
            int retries = 0;
            while (SoundManager.Instance == null && retries < maxRetries)
            {
                //Debug.LogWarning("SoundManager.Instance is null. Waiting for initialization...");
                yield return new WaitForSeconds(0.1f);
                retries++;
            }

            if (SoundManager.Instance == null)
            {
                //Debug.LogError("SoundManager.Instance is still null after waiting. Cannot play siren sound.");
                yield break;
            }

            try
            {
                SoundManager.Instance.PlayOneShot(sirenGroupIndex, sirenClipIndex);
                //Debug.Log($"사이렌 사운드 재생 시도: Group Index {sirenGroupIndex}, Clip Index {sirenClipIndex}");
            }
            catch (System.Exception e)
            {
                //Debug.LogError("사이렌 사운드 재생 중 오류 발생: " + e.Message);
            }

            yield return EnableVignetteEffect();

            if (isPracticeMode)
            {
                yield return FlashingSubtitle("건물에 화재가 발생했습니다!", 0.6f);
            }
            hasFireRecognized = true;
            SequenceManager.Instance.CompleteStep(0);

            if (isPracticeMode)
            {
                ShowSubtitle("화재 발생 시, 가장 먼저 젖은 옷이나 수건 등의 천으로 호흡기를 보호하는 것이 중요합니다.");
                yield return new WaitForSeconds(textDelay);

                ShowSubtitle("주변에서 호흡 보호에 사용할 천을 찾아보세요!");
                if (outlineRagObject != null && isPracticeMode) AddOutline(outlineRagObject);
                yield return new WaitForSeconds(textDelay);
            }
            else
            {
                if (outlineRagObject != null) AddOutline(outlineRagObject);
            }
        }

        void OnRagGrabbed(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
        {
            if (!hasFireRecognized)
            {
                if (isPracticeMode) ShowSubtitle("첫째로 화재 상황을 인지해야 합니다!");
                SequenceManager.Instance.RecordSequenceError(0);
                return;
            }

            if (!hasRagGrabbed)
            {
                hasRagGrabbed = true;
                if (outlineRagObject != null) RemoveOutline(outlineRagObject);
                if (isPracticeMode) ShowSubtitle("잘했습니다! 이제 천을 물에 적셔야 합니다!");
                isSearchingForWater = true;
                searchTimer = waterSearchTime;
                //Debug.Log("천을 집었습니다. hasRagGrabbed: " + hasRagGrabbed);
            }
        }

        void Update()
        {
            if (isSearchingForWater)
            {
                searchTimer -= Time.deltaTime;
                if (searchTimer <= 0)
                {
                    if (isPracticeMode) AddOutlineToWater();
                    isSearchingForWater = false;
                }
            }

            if (!hasRagWetted && hasRagGrabbed && ragObject != null)
            {
                Collider ragCollider = ragObject.GetComponent<Collider>();
                if (ragCollider != null)
                {
                    foreach (GameObject waterObject in waterObjects)
                    {
                        if (waterObject == null) continue;
                        Collider waterCollider = waterObject.GetComponent<Collider>();
                        if (waterCollider != null && ragCollider.bounds.Intersects(waterCollider.bounds))
                        {
                            if (!isWetting)
                            {
                                isSearchingForWater = false;
                                isWetting = true;
                                wetTimer = 0f;
                                StartCoroutine(WetRagSequence());
                            }
                            break;
                        }
                    }
                }
            }

            if (headTransform != null && ragObject != null)
            {
                float distanceToHead = Vector3.Distance(ragObject.transform.position, headTransform.position);
                if (!isProtecting && distanceToHead <= protectionDistance)
                {
                    if (!hasRagGrabbed)
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 천을 집어주세요!");
                        SequenceManager.Instance.RecordSequenceError(1);
                        return;
                    }
                    if (!hasRagWetted)
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 천을 물에 적셔야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(1);
                        return;
                    }
                    SequenceManager.Instance.CompleteStep(1);
                    SetProtectionState(true);
                }
                else if (isProtecting && distanceToHead > protectionDistance)
                {
                    SetProtectionState(false);
                }
            }

            CheckTriggerCollision();
        }

        void CheckTriggerCollision()
        {
            if (headTransform == null) return;

            if (!hasReachedFrontDoor)
            {
                Collider frontTriggerCollider = frontDoorTrigger.GetComponent<Collider>();
                if (frontTriggerCollider != null && frontTriggerCollider.bounds.Contains(headTransform.position))
                {
                    if (!hasRagGrabbed)
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 천을 집어주세요!");
                        SequenceManager.Instance.RecordSequenceError(1);
                        return;
                    }
                    if (!hasRagWetted)
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 천을 물에 적셔야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(1);
                        return;
                    }
                    if (!hasProtectionActivated)
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 호흡 보호를 위해 젖은 천을 입에 대세요!");
                        SequenceManager.Instance.RecordSequenceError(1);
                        return;
                    }
                    if (DoorArrow != null) DoorArrow.SetActive(false);
                    hasReachedFrontDoor = true;
                    SequenceManager.Instance.CompleteStep(2);
                    StartCoroutine(FrontDoorSequence());
                }
            }

            if (!hasReachedBackDoor)
            {
                Collider backTriggerCollider = backDoorTrigger.GetComponent<Collider>();
                if (backTriggerCollider != null && backTriggerCollider.bounds.Contains(headTransform.position))
                {
                    if (!hasReachedFrontDoor)
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 문 손잡이의 온도를 확인하고 문을 열어야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(2);
                        return;
                    }
                    hasReachedBackDoor = true;
                    StartCoroutine(BackDoorSequence());
                }
            }
        }

        void ShowSubtitle(string message)
        {
            if (subtitleText != null && isPracticeMode)
            {
                subtitleText.color = Color.black;
                subtitleText.text = message;
                //Debug.Log($"자막 표시: {message}");
            }
            else
            {
                //Debug.LogWarning("subtitleText가 null이거나 평가 모드입니다!");
            }
        }

        IEnumerator FlashingSubtitle(string text, float flashSpeed = 0.5f)
        {
            if (!isPracticeMode) yield break;

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

        void TriggerHapticFeedback()
        {
            if (leftHandController != null)
            {
                leftHandController.SendHapticImpulse(vibrationIntensity, vibrationDuration);
            }
            if (rightHandController != null)
            {
                rightHandController.SendHapticImpulse(vibrationIntensity, vibrationDuration);
            }
        }

        IEnumerator EnableVignetteEffect()
        {
            if (vignette == null) yield break;

            float duration = 1.5f;
            float timer = 0f;
            float start = vignette.intensity.value;
            float target = 0.70f;

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
            if (isPracticeMode)
            {
                ShowSubtitle("대피 시 문을 열기 전 손잡이의 온도를 먼저 확인해야 합니다!");
                yield return new WaitForSeconds(textDelay);

                ShowSubtitle("문 손잡이가 뜨거우면 반대편에 불이 있을 수 있으니, 다른 경로로 대피해야 합니다.");
                yield return new WaitForSeconds(textDelay);

                ShowSubtitle("맨 손으로 온도를 확인하는 것은 위험합니다! 젖은 천을 이용해서 확인해봅시다.");
                if (outlineDoorHandleObject != null && isPracticeMode) AddOutline(outlineDoorHandleObject);
                yield return new WaitForSeconds(textDelay);
            }

            isHandleTouched = false;
            handleTouchTime = 0f;

            while (!isHandleTouched)
            {
                if (IsRagTouchingHandle())
                {
                    if (handleTouchTime == 0f)
                    {
                        if (isPracticeMode) ShowSubtitle("손잡이에 천이 닿았습니다. 온도를 확인하는 중입니다...");
                    }

                    handleTouchTime += Time.deltaTime;
                    TriggerHapticFeedback();

                    if (handleTouchTime >= requiredTouchTime)
                    {
                        isHandleSafe = true;
                        isHandleTouched = true;
                        if (outlineDoorObject != null) RemoveOutline(outlineDoorObject);
                        if (outlineDoorHandleObject != null) RemoveOutline(outlineDoorHandleObject);
                        if (isPracticeMode) ShowSubtitle("문 손잡이가 뜨겁지 않은 것으로 보아 안전합니다. 문을 열고 탈출하세요!");

                        if (doorRb != null)
                        {
                            doorRb.isKinematic = false;
                        }
                    }
                }
                else
                {
                    if (handleTouchTime > 0f)
                    {
                        if (isPracticeMode) ShowSubtitle("손잡이에서 천이 떨어졌습니다. 다시 시도하세요.");
                    }

                    handleTouchTime = 0f;
                }

                yield return null;
            }

            if (doorRb != null)
            {
                doorRb.isKinematic = false;
            }
        }

        IEnumerator BackDoorSequence()
        {
            if (isPracticeMode)
            {
                ShowSubtitle("잘했습니다! 방을 나왔습니다!");
                yield return new WaitForSeconds(textDelay);

                ShowSubtitle("이제 안전하게 건물 밖으로 대피하는 법을 배워봅시다.");
                yield return new WaitForSeconds(textDelay);
                if (NextSectionArrow != null) NextSectionArrow.SetActive(true);
            }
        }

        IEnumerator WetRagSequence()
        {
            hasRagWetted = true;
            //Debug.Log("천이 젖음. hasRagWetted: true");
            if (isPracticeMode)
            {
                if (WaterArrow1 != null) WaterArrow1.SetActive(false);
                if (WaterArrow2 != null) WaterArrow2.SetActive(false);
                ShowSubtitle("잘했습니다! 충분히 천을 물에 적셔주세요!");
                yield return new WaitForSeconds(textDelay);
                ShowSubtitle("이제 젖은 천을 호흡기에 갖다 대어 호흡을 보호합시다!");
            }
        }

        void AddOutline(GameObject target)
        {
            if (target == null || !isPracticeMode) return;

            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.AddComponent<Outline>();
            }

            outline.OutlineMode = Outline.Mode.OutlineAll;
            outline.OutlineColor = outlineColor;
            outline.OutlineWidth = outlineWidth;
            outline.enabled = true;
        }

        void RemoveOutline(GameObject target)
        {
            if (target == null) return;

            Outline outline = target.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = false;
            }

            // 화살표 비활성화
            if (WaterArrow1 != null) WaterArrow1.SetActive(false);
            if (WaterArrow2 != null) WaterArrow2.SetActive(false);
        }

        void AddOutlineToWater()
        {
            if (isPracticeMode)
            {
                ShowSubtitle("물이 있는 곳을 찾아 천을 적셔봅시다!");
                // 화살표 활성화
                WaterArrow1.SetActive(true);
                WaterArrow2.SetActive(true);
            }
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
                if (!hasShownProtectionMessage)
                {
                    if (isPracticeMode) ShowSubtitle("잘했어요! 이제 유독가스로부터 호흡기를 보호할 수 있습니다!");
                    hasShownProtectionMessage = true;
                }
                if (!hasProtectionActivated)
                {
                    onProtectionActivated?.Invoke();
                    hasProtectionActivated = true;
                    if (isPracticeMode) StartCoroutine(ShowNextSubtitleAfterDelay());
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
                    vignette.intensity.Override(0.8f);
                }
            }
        }

        private IEnumerator ShowNextSubtitleAfterDelay()
        {
            yield return new WaitForSeconds(5f);
            if (isPracticeMode)
            {
                ShowSubtitle("다음은 탈출을 위해 출입문을 찾아봅시다!");
                if (DoorArrow != null) DoorArrow.SetActive(true);
                if (outlineDoorObject != null) AddOutline(outlineDoorObject);
            }
        }

        private bool IsRagTouchingHandle()
        {
            if (ragObject == null || (doorHandle1 == null && doorHandle2 == null)) return false;
            Collider ragCollider = ragObject.GetComponent<Collider>();
            if (ragCollider == null) return false;

            return (doorHandle1 != null && ragCollider.bounds.Intersects(doorHandle1.bounds)) ||
                   (doorHandle2 != null && ragCollider.bounds.Intersects(doorHandle2.bounds));
        }

        public void SetWaterSearchTime(float time)
        {
            waterSearchTime = time;
            searchTimer = waterSearchTime;
        }
    }
}