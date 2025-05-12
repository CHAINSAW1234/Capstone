using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace FireEvacuation
{
#pragma warning disable 0618

    public class Section2 : MonoBehaviour
    {
        [Header("모드 설정")]
        [SerializeField] private bool isPracticeMode = true; // Inspector에서 연습/평가 모드 토글

        [Header("아웃라인 설정")]
        public Color outlineColor = Color.green; // 아웃라인 색상 (기본값: 빨간색)
        public float outlineWidth = 2f; // 아웃라인 두께
        public GameObject outlineEvacuationMap; // 아웃라인 적용할 안내도 오브젝트
        public GameObject outlineFireAlarmButton; // 아웃라인 적용할 화재 경보 버튼 오브젝트
        public GameObject outlineEmergencyDoor; // 아웃라인 적용할 비상문 오브젝트

        [Header("화살표 설정")]
        public GameObject BeforeSectionArrow; // 첫 번째 화살표 오브젝트
        public GameObject SmokeArriveArrow; // 두 번째 화살표 오브젝트
        public GameObject EmergencyArrow; // 두 번째 화살표 오브젝트
        public GameObject EndArrow; // 두 번째 화살표 오브젝트
        public GameObject ButtonArrow; // 버튼 오브젝트

        [Header("UI 설정")]
        public TMP_Text subtitleText;
        public float textDelay = 5f;

        [Header("후처리 효과")]
        public Volume globalVolume;
        private Vignette vignette;

        [Header("시작 트리거 설정")]
        public GameObject startTrigger;

        [Header("안내도 설정")]
        public GameObject evacuationMap;

        [Header("버튼 설정")]
        public GameObject fireAlarmButton;
        public Transform buttonTriggerTransform;
        public float pressDistance = 0.05f;
        public float returnSpeed = 5f;

        [Header("연기 트리거 설정")]
        public GameObject smokeTrigger;
        public List<ParticleSystem> smokeEffects;
        public GameObject smokeArrivalTrigger;

        [Header("Elevator Trigger 설정")]
        public GameObject elevatorTrigger;
        private bool hasEnteredElevator = false;

        [Header("포복 트리거 설정")]
        public GameObject crawlingTrigger;
        private Collider crawlingTriggerCollider;
        private bool isInsideCrawlingTrigger = false;

        private GameObject emergencyDoor;

        [Header("비상문 설정")]
        public GameObject doorTrigger;
        public GameObject exitTrigger;
        private float pushForce = 10f;
        private float triggerDistance = 0.3f;

        [Header("Player Head Transform")]
        public Transform headTransform;

        [Header("사운드 설정")]
        public bool playSoundOnButtonPress = true;
        public int soundGroupIndex = 0;
        public int soundClipIndex = 0;
        public bool loopSound = false;

        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable buttonInteractable;
        private Rigidbody buttonRb;
        private BoxCollider buttonCollider;
        private BoxCollider buttonTriggerCollider;
        private Vector3 buttonInitialPosition;
        private bool isButtonPressed = false;
        private bool isButtonTriggerActivated = false;

        private Rigidbody doorRb;
        private HingeJoint hingeJoint;
        private BoxCollider doorTriggerCollider;
        private bool isDoorEnabled = false;
        private bool isDoorOpening = false;

        private bool hasStartedSequence = false;
        private bool hasHighlightedMap = false;
        private bool hasActivatedAlarm = false;
        private bool hasEnteredSmokeArea = false;
        private bool hasCrawled = false;
        private bool hasReachedEmergencyDoor = false;
        private bool hasCompletedEvacuation = false;

        [Header("이벤트 설정")]
        [SerializeField] public UnityEvent onFireAlarmActivated;
        [SerializeField] public UnityEvent onCrawlingStarted;
        [SerializeField] public UnityEvent onEmergencyDoorOpened;

        private void Start()
        {
            SetupButton();
            InitPostProcessing();
            SetupEvacuationMap();
            SetupSmokeTrigger();
            SetupEmergencyDoor();
            SetupStartTrigger();

            // 화살표 초기 비활성화
            if (BeforeSectionArrow != null) BeforeSectionArrow.SetActive(false);
            if (SmokeArriveArrow != null) SmokeArriveArrow.SetActive(false);
            if (EmergencyArrow != null) EmergencyArrow.SetActive(false);
            if (EndArrow != null) EndArrow.SetActive(false);
            if (ButtonArrow != null) ButtonArrow.SetActive(false);

            if (headTransform == null)
            {
                headTransform = Camera.main?.transform;
                if (headTransform == null)
                {
                    //Debug.LogError("Head Transform (Main Camera)을 찾을 수 없습니다!");
                }
            }

            if (!isPracticeMode)
            {
                if (subtitleText != null)
                {
                    subtitleText.gameObject.SetActive(false);
                }
            }
        }

        void SetupStartTrigger()
        {
            if (startTrigger == null)
            {
                //Debug.LogError("시작 트리거 오브젝트가 지정되지 않았습니다!");
                return;
            }

            Collider startTriggerCollider = startTrigger.GetComponent<Collider>();
            if (startTriggerCollider == null)
            {
                startTriggerCollider = startTrigger.AddComponent<BoxCollider>();
            }
            startTriggerCollider.isTrigger = true;
        }

        void SetupButton()
        {
            if (fireAlarmButton == null)
            {
                //Debug.LogError("화재 경보 버튼 오브젝트가 지정되지 않았습니다!");
                return;
            }

            buttonInteractable = fireAlarmButton.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            if (buttonInteractable == null)
            {
                buttonInteractable = fireAlarmButton.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            }

            buttonRb = fireAlarmButton.GetComponent<Rigidbody>();
            if (buttonRb == null)
            {
                buttonRb = fireAlarmButton.AddComponent<Rigidbody>();
            }
            buttonRb.isKinematic = true;
            buttonRb.useGravity = false;

            buttonCollider = fireAlarmButton.GetComponent<BoxCollider>();
            if (buttonCollider == null)
            {
                buttonCollider = fireAlarmButton.AddComponent<BoxCollider>();
            }

            if (buttonTriggerTransform != null)
            {
                buttonTriggerCollider = buttonTriggerTransform.GetComponent<BoxCollider>();
                if (buttonTriggerCollider == null)
                {
                    buttonTriggerCollider = buttonTriggerTransform.gameObject.AddComponent<BoxCollider>();
                }
                buttonTriggerCollider.isTrigger = true;
            }

            buttonInitialPosition = fireAlarmButton.transform.localPosition;

            buttonInteractable.hoverEntered.AddListener(OnButtonHoverEnter);
            buttonInteractable.hoverExited.AddListener(OnButtonHoverExit);

            //Debug.Log("✅ 화재 경보 버튼 설정 완료.");
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
                //Debug.LogError("Global Volume이 지정되지 않았거나 Vignette 설정을 찾을 수 없습니다!");
            }
        }

        void SetupEvacuationMap()
        {
            if (evacuationMap == null)
            {
                Debug.LogError("탈출 경로 안내도 오브젝트가 지정되지 않았습니다!");
                return;
            }
        }

        void SetupSmokeTrigger()
        {
            if (smokeTrigger == null)
            {
                //Debug.LogError("연기 트리거 오브젝트가 지정되지 않았습니다!");
                return;
            }

            Collider triggerCollider = smokeTrigger.GetComponent<Collider>();
            if (triggerCollider == null)
            {
                triggerCollider = smokeTrigger.AddComponent<BoxCollider>();
            }
            triggerCollider.isTrigger = true;

            if (crawlingTrigger == null)
            {
                //Debug.LogError("포복 트리거 오브젝트가 지정되지 않았습니다!");
                return;
            }

            crawlingTriggerCollider = crawlingTrigger.GetComponent<Collider>();
            if (crawlingTriggerCollider == null)
            {
                crawlingTriggerCollider = crawlingTrigger.AddComponent<BoxCollider>();
            }
            crawlingTriggerCollider.isTrigger = true;

            if (smokeArrivalTrigger == null)
            {
                //Debug.LogError("연기 도착 트리거 오브젝트가 지정되지 않았습니다!");
                return;
            }

            Collider arrivalTriggerCollider = smokeArrivalTrigger.GetComponent<Collider>();
            if (arrivalTriggerCollider == null)
            {
                arrivalTriggerCollider = smokeArrivalTrigger.AddComponent<BoxCollider>();
            }
            arrivalTriggerCollider.isTrigger = true;

            if (smokeEffects != null && smokeEffects.Count > 0)
            {
                foreach (var smokeEffect in smokeEffects)
                {
                    if (smokeEffect != null)
                    {
                        smokeEffect.Stop();
                    }
                }
            }
        }

        void SetupEmergencyDoor()
        {
            if (emergencyDoor == null)
            {
                //Debug.LogError("비상문 오브젝트가 지정되지 않았습니다!");
                return;
            }

            doorRb = emergencyDoor.GetComponent<Rigidbody>();
            if (doorRb == null)
            {
                doorRb = emergencyDoor.AddComponent<Rigidbody>();
            }
            doorRb.mass = 1f;
            doorRb.angularDamping = 0.05f;
            doorRb.useGravity = true;
            doorRb.isKinematic = false;
            doorRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            hingeJoint = emergencyDoor.GetComponent<HingeJoint>();
            if (hingeJoint == null)
            {
                hingeJoint = emergencyDoor.AddComponent<HingeJoint>();
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

            doorTriggerCollider = emergencyDoor.GetComponent<BoxCollider>();
            if (doorTriggerCollider == null)
            {
                doorTriggerCollider = emergencyDoor.AddComponent<BoxCollider>();
            }
            doorTriggerCollider.size = new Vector3(0.5f, 1f, triggerDistance);
            doorTriggerCollider.center = new Vector3(0, 0, 0.2f);
            doorTriggerCollider.isTrigger = true;

            if (doorTrigger == null)
            {
                //Debug.LogError("비상문 트리거 오브젝트가 지정되지 않았습니다!");
                return;
            }

            Collider doorApproachTriggerCollider = doorTrigger.GetComponent<Collider>();
            if (doorApproachTriggerCollider == null)
            {
                doorApproachTriggerCollider = doorTrigger.AddComponent<BoxCollider>();
            }
            doorApproachTriggerCollider.isTrigger = true;

            if (exitTrigger == null)
            {
                //Debug.LogError("비상문 반대편 트리거 오브젝트가 지정되지 않았습니다!");
                return;
            }

            Collider exitTriggerCollider = exitTrigger.GetComponent<Collider>();
            if (exitTriggerCollider == null)
            {
                exitTriggerCollider = exitTrigger.AddComponent<BoxCollider>();
            }
            exitTriggerCollider.isTrigger = true;

            //Debug.Log("✅ 비상문 설정 완료.");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isDoorEnabled || isDoorOpening)
            {
                return;
            }

            if (other.CompareTag("Hand") && other.gameObject.transform.position.z > emergencyDoor.transform.position.z)
            {
                if (!hasReachedEmergencyDoor)
                {
                    if (!SequenceManager.Instance.IsStepCompleted(3))
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 탈출 경로 안내도를 확인해야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(3);
                        return;
                    }
                    if (!SequenceManager.Instance.IsStepCompleted(4))
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 화재 경보 버튼을 눌러주세요!");
                        SequenceManager.Instance.RecordSequenceError(4);
                        return;
                    }
                    if (!SequenceManager.Instance.IsStepCompleted(5))
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 연기 구역에서 포복으로 이동해야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(5);
                        return;
                    }
                }

                OpenDoor();
            }
        }

        void OpenDoor()
        {
            if (doorRb != null && !isDoorOpening)
            {
                isDoorOpening = true;
                Vector3 pushDirection = -emergencyDoor.transform.right;
                doorRb.AddForceAtPosition(pushForce * pushDirection, emergencyDoor.transform.position, ForceMode.Impulse);
                onEmergencyDoorOpened?.Invoke();
                if (outlineEmergencyDoor != null) RemoveOutline(outlineEmergencyDoor);
                //Debug.Log("✅ 문이 손으로 밀려 열림.");
            }
        }

        void OnButtonHoverEnter(HoverEnterEventArgs args)
        {
            if (!SequenceManager.Instance.IsStepCompleted(3))
            {
                if (isPracticeMode) ShowSubtitle("먼저 탈출 경로 안내도를 확인해야 합니다!");
                SequenceManager.Instance.RecordSequenceError(3);
                return;
            }
            isButtonPressed = true;
            if (isPracticeMode) AddOutline(outlineFireAlarmButton);
        }

        void OnButtonHoverExit(HoverExitEventArgs args)
        {
            isButtonPressed = false;
            isButtonTriggerActivated = false;
            if (isPracticeMode) RemoveOutline(outlineFireAlarmButton);
        }

        void Update()
        {
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

            CheckTriggerCollision();
            CheckElevatorTrigger();
        }

        void CheckElevatorTrigger()
        {
            if (hasEnteredElevator || elevatorTrigger == null || headTransform == null) return;

            Collider elevatorTriggerCollider = elevatorTrigger.GetComponent<Collider>();
            if (elevatorTriggerCollider != null && elevatorTriggerCollider.bounds.Contains(headTransform.position))
            {
                hasEnteredElevator = true;
                StartCoroutine(ElevatorSequence());
            }
        }

        void CheckButtonTriggerCollision()
        {
            if (buttonTriggerCollider == null) return;

            if (buttonCollider.bounds.Intersects(buttonTriggerCollider.bounds))
            {
                if (!isButtonTriggerActivated)
                {
                    if (!SequenceManager.Instance.IsStepCompleted(3))
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 탈출 경로 안내도를 확인해야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(3);
                        return;
                    }
                    onFireAlarmActivated?.Invoke();
                    isButtonTriggerActivated = true;
                    hasActivatedAlarm = true;
                    SequenceManager.Instance.CompleteStep(4);
                    if (outlineFireAlarmButton != null) RemoveOutline(outlineFireAlarmButton);
                    if (isPracticeMode) ShowSubtitle("화재 경보가 활성화되었습니다! 이제 안전하게 이동해봅시다!");
                    if (ButtonArrow != null) ButtonArrow.SetActive(false);

                    if (playSoundOnButtonPress)
                    {
                        if (SoundManager.Instance == null)
                        {
                            //Debug.LogError("SoundManager.Instance is null! Cannot play sound.");
                            return;
                        }
                        try
                        {
                            SoundManager.Instance.PlayOneShot(soundGroupIndex, soundClipIndex);
                        }
                        catch (System.Exception e)
                        {
                            //Debug.LogError("사운드 재생 중 오류 발생: " + e.Message);
                        }
                    }
                }
            }
        }

        void CheckTriggerCollision()
        {
            if (headTransform == null) return;

            if (!hasStartedSequence && startTrigger != null)
            {
                Collider startTriggerCollider = startTrigger.GetComponent<Collider>();
                if (startTriggerCollider != null && startTriggerCollider.bounds.Contains(headTransform.position))
                {
                    hasStartedSequence = true;
                    StartCoroutine(EvacuationSequence());
                }
            }

            if (!hasEnteredSmokeArea && smokeTrigger != null)
            {
                Collider smokeTriggerCollider = smokeTrigger.GetComponent<Collider>();
                if (smokeTriggerCollider != null && smokeTriggerCollider.bounds.Contains(headTransform.position))
                {
                    if (!SequenceManager.Instance.IsStepCompleted(3))
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 탈출 경로 안내도를 확인해야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(3);
                        return;
                    }
                    if (!SequenceManager.Instance.IsStepCompleted(4))
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 화재 경보 버튼을 눌러주세요!");
                        SequenceManager.Instance.RecordSequenceError(4);
                        return;
                    }
                    hasEnteredSmokeArea = true;
                    StartCoroutine(SmokeSequence());
                }
            }

            if (!hasCrawled && smokeArrivalTrigger != null)
            {
                Collider arrivalTriggerCollider = smokeArrivalTrigger.GetComponent<Collider>();
                if (arrivalTriggerCollider != null && arrivalTriggerCollider.bounds.Contains(headTransform.position))
                {
                    if (!hasEnteredSmokeArea)
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 연기 구역에 들어가 포복으로 이동해야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(5);
                        return;
                    }
                    hasCrawled = true;
                    onCrawlingStarted?.Invoke();
                    SequenceManager.Instance.CompleteStep(5);
                    if (isPracticeMode)
                    {
                        ShowSubtitle("잘했어요! 포복을 완료했습니다. 이제 비상문으로 이동해봅시다!");
                        if (SmokeArriveArrow != null) SmokeArriveArrow.SetActive(false);
                        if (EmergencyArrow != null) EmergencyArrow.SetActive(true);
                    }
                }
            }

            if (!hasReachedEmergencyDoor && doorTrigger != null)
            {
                Collider doorApproachTriggerCollider = doorTrigger.GetComponent<Collider>();
                if (doorApproachTriggerCollider != null && doorApproachTriggerCollider.bounds.Contains(headTransform.position))
                {
                    if (!SequenceManager.Instance.IsStepCompleted(3))
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 탈출 경로 안내도를 확인해야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(3);
                        return;
                    }
                    if (!SequenceManager.Instance.IsStepCompleted(4))
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 화재 경보 버튼을 눌러주세요!");
                        SequenceManager.Instance.RecordSequenceError(4);
                        return;
                    }
                    if (!SequenceManager.Instance.IsStepCompleted(5))
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 연기 구역에서 포복으로 이동해야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(5);
                        return;
                    }
                    hasReachedEmergencyDoor = true;
                    SequenceManager.Instance.CompleteStep(6);
                    StartCoroutine(EmergencyDoorSequence());
                }
            }

            if (!hasCompletedEvacuation && exitTrigger != null)
            {
                Collider exitTriggerCollider = exitTrigger.GetComponent<Collider>();
                if (exitTriggerCollider != null && exitTriggerCollider.bounds.Contains(headTransform.position))
                {
                    if (!hasReachedEmergencyDoor)
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 비상문을 열고 통과해야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(6);
                        return;
                    }
                    hasCompletedEvacuation = true;
                    if (isPracticeMode)
                    {
                        ShowSubtitle("축하합니다 모든 화재 대피 훈련이 완료되었습니다! 이제 밖으로 대피하여 훈련을 완료하세요.");
                        if (EndArrow != null) EndArrow.SetActive(true);
                    }
                    onEmergencyDoorOpened?.Invoke();
                }
            }
        }

        IEnumerator EvacuationSequence()
        {
            if (isPracticeMode)
            {
                ShowSubtitle("건물 밖으로 대피 시 대피 경로를 파악하는 것이 가장 중요합니다.");
                yield return new WaitForSeconds(textDelay);

                ShowSubtitle("먼저 앞에 있는 탈출 경로 안내도를 확인하여 대피 경로를 파악하세요.");
                AddOutlineToEvacuationMap();
                yield return new WaitForSeconds(textDelay);

                ShowSubtitle("안내도를 확인한 후, 화재 경보 버튼을 눌러 주변에 위험을 알려야 합니다.");
                if (ButtonArrow != null) ButtonArrow.SetActive(true);
                yield return new WaitForSeconds(textDelay);
            }
            else
            {
                AddOutlineToEvacuationMap();
            }
        }

        IEnumerator ElevatorSequence()
        {
            if (isPracticeMode)
            {
                ShowSubtitle("화재 대피 시 엘리베이터 사용은 전원 차단의 위험이 있습니다.");
                yield return new WaitForSeconds(textDelay);
                ShowSubtitle("엘리베이터 대신 가까운 비상 계단으로 이동하세요.");
                yield return new WaitForSeconds(textDelay);
            }
        }

        IEnumerator SmokeSequence()
        {
            if (smokeEffects != null && smokeEffects.Count > 0)
            {
                foreach (var smokeEffect in smokeEffects)
                {
                    if (smokeEffect != null)
                    {
                        smokeEffect.Play();
                    }
                }
            }

            if (SoundManager.Instance != null)
            {
                try
                {
                    SoundManager.Instance.PlayOneShot(1, 0);
                }
                catch (System.Exception e)
                {
                    //Debug.LogError("연기 사운드 재생 중 오류 발생: " + e.Message);
                }
            }
            else
            {
                //Debug.LogError("SoundManager.Instance is null! 연기 사운드 재생 실패.");
            }

            if (isPracticeMode)
            {
                ShowSubtitle("화재로 인해 주변에 연기가 가득해졌습니다.");
                yield return new WaitForSeconds(textDelay);

                ShowSubtitle("화재로 발생한 연기는 독성이 있어 오래 노출되면 위험합니다!");
                yield return new WaitForSeconds(textDelay);

                ShowSubtitle("연기는 위로 올라가는 성질이 있으므로, 낮게 엎드려 포복으로 다음 위치까지 이동 해봅시다.");
                yield return new WaitForSeconds(textDelay);
                if (SmokeArriveArrow != null) SmokeArriveArrow.SetActive(true);
            }
        }

        IEnumerator EmergencyDoorSequence()
        {
            if (isPracticeMode)
            {
                if (EmergencyArrow != null) EmergencyArrow.SetActive(false);
                ShowSubtitle("비상문에 도착했습니다! 다음은 비상문 통과에 대해 배워봅시다.");
                yield return new WaitForSeconds(textDelay);

                ShowSubtitle("실제 화재 상황에서는 비상문을 막다른 길로 오해해 위험에 처하는 경우가 많습니다.");
                yield return new WaitForSeconds(textDelay);

                ShowSubtitle("비상문에 도착하면 '비상문'이라는 글을 찾아 문의 위치를 확인해야합니다.");
                yield return new WaitForSeconds(textDelay);

                ShowSubtitle("해당 문의 한 쪽을 미는 것 만으로 가볍게 문이 열립니다. 한 번 열어봅시다.");
                AddOutlineToEmergencyDoor();
                isDoorEnabled = true;
                yield return new WaitForSeconds(textDelay);
            }
            else
            {
                isDoorEnabled = true;
                AddOutlineToEmergencyDoor();
            }
        }

        void ShowSubtitle(string message)
        {
            if (subtitleText != null && isPracticeMode)
            {
                subtitleText.color = Color.black;
                subtitleText.text = message;
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
        }

        void AddOutlineToEvacuationMap()
        {
            if (outlineEvacuationMap != null && !hasHighlightedMap)
            {
                AddOutline(outlineEvacuationMap);
                hasHighlightedMap = true;
                SequenceManager.Instance.CompleteStep(3);
            }
        }

        void AddOutlineToEmergencyDoor()
        {
            if (outlineEmergencyDoor != null)
            {
                AddOutline(outlineEmergencyDoor);
            }
        }

        private void OnDestroy()
        {
            if (doorTriggerCollider != null)
            {
                Destroy(doorTriggerCollider);
            }
        }
    }
}