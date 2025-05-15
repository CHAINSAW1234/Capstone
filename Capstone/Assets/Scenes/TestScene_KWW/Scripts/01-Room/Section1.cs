using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace FireEvacuation
{
    public class Section1 : MonoBehaviour
    {
        public enum Mode { Study = 0, Evaluation = 1, NULL };
        private Mode currentMode;
        private bool isPracticeMode = true;

        [Header("설정")]
        [SerializeField] private float textDelay = 5f;
        [SerializeField] private float protectionDistance = 0.2f;
        [SerializeField] private float requiredTouchTime = 3f;

        [Header("오브젝트")]
        [SerializeField] private GameObject ragObject;
        [SerializeField] private GameObject[] waterObjects = new GameObject[2];
        [SerializeField] private GameObject doorObject;
        [SerializeField] private Collider doorHandle1;
        [SerializeField] private Collider doorHandle2;
        [SerializeField] private GameObject frontDoorTrigger;
        [SerializeField] private GameObject backDoorTrigger;
        [SerializeField] private Transform headTransform;

        [Header("UI")]
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text FeedbackAlarm;
        [SerializeField] private TMP_Text FeedbackProtection;
        [SerializeField] private TMP_Text FeedbackDoor;

        [Header("화살표")]
        [SerializeField] private GameObject WaterArrow1;
        [SerializeField] private GameObject WaterArrow2;
        [SerializeField] private GameObject DoorArrow;
        [SerializeField] private GameObject NextSectionArrow;

        [Header("아웃라인")]
        [SerializeField] private GameObject outlineRagObject;
        [SerializeField] private GameObject outlineDoorObject;
        [SerializeField] private GameObject outlineDoorHandleObject;
        [SerializeField] private Color outlineColor = Color.red;
        [SerializeField] private float outlineWidth = 2f;

        [Header("후처리")]
        [SerializeField] private Volume globalVolume;

        [Header("햅틱")]
        [SerializeField] private XRBaseController leftHandController;
        [SerializeField] private XRBaseController rightHandController;
        [SerializeField] private float vibrationIntensity = 0.8f;
        [SerializeField] private float vibrationDuration = 0.1f;


        private Vignette vignette;
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable ragGrabInteractable;
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable doorGrabInteractable;
        private Rigidbody doorRb;
        private HingeJoint hingeJoint;
        private Renderer ragRenderer;

        private bool hasFireRecognized;
        private bool hasRagGrabbed;
        private static bool hasRagWetted;
        private bool hasProtectionActivated;
        private bool hasReachedFrontDoor;
        private bool hasReachedBackDoor;
        private bool isSearchingForWater;
        private bool isWetting;
        private bool isProtecting;
        private bool isHandleTouched;
        private bool isHandleSafe;
        private float searchTimer;
        private float wetTimer;
        private float handleTouchTime;

        private void Start()
        {
            hasRagWetted = false;
            SetMode();
            InitializeComponents();
            StartCoroutine(DrillSequence());
        }

        void SetMode()
        {
            // PlayerPrefs에서 mode 값을 직접 읽어옴
            int modeValue = PlayerPrefs.GetInt("mode", (int)Mode.NULL); // 기본값은 NULL
            currentMode = (Mode)modeValue;

            // 모드에 따라 다른 동작 수행
            if (currentMode == Mode.Study)
            {
                isPracticeMode = true;
            }
            else if (currentMode == Mode.Evaluation)
            {
                isPracticeMode = false;
            }
            else
            {
                Debug.Log("모드가 설정되지 않았습니다.");
            }
        }

        private void InitializeComponents()
        {
            // 천 초기화
            if (ragObject)
            {
                ragGrabInteractable = ragObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() ?? ragObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                ragGrabInteractable.selectEntered.AddListener(OnRagGrabbed);

                ragRenderer = ragObject.GetComponent<Renderer>();
                if (ragRenderer)
                {
                    ragRenderer.material = new Material(ragRenderer.material) { color = Color.red };
                }

                if (!ragObject.GetComponent<Collider>()) ragObject.AddComponent<BoxCollider>();
                if (!ragObject.GetComponent<Rigidbody>())
                {
                    var rb = ragObject.AddComponent<Rigidbody>();
                    rb.useGravity = true;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                }
            }

            // 물 오브젝트 초기화
            foreach (var water in waterObjects)
            {
                if (!water) continue;
                var collider = water.GetComponent<Collider>() ?? water.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                water.tag = "Water";
            }

            // 문 초기화
            SetupDoor();

            // 후처리 초기화
            if (globalVolume && globalVolume.profile.TryGet(out vignette))
            {
                vignette.active = false;
                vignette.intensity.Override(0f);
            }

            // 화살표 초기화
            WaterArrow1?.SetActive(false);
            WaterArrow2?.SetActive(false);
            DoorArrow?.SetActive(false);
            NextSectionArrow?.SetActive(false);

            // UI 초기화
            if (!isPracticeMode && subtitleText) subtitleText.gameObject.SetActive(false);

            // 헤드 트랜스폼 초기화
            headTransform ??= GameObject.Find("Main Camera")?.transform;
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

        private IEnumerator DrillSequence()
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
            else
            {
                yield return new WaitForSeconds(textDelay * 2);
            }

            // 사이렌 재생
            if (SoundManager.Instance)
            {
                SoundManager.Instance.PlayOneShot(0, 0);
            }

            yield return EnableVignetteEffect();

            if (isPracticeMode)
            {
                ShowSubtitle("건물에 화재가 발생했습니다!");
                yield return new WaitForSeconds(textDelay);
                ShowSubtitle("화재 발생 시, 호흡기를 보호하는 것이 중요합니다.");
                yield return new WaitForSeconds(textDelay);
                ShowSubtitle("호흡 보호에 사용할 천을 찾아보세요!");
                if (outlineRagObject) AddOutline(outlineRagObject);
            }

            hasFireRecognized = true;
            SequenceManager.Instance.CompleteStep(0);
        }

        private void OnRagGrabbed(SelectEnterEventArgs args)
        {
            if (!hasFireRecognized)
            {
                if (isPracticeMode) ShowSubtitle("먼저 화재 상황을 인지해야 합니다!");
                SequenceManager.Instance.RecordSequenceError(0);
                RecordFeedback(FeedbackAlarm, "화재 상황을 충분히 인지하지 않았습니다.");
                return;
            }

            if (!hasRagGrabbed)
            {
                hasRagGrabbed = true;
                if (outlineRagObject) RemoveOutline(outlineRagObject);
                StartCoroutine(WaterSequence());
            }
        }

        private void Update()
        {

            if (!hasRagWetted && hasRagGrabbed && ragObject)
            {
                var ragCollider = ragObject.GetComponent<Collider>();
                if (ragCollider)
                {
                    foreach (var water in waterObjects)
                    {
                        if (!water) continue;
                        var waterCollider = water.GetComponent<Collider>();
                        if (waterCollider && ragCollider.bounds.Intersects(waterCollider.bounds))
                        {
                            if (!isWetting)
                            {
                                isWetting = true;
                                StartCoroutine(WetRagSequence());
                            }
                            break;
                        }
                    }
                }
            }

            if (headTransform && ragObject)
            {
                float distanceToHead = Vector3.Distance(ragObject.transform.position, headTransform.position);
                if (!isProtecting && distanceToHead <= protectionDistance)
                {
                    if (!hasRagWetted)
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 천을 물에 적셔야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(1);
                        RecordFeedback(FeedbackProtection, "호흡 보호 과정에서 절차가 틀렸습니다.(물 적시기)");
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

        private void CheckTriggerCollision()
        {
            if (!headTransform) return;

            if (!hasReachedFrontDoor)
            {
                var frontTriggerCollider = frontDoorTrigger.GetComponent<Collider>();
                if (frontTriggerCollider && frontTriggerCollider.bounds.Contains(headTransform.position))
                {
                    if (!hasProtectionActivated)
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 호흡 보호를 위해 젖은 천을 입에 대세요!");
                        SequenceManager.Instance.RecordSequenceError(1);
                        RecordFeedback(FeedbackProtection, "호흡 보호 절차를 생략했습니다.");
                        return;
                    }
                    if (DoorArrow) DoorArrow.SetActive(false);
                    hasReachedFrontDoor = true;
                    SequenceManager.Instance.CompleteStep(2);
                    StartCoroutine(FrontDoorSequence());
                }
            }

            if (!hasReachedBackDoor)
            {
                var backTriggerCollider = backDoorTrigger.GetComponent<Collider>();
                if (backTriggerCollider && backTriggerCollider.bounds.Contains(headTransform.position))
                {
                    if (!hasReachedFrontDoor)
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 문 손잡이의 온도를 확인하고 문을 열어야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(2);
                        RecordFeedback(FeedbackDoor, "문 손잡이의 온도를 확인하지 않았습니다.");
                        return;
                    }
                    hasReachedBackDoor = true;
                    StartCoroutine(BackDoorSequence());
                }
            }
        }

        private void ShowSubtitle(string message)
        {
            if (subtitleText && isPracticeMode)
            {
                subtitleText.color = Color.black;
                subtitleText.text = message;
            }
        }

        private void TriggerHapticFeedback()
        {
            leftHandController?.SendHapticImpulse(vibrationIntensity, vibrationDuration);
            rightHandController?.SendHapticImpulse(vibrationIntensity, vibrationDuration);
        }

        private IEnumerator EnableVignetteEffect()
        {
            if (!vignette) yield break;

            float duration = 1.5f;
            float timer = 0f;
            float target = 0.70f;

            vignette.active = true;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                vignette.intensity.value = Mathf.Lerp(0f, target, timer / duration);
                yield return null;
            }

            vignette.intensity.Override(target);
        }

        private IEnumerator FrontDoorSequence()
        {
            if (isPracticeMode)
            {
                ShowSubtitle("문 손잡이가 뜨거우면 반대편에 불이 있을 수 있으니, 다른 경로로 대피해야 합니다.");
                yield return new WaitForSeconds(textDelay);

                ShowSubtitle("맨 손으로 온도를 확인하는 것은 위험합니다! 젖은 천을 이용해서 확인해봅시다.");
                if (outlineDoorHandleObject) AddOutline(outlineDoorHandleObject);
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
        }

        private IEnumerator WaterSequence()
        {
            if (isPracticeMode)
            {
                ShowSubtitle("잘했습니다! 이제 천을 물에 적셔야 합니다!");
                yield return new WaitForSeconds(textDelay);
                ShowSubtitle("물이 있는 곳을 찾아 천을 적셔봅시다!");
                WaterArrow1?.SetActive(true);
                WaterArrow2?.SetActive(true);
                yield return new WaitForSeconds(textDelay);
            }
        }

        private IEnumerator BackDoorSequence()
        {
            if (isPracticeMode)
            {
                ShowSubtitle("잘했습니다! 방을 나왔습니다!");
                yield return new WaitForSeconds(textDelay);
                ShowSubtitle("이제 안전하게 건물 밖으로 대피하는 법을 배워봅시다.");
                if (NextSectionArrow) NextSectionArrow.SetActive(true);
            }
        }

        private IEnumerator WetRagSequence()
        {
            hasRagWetted = true;
            if (isPracticeMode)
            {
                if (WaterArrow1) WaterArrow1.SetActive(false);
                if (WaterArrow2) WaterArrow2.SetActive(false);
                ShowSubtitle("잘했습니다! 충분히 천을 물에 적셔주세요!");
                yield return new WaitForSeconds(textDelay);
                ShowSubtitle("이제 젖은 천을 호흡기에 갖다 대어 호흡을 보호합시다!");
            }
        }

        private void AddOutline(GameObject target)
        {
            if (!target || !isPracticeMode) return;

            var outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
            outline.OutlineMode = Outline.Mode.OutlineAll;
            outline.OutlineColor = outlineColor;
            outline.OutlineWidth = outlineWidth;
            outline.enabled = true;
        }

        private void RemoveOutline(GameObject target)
        {
            if (!target) return;
            if (target.TryGetComponent<Outline>(out var outline))
            {
                outline.enabled = false;
            }
        }

        private void SetProtectionState(bool state)
        {
            if (isProtecting == state) return;

            isProtecting = state;
            if (isProtecting)
            {
                if (!hasProtectionActivated)
                {
                    hasProtectionActivated = true;
                    if (isPracticeMode)
                    {
                        ShowSubtitle("이제 안전하게 호흡이 가능해졌습니다!");
                        StartCoroutine(AfterWaterSequence());
                    }
                }
                if (vignette) vignette.intensity.Override(0f);
            }
            else if (vignette)
            {
                vignette.intensity.Override(0.9f);
            }
        }

        private IEnumerator AfterWaterSequence()
        {
            yield return new WaitForSeconds(textDelay);
            if (isPracticeMode)
            {
                ShowSubtitle("다음으로는 출입문을 찾아봅시다!");
                if (DoorArrow) DoorArrow.SetActive(true);
                if (outlineDoorObject) AddOutline(outlineDoorObject);
            }
        }

        private bool IsRagTouchingHandle()
        {
            if (!ragObject || (!doorHandle1 && !doorHandle2)) return false;
            var ragCollider = ragObject.GetComponent<Collider>();
            if (!ragCollider) return false;

            return (doorHandle1 && ragCollider.bounds.Intersects(doorHandle1.bounds)) ||
                   (doorHandle2 && ragCollider.bounds.Intersects(doorHandle2.bounds));
        }

        private void RecordFeedback(TMP_Text feedbackText, string message)
        {
            if (feedbackText) feedbackText.text = message;
        }
    }
}