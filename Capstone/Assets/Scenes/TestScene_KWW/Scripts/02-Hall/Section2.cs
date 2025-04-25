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

        [Header("포복 트리거 설정")]
        public GameObject crawlingTrigger;
        private Collider crawlingTriggerCollider;
        private bool isInsideCrawlingTrigger = false;

        [Header("비상문 설정")]
        public GameObject emergencyDoor;
        public Collider doorHandle;
        public GameObject doorTrigger;
        public GameObject exitTrigger;

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
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable doorGrabInteractable;

        private bool hasStartedSequence = false;
        private bool hasHighlightedMap = false;
        private bool hasActivatedAlarm = false;
        private bool hasEnteredSmokeArea = false;
        private bool hasCrawled = false;
        private bool hasReachedEmergencyDoor = false;
        private bool hasCompletedEvacuation = false;

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

            if (headTransform == null)
            {
                headTransform = Camera.main?.transform;
                if (headTransform == null)
                {
                    Debug.LogError("Head Transform (Main Camera)을 찾을 수 없습니다!");
                }
            }
        }

        void SetupStartTrigger()
        {
            if (startTrigger == null)
            {
                Debug.LogError("시작 트리거 오브젝트가 지정되지 않았습니다!");
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
                Debug.LogError("화재 경보 버튼 오브젝트가 지정되지 않았습니다!");
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
                Debug.LogError("연기 트리거 오브젝트가 지정되지 않았습니다!");
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
                Debug.LogError("포복 트리거 오브젝트가 지정되지 않았습니다!");
                return;
            }

            crawlingTriggerCollider = crawlingTrigger.GetComponent<Collider>();
            if (crawlingTriggerCollider == null)
            {
                crawlingTriggerCollider = crawlingTrigger.AddComponent<BoxCollider>();
            }
            crawlingTriggerCollider.isTrigger = true;

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
                Debug.LogError("비상문 오브젝트가 지정되지 않았습니다!");
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

            doorGrabInteractable = emergencyDoor.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (doorGrabInteractable == null)
            {
                doorGrabInteractable = emergencyDoor.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            }
            doorGrabInteractable.colliders.Clear();
            if (doorHandle != null)
            {
                doorGrabInteractable.colliders.Add(doorHandle);
            }
            doorGrabInteractable.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.VelocityTracking;
            doorGrabInteractable.trackPosition = true;
            doorGrabInteractable.trackRotation = true;
            doorGrabInteractable.throwOnDetach = true;
            doorGrabInteractable.enabled = false;

            if (doorTrigger == null)
            {
                Debug.LogError("비상문 트리거 오브젝트가 지정되지 않았습니다!");
                return;
            }

            Collider doorTriggerCollider = doorTrigger.GetComponent<Collider>();
            if (doorTriggerCollider == null)
            {
                doorTriggerCollider = doorTrigger.AddComponent<BoxCollider>();
            }
            doorTriggerCollider.isTrigger = true;

            if (exitTrigger == null)
            {
                Debug.LogError("비상문 반대편 트리거 오브젝트가 지정되지 않았습니다!");
                return;
            }

            Collider exitTriggerCollider = exitTrigger.GetComponent<Collider>();
            if (exitTriggerCollider == null)
            {
                exitTriggerCollider = exitTrigger.AddComponent<BoxCollider>();
            }
            exitTriggerCollider.isTrigger = true;

            Debug.Log("✅ 비상문 설정 완료.");
        }

        void OnButtonHoverEnter(HoverEnterEventArgs args)
        {
            if (!SequenceManager.Instance.IsStepCompleted(3)) // 대피도 확인 완료 여부
            {
                ShowSubtitle("먼저 탈출 경로 안내도를 확인해주세요!");
                SequenceManager.Instance.RecordSequenceError(4); // 경보 울리기 순서 오류
                return;
            }
            isButtonPressed = true;
        }

        void OnButtonHoverExit(HoverExitEventArgs args)
        {
            isButtonPressed = false;
            isButtonTriggerActivated = false;
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

            if (hasEnteredSmokeArea && !hasCrawled && headTransform != null && crawlingTriggerCollider != null)
            {
                isInsideCrawlingTrigger = crawlingTriggerCollider.bounds.Contains(headTransform.position);

                if (!isInsideCrawlingTrigger && !hasCrawled)
                {
                    StartCoroutine(CompleteCrawling());
                }
            }
        }

        IEnumerator CompleteCrawling()
        {
            yield return new WaitForSeconds(2f);
            if (!isInsideCrawlingTrigger)
            {
                hasCrawled = true;
                onCrawlingStarted?.Invoke();
                SequenceManager.Instance.CompleteStep(5); // 포복 완료
                ShowSubtitle("잘했어요! 포복을 완료했습니다. 이제 비상문으로 이동하세요!");
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
                        ShowSubtitle("먼저 탈출 경로 안내도를 확인해주세요!");
                        SequenceManager.Instance.RecordSequenceError(4);
                        return;
                    }
                    onFireAlarmActivated?.Invoke();
                    isButtonTriggerActivated = true;
                    hasActivatedAlarm = true;
                    SequenceManager.Instance.CompleteStep(4); // 경보 울리기 완료
                    ShowSubtitle("화재 경보가 활성화되었습니다! 이제 안전하게 이동하세요!");

                    if (playSoundOnButtonPress)
                    {
                        if (SoundManager.Instance == null)
                        {
                            Debug.LogError("SoundManager.Instance is null! Cannot play sound.");
                            return;
                        }
                        try
                        {
                            SoundManager.Instance.PlayOneShot(soundGroupIndex, soundClipIndex);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError("사운드 재생 중 오류 발생: " + e.Message);
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
                        ShowSubtitle("먼저 탈출 경로 안내도를 확인해주세요!");
                        SequenceManager.Instance.RecordSequenceError(5); // 포복 순서 오류
                        return;
                    }
                    if (!SequenceManager.Instance.IsStepCompleted(4))
                    {
                        ShowSubtitle("먼저 화재 경보 버튼을 눌러주세요!");
                        SequenceManager.Instance.RecordSequenceError(5);
                        return;
                    }
                    hasEnteredSmokeArea = true;
                    StartCoroutine(SmokeSequence());
                }
            }

            if (!hasReachedEmergencyDoor && doorTrigger != null)
            {
                Collider doorTriggerCollider = doorTrigger.GetComponent<Collider>();
                if (doorTriggerCollider != null && doorTriggerCollider.bounds.Contains(headTransform.position))
                {
                    if (!SequenceManager.Instance.IsStepCompleted(3))
                    {
                        ShowSubtitle("먼저 탈출 경로 안내도를 확인해주세요!");
                        SequenceManager.Instance.RecordSequenceError(6); // 비상문 사용 순서 오류
                        return;
                    }
                    if (!SequenceManager.Instance.IsStepCompleted(4))
                    {
                        ShowSubtitle("먼저 화재 경보 버튼을 눌러주세요!");
                        SequenceManager.Instance.RecordSequenceError(6);
                        return;
                    }
                    if (!SequenceManager.Instance.IsStepCompleted(5))
                    {
                        ShowSubtitle("먼저 연기 구역에서 포복으로 이동해야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(6);
                        return;
                    }
                    hasReachedEmergencyDoor = true;
                    SequenceManager.Instance.CompleteStep(6); // 비상문 사용 완료
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
                        ShowSubtitle("먼저 비상문을 열고 통과해야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(6);
                        return;
                    }
                    hasCompletedEvacuation = true;
                    ShowSubtitle("축하합니다 모든 화재 대피 훈련이 완료되었습니다!");
                    onEmergencyDoorOpened?.Invoke();
                }
            }
        }

        IEnumerator EvacuationSequence()
        {
            HighlightEvacuationMap();
            ShowSubtitle("먼저 앞에 있는 탈출 경로 안내도를 확인하여 대피 경로를 파악하세요.");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("안내도를 확인한 후, 화재 경보 버튼을 눌러 주변에 위험을 알려야 합니다.");
            yield return new WaitForSeconds(textDelay);
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

            ShowSubtitle("주변에 연기가 가득해졌어요!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("연기가 자욱합니다! 낮게 엎드려 포복으로 이동해야 합니다!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("이 연기는 독성이 있어 오래 노출되면 위험해요!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("연기 구역에서는 낮게 엎드려 포복으로 안전하게 이동하세요!");
            yield return new WaitForSeconds(textDelay);
        }

        IEnumerator EmergencyDoorSequence()
        {
            ShowSubtitle("비상문에 도착했습니다!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("다음은 비상문 통과입니다!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("실제 화재 상황에서는 비상문을 막다른 길로 오해해 위험에 처하는 경우가 많습니다.");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("비상문에 도착하면 '비상문'이라는 글을 찾아 문의 위치를 확인하세요!");
            yield return new WaitForSeconds(textDelay);

            HighlightEmergencyDoor();
            ShowSubtitle("문이 하이라이트되었습니다. 문의 한쪽을 밀어 열고 탈출하세요!");
            yield return new WaitForSeconds(textDelay);

            onEmergencyDoorOpened?.Invoke();
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
                    SequenceManager.Instance.CompleteStep(3); // 대피도 확인 완료
                }
            }
        }

        void HighlightEmergencyDoor()
        {
            if (emergencyDoor != null)
            {
                Renderer doorRenderer = emergencyDoor.GetComponent<Renderer>();
                if (doorRenderer != null)
                {
                    doorRenderer.material.color = Color.green;
                    doorGrabInteractable.enabled = true;
                }
            }
        }
    }
}