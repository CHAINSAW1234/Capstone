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
    public class Section2 : MonoBehaviour
    {
        public enum Mode { Study = 0, Evaluation = 1, NULL };
        private Mode currentMode;
        private bool isPracticeMode = true;


        [Header("설정")]
        [SerializeField] private float textDelay = 5f;
        [SerializeField] private float pressDistance = 0.05f;
        [SerializeField] private float returnSpeed = 5f;
        [SerializeField] private float pushForce = 10f;
        [SerializeField] private float triggerDistance = 0.3f;

        [Header("오브젝트")]
        [SerializeField] private GameObject startTrigger;
        [SerializeField] private GameObject evacuationMap;
        [SerializeField] private GameObject fireAlarmButton;
        [SerializeField] private Transform buttonTriggerTransform;
        [SerializeField] private GameObject smokeTrigger;
        [SerializeField] private GameObject smokeErrorTrigger;
        [SerializeField] private GameObject smokeArrivalTrigger;
        [SerializeField] private GameObject crawlingTrigger;
        [SerializeField] private GameObject doorTrigger;
        [SerializeField] private GameObject exitTrigger;
        [SerializeField] private List<GameObject> evacuationErrorTriggers;
        [SerializeField] private List<ParticleSystem> smokeEffects;

        [Header("아웃라인")]
        [SerializeField] private GameObject outlineEvacuationMap;
        [SerializeField] private GameObject outlineEmergencyDoor;
        [SerializeField] private Color outlineColor = Color.green;
        [SerializeField] private float outlineWidth = 2f;

        [Header("화살표")]
        [SerializeField] private GameObject BeforeSectionArrow;
        [SerializeField] private GameObject SmokeArriveArrow;
        [SerializeField] private GameObject EmergencyArrow;
        [SerializeField] private GameObject EndArrow;
        [SerializeField] private GameObject ButtonArrow;

        [Header("UI")]
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text FeedbackMap;
        [SerializeField] private TMP_Text FeedbackButton;
        [SerializeField] private TMP_Text FeedbackSmoke;
        [SerializeField] private TMP_Text FeedbackEmergency;

        [Header("플레이어")]
        [SerializeField] private Transform headTransform;

        [Header("사운드")]
        [SerializeField] private bool playSoundOnButtonPress = true;

        [Header("이벤트")]
        [SerializeField] private UnityEvent onFireAlarmActivated;
        [SerializeField] private UnityEvent onCrawlingStarted;

        private Vignette vignette;
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable buttonInteractable;
        private Rigidbody buttonRb;
        private BoxCollider buttonCollider;
        private BoxCollider buttonTriggerCollider;
        private Vector3 buttonInitialPosition;
        private Rigidbody doorRb;
        private HingeJoint hingeJoint;
        private BoxCollider doorTriggerCollider;
        private Collider crawlingTriggerCollider;

        private bool hasStartedSequence;
        private bool hasHighlightedMap;
        private bool hasActivatedAlarm;
        private bool hasEnteredSmokeArea;
        private bool hasCrawled;
        private bool hasReachedEmergencyDoor;
        private bool hasCompletedEvacuation;
        private bool hasEnteredElevator;
        private bool isButtonPressed;
        private bool isButtonTriggerActivated;
        private bool isDoorEnabled;
        private bool isDoorOpening;
        private bool hasRecordedSmokeError;
        private HashSet<int> recordedEvacuationErrorTriggers = new HashSet<int>();

        private void Start()
        {
            SetMode();
            InitializeComponents();
            if (!isPracticeMode && subtitleText) subtitleText.gameObject.SetActive(false);
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
            // Head Transform
            headTransform ??= Camera.main?.transform;

            // Arrows
            BeforeSectionArrow?.SetActive(false);
            SmokeArriveArrow?.SetActive(false);
            EmergencyArrow?.SetActive(false);
            EndArrow?.SetActive(false);
            ButtonArrow?.SetActive(false);

            // Button
            if (fireAlarmButton)
            {
                buttonInteractable = fireAlarmButton.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>() ?? fireAlarmButton.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
                buttonInteractable.hoverEntered.AddListener(OnButtonHoverEnter);
                buttonInteractable.hoverExited.AddListener(OnButtonHoverExit);

                buttonRb = fireAlarmButton.GetComponent<Rigidbody>() ?? fireAlarmButton.AddComponent<Rigidbody>();
                buttonRb.isKinematic = true;
                buttonRb.useGravity = false;

                buttonCollider = fireAlarmButton.GetComponent<BoxCollider>() ?? fireAlarmButton.AddComponent<BoxCollider>();
                buttonInitialPosition = fireAlarmButton.transform.localPosition;

                if (buttonTriggerTransform)
                {
                    buttonTriggerCollider = buttonTriggerTransform.GetComponent<BoxCollider>() ?? buttonTriggerTransform.gameObject.AddComponent<BoxCollider>();
                    buttonTriggerCollider.isTrigger = true;
                }
            }

            // Start Trigger
            if (startTrigger)
            {
                var collider = startTrigger.GetComponent<Collider>() ?? startTrigger.AddComponent<BoxCollider>();
                collider.isTrigger = true;
            }

            // Smoke Triggers
            if (smokeTrigger)
            {
                var collider = smokeTrigger.GetComponent<Collider>() ?? smokeTrigger.AddComponent<BoxCollider>();
                collider.isTrigger = true;
            }
            if (crawlingTrigger)
            {
                crawlingTriggerCollider = crawlingTrigger.GetComponent<Collider>() ?? crawlingTrigger.AddComponent<BoxCollider>();
                crawlingTriggerCollider.isTrigger = true;
            }
            if (smokeArrivalTrigger)
            {
                var collider = smokeArrivalTrigger.GetComponent<Collider>() ?? smokeArrivalTrigger.AddComponent<BoxCollider>();
                collider.isTrigger = true;
            }
            if (smokeEffects != null)
            {
                foreach (var effect in smokeEffects) effect?.Stop();
            }

            // Error Triggers
            if (smokeErrorTrigger)
            {
                var collider = smokeErrorTrigger.GetComponent<Collider>() ?? smokeErrorTrigger.AddComponent<BoxCollider>();
                collider.isTrigger = true;
            }
            if (evacuationErrorTriggers != null)
            {
                foreach (var trigger in evacuationErrorTriggers)
                {
                    if (trigger)
                    {
                        var collider = trigger.GetComponent<Collider>() ?? trigger.AddComponent<BoxCollider>();
                        collider.isTrigger = true;
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isDoorEnabled || isDoorOpening || !other.CompareTag("Hand")) return;

            if (!hasReachedEmergencyDoor && !SequenceManager.Instance.IsStepCompleted(3))
            {
                if (isPracticeMode) ShowSubtitle("먼저 탈출 경로 안내도를 확인해야 합니다!");
                SequenceManager.Instance.RecordSequenceError(3);
                RecordFeedback(FeedbackMap, "탈출 경로 안내도를 확인하지 않았습니다.");
                if (isPracticeMode) return;
            }
            if (!SequenceManager.Instance.IsStepCompleted(4))
            {
                if (isPracticeMode) ShowSubtitle("먼저 화재 경보 버튼을 눌러주세요!");
                SequenceManager.Instance.RecordSequenceError(4);
                RecordFeedback(FeedbackButton, "화재 경보 버튼을 누르지 않았습니다.");
                if (isPracticeMode) return;
            }
            isDoorOpening = true;
            if (outlineEmergencyDoor) RemoveOutline(outlineEmergencyDoor);

        }

        private void OnButtonHoverEnter(HoverEnterEventArgs args)
        {
            isButtonPressed = true;
        }

        private void OnButtonHoverExit(HoverExitEventArgs args)
        {
            isButtonPressed = false;
            isButtonTriggerActivated = false;
        }

        private void Update()
        {
            if (isButtonPressed)
            {
                var targetPosition = buttonInitialPosition + Vector3.down * pressDistance;
                fireAlarmButton.transform.localPosition = Vector3.Lerp(fireAlarmButton.transform.localPosition, targetPosition, Time.deltaTime * returnSpeed);
                CheckButtonTriggerCollision();
            }
            else if (fireAlarmButton)
            {
                fireAlarmButton.transform.localPosition = Vector3.Lerp(fireAlarmButton.transform.localPosition, buttonInitialPosition, Time.deltaTime * returnSpeed);
            }

            CheckTriggerCollision();
        }

        private void CheckButtonTriggerCollision()
        {
            if (!buttonTriggerCollider || !buttonCollider.bounds.Intersects(buttonTriggerCollider.bounds) || isButtonTriggerActivated) return;

            onFireAlarmActivated?.Invoke();
            isButtonTriggerActivated = true;
            hasActivatedAlarm = true;
            SequenceManager.Instance.CompleteStep(4);
            if (isPracticeMode) ShowSubtitle("화재 경보가 활성화되었습니다! 이제 안전하게 이동해봅시다!");
            if (ButtonArrow) ButtonArrow.SetActive(false);

            if (playSoundOnButtonPress && SoundManager.Instance)
            {
                SoundManager.Instance.PlayOneShot(0, 1);
            }
        }

        private void CheckTriggerCollision()
        {
            if (!headTransform) return;

            // Evacuation Error Triggers
            foreach (var trigger in evacuationErrorTriggers ?? new List<GameObject>())
            {
                if (!trigger) continue;
                var collider = trigger.GetComponent<Collider>();
                var triggerId = trigger.GetInstanceID();
                if (collider?.bounds.Contains(headTransform.position) == true && !recordedEvacuationErrorTriggers.Contains(triggerId))
                {
                    if (isPracticeMode) ShowSubtitle("잘못된 대피 경로 입니다! 대피도를 확인하세요.");
                    SequenceManager.Instance.RecordSequenceError(3);
                    RecordFeedback(FeedbackMap, "잘못된 탈출 경로로 이동했습니다.");
                    recordedEvacuationErrorTriggers.Add(triggerId);
                    if (isPracticeMode) return;
                }
            }

            // Smoke Error Trigger
            if (smokeErrorTrigger && !hasCrawled && !hasRecordedSmokeError)
            {
                var collider = smokeErrorTrigger.GetComponent<Collider>();
                if (collider?.bounds.Contains(headTransform.position) == true)
                {
                    if (isPracticeMode) ShowSubtitle("머리의 위치가 너무 높습니다! 포복으로 이동해주세요.");
                    SequenceManager.Instance.RecordSequenceError(5);
                    RecordFeedback(FeedbackSmoke, "포복이 충분히 낮지 않았습니다.");
                    hasRecordedSmokeError = true;
                    if (isPracticeMode) return;
                }
            }

            // Start Trigger
            if (!hasStartedSequence && startTrigger)
            {
                var collider = startTrigger.GetComponent<Collider>();
                if (collider?.bounds.Contains(headTransform.position) == true)
                {
                    hasStartedSequence = true;
                    StartCoroutine(EvacuationSequence());
                }
            }

            // Smoke Trigger
            if (!hasEnteredSmokeArea && smokeTrigger)
            {
                var collider = smokeTrigger.GetComponent<Collider>();
                if (collider?.bounds.Contains(headTransform.position) == true)
                {
                    if (!SequenceManager.Instance.IsStepCompleted(4))
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 화재 경보 버튼을 눌러주세요!");
                        SequenceManager.Instance.RecordSequenceError(4);
                        RecordFeedback(FeedbackButton, "화재 경보를 누르지 않았습니다.");
                        if (isPracticeMode) return;
                    }
                    hasEnteredSmokeArea = true;
                    StartCoroutine(SmokeSequence());
                }
            }

            // Smoke Arrival Trigger
            if (!hasCrawled && smokeArrivalTrigger)
            {
                var collider = smokeArrivalTrigger.GetComponent<Collider>();
                if (collider?.bounds.Contains(headTransform.position) == true)
                {
                    if (!SequenceManager.Instance.IsStepCompleted(4))
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 화재 경보 버튼을 눌러주세요!");
                        SequenceManager.Instance.RecordSequenceError(4);
                        RecordFeedback(FeedbackButton, "화재 경보를 누르지 않았습니다.");
                        if (isPracticeMode) return;
                    }
                    hasCrawled = true;
                    onCrawlingStarted?.Invoke();
                    SequenceManager.Instance.CompleteStep(5);
                    if (isPracticeMode)
                    {
                        ShowSubtitle("잘했어요! 포복을 완료했습니다. 이제 비상문으로 이동해봅시다!");
                        if (SmokeArriveArrow) SmokeArriveArrow.SetActive(false);
                        if (EmergencyArrow) EmergencyArrow.SetActive(true);
                    }
                }
            }

            // Door Trigger
            if (!hasReachedEmergencyDoor && doorTrigger)
            {
                var collider = doorTrigger.GetComponent<Collider>();
                if (collider?.bounds.Contains(headTransform.position) == true)
                {
                    if (!SequenceManager.Instance.IsStepCompleted(4))
                    {
                        if (isPracticeMode) ShowSubtitle("먼저 화재 경보 버튼을 눌러주세요!");
                        SequenceManager.Instance.RecordSequenceError(4);
                        RecordFeedback(FeedbackButton, "화재 경보를 누르지 않았습니다.");
                        if (isPracticeMode) return;
                    }
                    hasReachedEmergencyDoor = true;
                    SequenceManager.Instance.CompleteStep(6);
                    StartCoroutine(EmergencyDoorSequence());
                }
            }

            // Exit Trigger
            if (!hasCompletedEvacuation && exitTrigger)
            {
                var collider = exitTrigger.GetComponent<Collider>();
                if (collider?.bounds.Contains(headTransform.position) == true)
                {
                    hasCompletedEvacuation = true;
                    if (isPracticeMode)
                    {
                        ShowSubtitle("축하합니다 모든 화재 대피 훈련이 완료되었습니다! 이제 밖으로 대피하여 훈련을 완료하세요.");
                        if (EndArrow) EndArrow.SetActive(true);
                    }
                }
            }
        }

        private IEnumerator EvacuationSequence()
        {
            if (isPracticeMode)
            {
                ShowSubtitle("건물 밖으로 대피 시 대피 경로를 파악하는 것이 가장 중요합니다.");
                yield return new WaitForSeconds(textDelay);
                ShowSubtitle("먼저 앞에 있는 탈출 경로 안내도를 확인하여 대피 경로를 파악하세요.");
                if (outlineEvacuationMap) AddOutline(outlineEvacuationMap);
                hasHighlightedMap = true;
                SequenceManager.Instance.CompleteStep(3);
                yield return new WaitForSeconds(textDelay);
                ShowSubtitle("안내도를 확인한 후, 화재 경보 버튼을 눌러 주변에 위험을 알려야 합니다.");
                if (ButtonArrow) ButtonArrow.SetActive(true);
                if (outlineEvacuationMap) RemoveOutline(outlineEvacuationMap);
                yield return new WaitForSeconds(textDelay);
            }
            hasHighlightedMap = true;
            SequenceManager.Instance.CompleteStep(3);
        }

        private IEnumerator SmokeSequence()
        {
            foreach (var effect in smokeEffects ?? new List<ParticleSystem>())
            {
                effect?.Play();
            }

            if (SoundManager.Instance)
            {
                SoundManager.Instance.PlayOneShot(1, 0);
            }

            if (isPracticeMode)
            {
                ShowSubtitle("화재로 인해 주변에 연기가 가득해졌습니다.");
                yield return new WaitForSeconds(textDelay);
                ShowSubtitle("화재로 발생한 연기는 독성이 있어 오래 노출되면 위험합니다!");
                yield return new WaitForSeconds(textDelay);
                ShowSubtitle("연기는 위로 올라가는 성질이 있으므로, 낮게 엎드려 포복으로 다음 위치까지 이동 해봅시다.");
                if (SmokeArriveArrow) SmokeArriveArrow.SetActive(true);
                yield return new WaitForSeconds(textDelay);
            }
        }

        private IEnumerator EmergencyDoorSequence()
        {
            if (isPracticeMode)
            {
                if (EmergencyArrow) EmergencyArrow.SetActive(false);
                ShowSubtitle("비상문에 도착했습니다! 다음은 비상문 통과에 대해 배워봅시다.");
                yield return new WaitForSeconds(textDelay);
                ShowSubtitle("실제 화재 상황에서는 비상문을 막다른 길로 오해해 위험에 처하는 경우가 많습니다.");
                yield return new WaitForSeconds(textDelay);
                ShowSubtitle("비상문에 도착하면 '비상문'이라는 글을 찾아 문의 위치를 확인해야합니다.");
                yield return new WaitForSeconds(textDelay);
                if (outlineEmergencyDoor) AddOutline(outlineEmergencyDoor);
                ShowSubtitle("해당 문의 한 쪽을 미는 것 만으로 가볍게 문이 열립니다. 한 번 열어봅시다.");
                isDoorEnabled = true;
                yield return new WaitForSeconds(textDelay);
            }
            else
            {
                isDoorEnabled = true;
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

        private void RecordFeedback(TMP_Text feedbackText, string message)
        {
            if (feedbackText) feedbackText.text = message;
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
            if (target.TryGetComponent<Outline>(out var outline)) outline.enabled = false;
        }

        private void OnDestroy()
        {
            if (doorTriggerCollider) Destroy(doorTriggerCollider);
        }
    }
}