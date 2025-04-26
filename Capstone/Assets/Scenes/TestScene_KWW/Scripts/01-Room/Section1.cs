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
        public int sirenGroupIndex = 0; // 사운드 그룹 인덱스
        public int sirenClipIndex = 0; // 재생할 클립 인덱스

        [Header("후처리 효과")]
        public Volume globalVolume; // 전역 볼륨 (후처리 효과)
        private Vignette vignette; // 비네트 효과

        [Header("Rag 설정")]
        public GameObject ragObject; // Rag 오브젝트
        public GameObject waterObject; // Water 오브젝트
        public Transform headTransform; // XR Rig의 Main Camera
        public float protectionDistance = 0.2f; // 머리와의 보호 감지 거리

        [Header("문 설정")]
        public GameObject doorObject; // 문 오브젝트
        public Collider doorHandle1; // 문 손잡이 1
        public Collider doorHandle2; // 문 손잡이 2
        public GameObject frontDoorTrigger; // 문 앞 트리거
        public GameObject backDoorTrigger; // 문 반대편 트리거

        // 타이머 관련 변수
        public float waterSearchTime = 15f; // 물 찾기 시간
        private float searchTimer = 0f;
        private bool isSearchingForWater = false;
        private float wetTimeRequired = 5f; // 헝겊을 적시는 데 필요한 시간
        private float wetTimer = 0f;
        private bool isWetting = false;

        // UnityEvent
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

        private void Start()
        {
            // 강제로 hasRagWetted 초기화 (static 변수로 인해 테스트 중 유지될 수 있음)
            hasRagWetted = false;
            InitializeRag();
            InitPostProcessing();
            SetupDoor();
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
                grabInteractable.selectEntered.AddListener(OnRagGrabbed);
            }

            cubeRenderer = ragObject.GetComponent<Renderer>();
            if (cubeRenderer == null)
            {
                Debug.LogError("Rag 오브젝트에 Renderer 컴포넌트가 없습니다!");
            }
            else
            {
                cubeRenderer.material = new Material(cubeRenderer.material);
                cubeRenderer.material.color = Color.red;
            }

            // Ensure ragObject has a collider and Rigidbody
            Collider ragCollider = ragObject.GetComponent<Collider>();
            if (ragCollider == null)
            {
                ragCollider = ragObject.AddComponent<BoxCollider>();
                Debug.LogWarning("Rag 오브젝트에 콜라이더가 없어 추가했습니다.");
            }

            Rigidbody ragRb = ragObject.GetComponent<Rigidbody>();
            if (ragRb == null)
            {
                ragRb = ragObject.AddComponent<Rigidbody>();
                ragRb.useGravity = true;
                ragRb.isKinematic = false;
                ragRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                Debug.LogWarning("Rag 오브젝트에 Rigidbody가 없어 추가했습니다.");
            }

            if (waterObject == null)
            {
                Debug.LogError("Water 오브젝트가 지정되지 않았습니다!");
                return;
            }
            else
            {
                Collider waterCollider = waterObject.GetComponent<Collider>();
                if (waterCollider == null)
                {
                    waterCollider = waterObject.AddComponent<BoxCollider>();
                    waterCollider.isTrigger = true;
                    Debug.LogWarning("Water 오브젝트에 콜라이더가 없어 추가했습니다.");
                }
                else if (!waterCollider.isTrigger)
                {
                    waterCollider.isTrigger = true;
                    Debug.LogWarning("Water 오브젝트의 콜라이더가 트리거로 설정되지 않았습니다. 트리거로 설정했습니다.");
                }

                if (!waterObject.CompareTag("Water"))
                {
                    Debug.LogWarning("Water 오브젝트에 'Water' 태그가 없습니다. 태그를 추가합니다.");
                    waterObject.tag = "Water";
                }
            }

            if (headTransform == null)
            {
                headTransform = GameObject.Find("Main Camera")?.transform;
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

            Debug.Log("✅ 문 설정 완료.");
        }

        IEnumerator DrillSequence()
        {
            ShowSubtitle("화재 대피 훈련에 오신 것을 환영합니다!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("이 훈련은 아파트 화재 상황에서 안전한 대피 방법을 익히는 과정입니다.");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("단계별로 따라오며 안전한 대피 방법을 익혀보세요!");
            yield return new WaitForSeconds(textDelay);

            // Wait for SoundManager to be ready
            int maxRetries = 50; // Retry for up to 5 seconds (50 * 0.1s)
            int retries = 0;
            while (SoundManager.Instance == null && retries < maxRetries)
            {
                Debug.LogWarning("SoundManager.Instance is null. Waiting for initialization...");
                yield return new WaitForSeconds(0.1f);
                retries++;
            }

            if (SoundManager.Instance == null)
            {
                Debug.LogError("SoundManager.Instance is still null after waiting. Cannot play siren sound.");
                yield break; // Exit the coroutine if SoundManager is not available
            }

            try
            {
                SoundManager.Instance.PlayOneShot(sirenGroupIndex, sirenClipIndex);
                Debug.Log($"사이렌 사운드 재생 시도: Group Index {sirenGroupIndex}, Clip Index {sirenClipIndex}");
            }
            catch (System.Exception e)
            {
                Debug.LogError("사이렌 사운드 재생 중 오류 발생: " + e.Message);
            }

            yield return EnableVignetteEffect();

            yield return FlashingSubtitle("건물에 화재가 발생했습니다!", 0.6f);
            hasFireRecognized = true;
            SequenceManager.Instance.CompleteStep(0); // 상황 인지 완료

            ShowSubtitle("화재 발생 시, 가장 먼저 젖은 옷이나 수건 등의 천으로 호흡기를 보호하는 것이 중요합니다.");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("주변에서 호흡 보호에 사용할 천을 찾아보세요!");
            yield return new WaitForSeconds(textDelay);

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
                ShowSubtitle("먼저 경보를 듣고 상황을 파악하세요!");
                SequenceManager.Instance.RecordSequenceError(1); // 호흡 보호 순서 오류
                return;
            }

            if (!hasRagGrabbed)
            {
                hasRagGrabbed = true;
                ShowSubtitle("잘했습니다! 이제 천을 물에 적셔야 합니다!");
                isSearchingForWater = true;
                searchTimer = waterSearchTime;
                SequenceManager.Instance.CompleteStep(1); // 호흡 보호 완료
                Debug.Log("천을 집었습니다. hasRagGrabbed: " + hasRagGrabbed);
            }
        }

        void Update()
        {
            if (isSearchingForWater)
            {
                searchTimer -= Time.deltaTime;
                if (searchTimer <= 0)
                {
                    HighlightWater();
                    isSearchingForWater = false;
                }
            }

            // 물에 천을 넣었는지 콜라이더 겹침으로 확인
            if (!hasRagWetted && hasRagGrabbed && ragObject != null && waterObject != null)
            {
                Collider ragCollider = ragObject.GetComponent<Collider>();
                Collider waterCollider = waterObject.GetComponent<Collider>();
                if (ragCollider != null && waterCollider != null && ragCollider.bounds.Intersects(waterCollider.bounds))
                {
                    if (!isWetting)
                    {
                        isSearchingForWater = false;
                        isWetting = true;
                        wetTimer = 0f;
                        ShowSubtitle("잘했습니다! 충분히 천을 물에 적셔주세요!");
                        ShowSubtitle("잘했습니다! 이제 젖은 천을 호흡기에 갖다 대어 호흡을 보호합시다!");
                    }
                }
                else if (isWetting)
                {
                    isWetting = false;
                    ShowSubtitle("천을 충분히 물에 적셔야 합니다!");
                    Debug.Log("천이 물에서 나옴. isWetting: false");
                }
            }

            if (isWetting)
            {
                wetTimer += Time.deltaTime;
                if (wetTimer >= wetTimeRequired && !hasRagWetted)
                {
                    WetRag();
                    isWetting = false;
                }
            }

            if (headTransform != null && ragObject != null)
            {
                float distanceToHead = Vector3.Distance(ragObject.transform.position, headTransform.position);
                if (!isProtecting && distanceToHead <= protectionDistance)
                {
                    if (!hasRagGrabbed)
                    {
                        ShowSubtitle("먼저 천을 집어주세요!");
                        SequenceManager.Instance.RecordSequenceError(1);
                        return;
                    }
                    if (!hasRagWetted)
                    {
                        ShowSubtitle("먼저 천을 물에 적셔야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(1);
                        return;
                    }
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
                        ShowSubtitle("먼저 천을 집어주세요!");
                        SequenceManager.Instance.RecordSequenceError(2); // 문 탈출 순서 오류
                        return;
                    }
                    if (!hasRagWetted)
                    {
                        ShowSubtitle("먼저 천을 물에 적셔야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(2);
                        return;
                    }
                    if (!hasProtectionActivated)
                    {
                        ShowSubtitle("먼저 호흡 보호를 위해 젖은 천을 입에 대세요!");
                        SequenceManager.Instance.RecordSequenceError(2);
                        return;
                    }
                    hasReachedFrontDoor = true;
                    SequenceManager.Instance.CompleteStep(2); // 문 탈출 완료
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
                        ShowSubtitle("먼저 문 손잡이의 온도를 확인하고 문을 열어야 합니다!");
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
            if (subtitleText != null)
            {
                subtitleText.color = Color.black;
                subtitleText.text = message;
                Debug.Log($"자막 표시: {message}");
            }
            else
            {
                Debug.LogWarning("subtitleText가 null입니다!");
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
            ShowSubtitle("대피 시 문을 열기 전 손잡이의 온도를 먼저 확인해야 합니다!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("문 손잡이가 뜨거우면 반대편에 불이 있을 수 있으니, 다른 경로로 대피해야 합니다.");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("이번 훈련은 안전한 상황을 전제로 진행됩니다. 문을 열고 탈출하세요!");
            yield return new WaitForSeconds(textDelay);

            if (doorGrabInteractable != null)
            {
                doorGrabInteractable.enabled = true;
                Debug.Log("✅ 문 상호작용 활성화.");
            }
        }

        IEnumerator BackDoorSequence()
        {
            ShowSubtitle("잘했습니다! 방을 나왔습니다!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("이제 건물 밖으로 대피해봅시다.");
            yield return new WaitForSeconds(textDelay);
        }

        void WetRag()
        {
            hasRagWetted = true;
            if (cubeRenderer != null)
            {
                cubeRenderer.material.color = Color.green;
            }
            ShowSubtitle("잘했습니다! 이제 젖은 천을 호흡기 주변에 대보세요!");
            Debug.Log("천이 젖음. hasRagWetted: true");
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
            ShowSubtitle("물이 있는 곳을 찾아 천을 적셔봅시다!");
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
                    ShowSubtitle("잘했어요! 이제 유독가스로부터 호흡기를 보호할 수 있습니다!");
                    hasShownProtectionMessage = true;
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
            ShowSubtitle("다음은 탈출을 위해 출입문을 찾아봅시다!");
        }

        public void SetWaterSearchTime(float time)
        {
            waterSearchTime = time;
            searchTimer = waterSearchTime;
        }
    }
}