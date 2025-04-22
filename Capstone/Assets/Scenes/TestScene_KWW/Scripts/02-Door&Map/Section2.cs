using System.Collections;
using System.Collections.Generic; // List<T>를 사용하기 위해 필요
using UnityEngine;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace FireEvacuation
{
#pragma warning disable 0618 // XRBaseController에 대한 폐기 경고 억제

    public class Section2 : MonoBehaviour
    {
        [Header("UI 설정")]
        public TMP_Text subtitleText; // 자막 텍스트 UI
        public float textDelay = 5f; // 자막 표시 지연 시간 (초)

        [Header("후처리 효과")]
        public Volume globalVolume; // 전역 볼륨 (후처리 효과)
        private Vignette vignette; // 비네트 효과

        [Header("시작 트리거 설정")]
        public GameObject startTrigger; // 대사 시작 트리거 오브젝트

        [Header("안내도 설정")]
        public GameObject evacuationMap; // 탈출 경로 안내도 오브젝트

        [Header("버튼 설정")]
        public GameObject fireAlarmButton; // 화재 경보 버튼 오브젝트
        public Transform buttonTriggerTransform; // 버튼 트리거 Transform
        public float pressDistance = 0.05f; // 버튼 눌림 거리
        public float returnSpeed = 5f; // 버튼 복귀 속도

        [Header("연기 트리거 설정")]
        public GameObject smokeTrigger; // 연기 트리거 오브젝트
        public List<ParticleSystem> smokeEffects; // 연기 파티클 시스템 리스트 (다중 연기 VFX 지원)

        [Header("포복 트리거 설정")]
        public GameObject crawlingTrigger; // 포복 여부를 판단할 트리거 오브젝트
        private Collider crawlingTriggerCollider; // 포복 트리거 콜라이더
        private bool isInsideCrawlingTrigger = false; // 사용자의 머리가 트리거 안에 있는지 여부

        [Header("비상문 설정")]
        public GameObject emergencyDoor; // 비상문 오브젝트
        public Collider doorHandle; // 비상문 손잡이
        public GameObject doorTrigger; // 비상문 트리거
        public GameObject exitTrigger; // 비상문 반대편 트리거 (완료 메시지 출력용)

        [Header("Player Head Transform")]
        public Transform headTransform; // 플레이어의 헤드 Transform (예: XR 카메라)

        [Header("사운드 설정")]
        public bool playSoundOnButtonPress = true; // 버튼 눌림 시 사운드 재생 여부
        public int soundGroupIndex = 0; // 재생할 SoundGroup 인덱스
        public int soundClipIndex = 0; // 재생할 Clip 인덱스
        public bool loopSound = false; // 사운드 루프 여부

        // 버튼 관련 변수
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable buttonInteractable; // 버튼 상호작용
        private Rigidbody buttonRb; // 버튼 Rigidbody
        private BoxCollider buttonCollider; // 버튼 콜라이더
        private BoxCollider buttonTriggerCollider; // 버튼 트리거 콜라이더
        private Vector3 buttonInitialPosition; // 버튼 초기 위치
        private bool isButtonPressed = false; // 버튼 눌림 상태
        private bool isButtonTriggerActivated = false; // 버튼 트리거 활성화 상태

        // 비상문 관련 변수
        private Rigidbody doorRb; // 비상문 Rigidbody
        private HingeJoint hingeJoint; // 비상문 힌지 조인트
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable doorGrabInteractable; // 비상문 잡기 상호작용

        // 시나리오 진행 상태 추적
        private bool hasStartedSequence = false; // 대사 시퀀스 시작 여부
        private bool hasHighlightedMap = false; // 안내도 확인 완료 여부
        private bool hasActivatedAlarm = false; // 경보 활성화 완료 여부
        private bool hasEnteredSmokeArea = false; // 연기 구역 진입 완료 여부
        private bool hasCrawled = false; // 포복 완료 여부
        private bool hasReachedEmergencyDoor = false; // 비상문 도달 완료 여부
        private bool hasCompletedEvacuation = false; // 최종 트리거 도달 완료 여부

        // UnityEvent로 이벤트 처리
        [SerializeField] public UnityEvent onFireAlarmActivated; // 경보 활성화 이벤트
        [SerializeField] public UnityEvent onCrawlingStarted; // 포복 시작 이벤트
        [SerializeField] public UnityEvent onEmergencyDoorOpened; // 비상문 열림 이벤트

        private void Start()
        {
            // 버튼 초기화
            SetupButton();

            // 후처리 초기화
            InitPostProcessing();

            // 안내도 초기화
            SetupEvacuationMap();

            // 연기 트리거 초기화
            SetupSmokeTrigger();

            // 비상문 초기화
            SetupEmergencyDoor();

            // 시작 트리거 초기화
            SetupStartTrigger();

            // 플레이어 헤드 Transform 찾기
            if (headTransform == null)
            {
                headTransform = Camera.main?.transform; // XR 설정에서 Main Camera 사용
                if (headTransform == null)
                {
                    Debug.LogError("Head Transform (Main Camera)을 찾을 수 없습니다! Inspector에서 수동으로 지정해주세요.");
                }
                else
                {
                    Debug.Log("Head Transform 지정됨: " + headTransform.name);
                }
            }
        }

        // 시작 트리거 설정 메서드
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

        // 버튼 설정 메서드
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

            // 트리거 설정
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

        // 후처리 설정 메서드
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

        // 안내도 설정 메서드
        void SetupEvacuationMap()
        {
            if (evacuationMap == null)
            {
                Debug.LogError("탈출 경로 안내도 오브젝트가 지정되지 않았습니다!");
                return;
            }
        }

        // 연기 트리거 설정 메서드
        void SetupSmokeTrigger()
        {
            if (smokeTrigger == null)
            {
                Debug.LogError("연기 트리거 오브젝트가 지정되지 않았습니다!");
                return;
            }

            // 트리거 콜라이더 설정
            Collider triggerCollider = smokeTrigger.GetComponent<Collider>();
            if (triggerCollider == null)
            {
                triggerCollider = smokeTrigger.AddComponent<BoxCollider>();
            }
            triggerCollider.isTrigger = true;

            // 포복 트리거 설정
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

            // 연기 파티클 시스템 초기화 (모두 꺼짐 상태로 시작)
            if (smokeEffects != null && smokeEffects.Count > 0)
            {
                foreach (var smokeEffect in smokeEffects)
                {
                    if (smokeEffect != null)
                    {
                        smokeEffect.Stop();
                    }
                    else
                    {
                        Debug.LogWarning("연기 파티클 시스템 리스트에 null 항목이 있습니다!");
                    }
                }
            }
            else
            {
                Debug.LogWarning("연기 파티클 시스템이 지정되지 않았거나 리스트가 비어 있습니다!");
            }
        }

        // 비상문 설정 메서드
        void SetupEmergencyDoor()
        {
            if (emergencyDoor == null)
            {
                Debug.LogError("비상문 오브젝트가 지정되지 않았습니다!");
                return;
            }

            // Rigidbody 설정
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

            // HingeJoint 설정
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

            // XR Grab Interactable 설정
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
            doorGrabInteractable.enabled = false; // 초기에는 문 상호작용 비활성화

            // 비상문 트리거 설정
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

            // 비상문 반대편 트리거 설정 (완료 메시지 출력용)
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

        // 버튼 호버 시작 이벤트
        void OnButtonHoverEnter(HoverEnterEventArgs args)
        {
            if (!hasHighlightedMap)
            {
                ShowSubtitle("먼저 탈출 경로 안내도를 확인해주세요!");
                return;
            }
            isButtonPressed = true;
        }

        // 버튼 호버 종료 이벤트
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

            // 트리거 충돌 확인
            CheckTriggerCollision();

            // 포복 트리거 확인
            if (hasEnteredSmokeArea && !hasCrawled && headTransform != null && crawlingTriggerCollider != null)
            {
                bool wasInside = isInsideCrawlingTrigger;
                isInsideCrawlingTrigger = crawlingTriggerCollider.bounds.Contains(headTransform.position);

                if (wasInside != isInsideCrawlingTrigger) // 상태가 변경되었을 때만 메시지 표시
                {
                    if (isInsideCrawlingTrigger)
                    {
                        ShowSubtitle("너무 높습니다! 더 낮게 엎드려 포복으로 이동하세요!");
                    }
                    else
                    {
                        ShowSubtitle("좋습니다! 낮게 포복으로 이동 중이에요!");
                    }
                }

                // 포복 트리거를 벗어나면 포복 성공으로 간주
                if (!isInsideCrawlingTrigger && !hasCrawled)
                {
                    StartCoroutine(CompleteCrawling());
                }
            }
        }

        // 포복 완료 처리
        IEnumerator CompleteCrawling()
        {
            yield return new WaitForSeconds(2f); // 포복 상태를 2초 유지해야 완료로 간주
            if (!isInsideCrawlingTrigger) // 여전히 트리거 밖에 있는지 확인
            {
                hasCrawled = true;
                onCrawlingStarted?.Invoke();
                ShowSubtitle("잘했어요! 포복을 완료했습니다. 이제 비상문으로 이동하세요!");
            }
        }

        // 버튼 트리거 충돌 확인
        void CheckButtonTriggerCollision()
        {
            if (buttonTriggerCollider == null) return;

            if (buttonCollider.bounds.Intersects(buttonTriggerCollider.bounds))
            {
                if (!isButtonTriggerActivated)
                {
                    if (!hasHighlightedMap)
                    {
                        ShowSubtitle("먼저 탈출 경로 안내도를 확인해주세요!");
                        return;
                    }
                    onFireAlarmActivated?.Invoke();
                    isButtonTriggerActivated = true;
                    hasActivatedAlarm = true;
                    ShowSubtitle("화재 경보가 활성화되었습니다! 이제 안전하게 이동하세요!");
                    StartCoroutine(ShowCrawlingInstruction());

                    // 버튼 눌림 시 사운드 재생
                    if (playSoundOnButtonPress && SoundManager.Instance != null)
                    {
                        SoundManager.Instance.Play(soundGroupIndex, soundClipIndex, loopSound);
                    }
                }
            }
        }

        // 트리거 충돌 확인 (시작, 연기, 비상문, 완료 트리거)
        void CheckTriggerCollision()
        {
            if (headTransform == null) return;

            // 시작 트리거 확인
            if (!hasStartedSequence && startTrigger != null)
            {
                Collider startTriggerCollider = startTrigger.GetComponent<Collider>();
                if (startTriggerCollider != null && startTriggerCollider.bounds.Contains(headTransform.position))
                {
                    hasStartedSequence = true;
                    StartCoroutine(EvacuationSequence());
                }
            }

            // 연기 트리거 확인
            if (!hasEnteredSmokeArea && smokeTrigger != null)
            {
                Collider smokeTriggerCollider = smokeTrigger.GetComponent<Collider>();
                if (smokeTriggerCollider != null && smokeTriggerCollider.bounds.Contains(headTransform.position))
                {
                    if (!hasHighlightedMap)
                    {
                        ShowSubtitle("먼저 탈출 경로 안내도를 확인해주세요!");
                        return;
                    }
                    if (!hasActivatedAlarm)
                    {
                        ShowSubtitle("먼저 화재 경보 버튼을 눌러주세요!");
                        return;
                    }
                    hasEnteredSmokeArea = true;
                    StartCoroutine(SmokeSequence());
                }
            }

            // 비상문 트리거 확인
            if (!hasReachedEmergencyDoor && doorTrigger != null)
            {
                Collider doorTriggerCollider = doorTrigger.GetComponent<Collider>();
                if (doorTriggerCollider != null && doorTriggerCollider.bounds.Contains(headTransform.position))
                {
                    if (!hasHighlightedMap)
                    {
                        ShowSubtitle("먼저 탈출 경로 안내도를 확인해주세요!");
                        return;
                    }
                    if (!hasActivatedAlarm)
                    {
                        ShowSubtitle("먼저 화재 경보 버튼을 눌러주세요!");
                        return;
                    }
                    if (!hasCrawled)
                    {
                        ShowSubtitle("먼저 연기 구역에서 포복으로 이동해야 합니다!");
                        return;
                    }
                    hasReachedEmergencyDoor = true;
                    StartCoroutine(EmergencyDoorSequence());
                }
            }

            // 비상문 반대편 트리거 확인 (완료 메시지 출력)
            if (!hasCompletedEvacuation && exitTrigger != null)
            {
                Collider exitTriggerCollider = exitTrigger.GetComponent<Collider>();
                if (exitTriggerCollider != null && exitTriggerCollider.bounds.Contains(headTransform.position))
                {
                    if (!hasReachedEmergencyDoor)
                    {
                        ShowSubtitle("먼저 비상문을 열고 통과해야 합니다!");
                        return;
                    }
                    hasCompletedEvacuation = true;
                    ShowSubtitle("축하합니다 모든 화재 대피 훈련이 완료되었습니다!");
                    onEmergencyDoorOpened?.Invoke(); // 훈련 완료 후 이벤트 호출
                }
            }
        }

        // 대피 시퀀스 코루틴
        IEnumerator EvacuationSequence()
        {
            // 안내도 하이라이트
            HighlightEvacuationMap();
            ShowSubtitle("먼저 앞에 있는 탈출 경로 안내도를 확인하여 대피 경로를 파악하세요.");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("안내도를 확인한 후, 화재 경보 버튼을 눌러 주변에 위험을 알려야 합니다.");
            yield return new WaitForSeconds(textDelay);
        }

        // 연기 시퀀스 코루틴
        IEnumerator SmokeSequence()
        {
            ShowSubtitle("주변에 연기가 가득해졌어요!");
            yield return new WaitForSeconds(textDelay);

            // 연기 효과 활성화 (모든 연기 VFX 재생)
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

            ShowSubtitle("연기가 자욱합니다! 낮게 엎드려 포복으로 이동해야 합니다!");
            yield return new WaitForSeconds(textDelay);
        }

        // 비상문 시퀀스 코루틴
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

            // 비상문 하이라이트
            HighlightEmergencyDoor();
            ShowSubtitle("문이 하이라이트되었습니다. 문의 한쪽을 밀어 열고 탈출하세요!");
            yield return new WaitForSeconds(textDelay);

            // 문 열림 이벤트 호출
            onEmergencyDoorOpened?.Invoke();
        }

        // 포복 지시 표시 코루틴
        IEnumerator ShowCrawlingInstruction()
        {
            ShowSubtitle("이 연기는 독성이 있어 오래 노출되면 위험해요!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("연기 구역에서는 낮게 엎드려 포복으로 안전하게 이동하세요!");
            yield return new WaitForSeconds(textDelay);
        }

        // 자막 표시 메서드
        void ShowSubtitle(string message)
        {
            if (subtitleText != null)
            {
                subtitleText.color = Color.black;
                subtitleText.text = message;
            }
        }

        // 안내도 하이라이트 메서드
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

        // 비상문 하이라이트 메서드
        void HighlightEmergencyDoor()
        {
            if (emergencyDoor != null)
            {
                Renderer doorRenderer = emergencyDoor.GetComponent<Renderer>();
                if (doorRenderer != null)
                {
                    doorRenderer.material.color = Color.green; // 문을 초록색으로 하이라이트
                    doorGrabInteractable.enabled = true; // 문 상호작용 활성화
                }
                else
                {
                    Debug.LogWarning("비상문에 Renderer 컴포넌트가 없습니다!");
                }
            }
        }
    }
}