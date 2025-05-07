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

    public class Section2_ver2 : MonoBehaviour
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
        public GameObject smokeArrivalTrigger;

        [Header("포복 트리거 설정")]
        public GameObject crawlingTrigger;
        private Collider crawlingTriggerCollider;
        private bool isInsideCrawlingTrigger = false;

        private GameObject emergencyDoor;
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
                    //Debug.LogError("Head Transform (Main Camera)을 찾을 수 없습니다!");
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
                vignette.intensity.Override(0.8f);
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
                //Debug.LogError("탈출 경로 안내도 오브젝트가 지정되지 않았습니다!");
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
                //Debug.Log("✅ 문이 손으로 밀려 열림.");
            }
        }

        void OnButtonHoverEnter(HoverEnterEventArgs args)
        {
            if (!SequenceManager.Instance.IsStepCompleted(5))
            {
                ShowSubtitle("먼저 탈출 경로 안내도를 확인해야 합니다!");
                SequenceManager.Instance.RecordSequenceError(5);
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
        }

        void CheckButtonTriggerCollision()
        {
            if (buttonTriggerCollider == null) return;

            if (buttonCollider.bounds.Intersects(buttonTriggerCollider.bounds))
            {
                if (!isButtonTriggerActivated)
                {
                    if (!SequenceManager.Instance.IsStepCompleted(5))
                    {
                        ShowSubtitle("먼저 탈출 경로 안내도를 확인해야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(5);
                        return;
                    }
                    onFireAlarmActivated?.Invoke();
                    isButtonTriggerActivated = true;
                    hasActivatedAlarm = true;
                    SequenceManager.Instance.CompleteStep(6);
                    ShowSubtitle("화재 경보가 활성화되었습니다! 이제 안전하게 이동해봅시다!");

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

            // 1. 비상문 대피 (Step 3)
            if (!hasReachedEmergencyDoor && doorTrigger != null)
            {
                Collider doorApproachTriggerCollider = doorTrigger.GetComponent<Collider>();
                if (doorApproachTriggerCollider != null && doorApproachTriggerCollider.bounds.Contains(headTransform.position))
                {
                    hasReachedEmergencyDoor = true;
                    SequenceManager.Instance.CompleteStep(3);
                    StartCoroutine(EmergencyDoorSequence());
                }
            }

            // 2. StartTrigger 도착 (Step 4)
            if (!hasStartedSequence && startTrigger != null && hasReachedEmergencyDoor)
            {
                Collider startTriggerCollider = startTrigger.GetComponent<Collider>();
                if (startTriggerCollider != null && startTriggerCollider.bounds.Contains(headTransform.position))
                {
                    if (!SequenceManager.Instance.IsStepCompleted(3))
                    {
                        ShowSubtitle("먼저 비상문을 열고 통과해야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(3);
                        return;
                    }
                    hasStartedSequence = true;
                    SequenceManager.Instance.CompleteStep(4);
                    StartCoroutine(EvacuationSequence());
                }
            }

            // 3. 대피도 확인 (Step 5, HighlightEvacuationMap에서 완료)

            // 4. 버튼 클릭 (Step 6, CheckButtonTriggerCollision에서 완료)

            // 5. 포복 트리거 도착 (Step 7)
            if (!hasEnteredSmokeArea && smokeTrigger != null && hasActivatedAlarm)
            {
                Collider smokeTriggerCollider = smokeTrigger.GetComponent<Collider>();
                if (smokeTriggerCollider != null && smokeTriggerCollider.bounds.Contains(headTransform.position))
                {
                    if (!SequenceManager.Instance.IsStepCompleted(5))
                    {
                        ShowSubtitle("먼저 탈출 경로 안내도를 확인해야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(5);
                        return;
                    }
                    if (!SequenceManager.Instance.IsStepCompleted(6))
                    {
                        ShowSubtitle("먼저 화재 경보 버튼을 눌러주세요!");
                        SequenceManager.Instance.RecordSequenceError(6);
                        return;
                    }
                    hasEnteredSmokeArea = true;
                    SequenceManager.Instance.CompleteStep(7);
                    StartCoroutine(SmokeSequence());
                }
            }

            // 6. 포복 (플레이어 동작)
            // 7. 포복 도착 트리거 도착 (Step 8)
            if (!hasCrawled && smokeArrivalTrigger != null && hasEnteredSmokeArea)
            {
                Collider arrivalTriggerCollider = smokeArrivalTrigger.GetComponent<Collider>();
                if (arrivalTriggerCollider != null && arrivalTriggerCollider.bounds.Contains(headTransform.position))
                {
                    if (!SequenceManager.Instance.IsStepCompleted(7))
                    {
                        ShowSubtitle("먼저 연기 구역에 들어가 포복으로 이동해야 합니다!");
                        SequenceManager.Instance.RecordSequenceError(7);
                        return;
                    }
                    hasCrawled = true;
                    onCrawlingStarted?.Invoke();
                    SequenceManager.Instance.CompleteStep(8);
                    ShowSubtitle("잘했어요! 포복을 완료했습니다. 훈련을 마무리했습니다!");
                }
            }

            // 최종 탈출 확인
            if (!hasCompletedEvacuation && exitTrigger != null && hasReachedEmergencyDoor)
            {
                Collider exitTriggerCollider = exitTrigger.GetComponent<Collider>();
                if (exitTriggerCollider != null && exitTriggerCollider.bounds.Contains(headTransform.position))
                {
                    hasCompletedEvacuation = true;
                    ShowSubtitle("축하합니다! 모든 화재 대피 훈련이 완료되었습니다!");
                    onEmergencyDoorOpened?.Invoke();
                }
            }
        }

        IEnumerator EvacuationSequence()
        {
            ShowSubtitle("잘했습니다! 다음으로는 화재 대피 시 대피 경로를 파악하는 것이 중요합니다!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("평상 시 자주 이용하는 장소가 아니라면 방문 시 대피 경로를 미리 파악해놓는 것이 중요합니다. ");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("앞의 탈출 경로 안내도를 확인하여 대피 경로를 파악하세요.");
            HighlightEvacuationMap();
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("안내도를 확인한 후, 가능하다면 화재 경보 버튼을 눌러 주변에 위험을 알려야 합니다.");
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

            ShowSubtitle("화재로 인해 주변에 연기가 가득해졌습니다.");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("화재로 발생한 연기는 독성이 있어 오래 노출되면 위험합니다!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("연기는 위로 올라가는 성질이 있으므로, 낮게 엎드려 포복으로 이동해야 합니다.");
            yield return new WaitForSeconds(textDelay);
        }

        IEnumerator EmergencyDoorSequence()
        {
            ShowSubtitle("비상문에 도착했습니다! 비상문 통과에 대해 배워봅시다.");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("실제 화재 상황에서는 비상문을 막다른 길로 오해해 위험에 처하는 경우가 많습니다.");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("비상문에 도착하면 '비상문'이라는 글을 찾아 문의 위치를 확인해야합니다.");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("해당 문의 한 쪽을 미는 것 만으로 가볍게 문이 열립니다. 한 번 열어봅시다.");
            HighlightEmergencyDoor();
            isDoorEnabled = true;
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
                    SequenceManager.Instance.CompleteStep(5);
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
                }
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