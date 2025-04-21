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

        // 버튼 관련 변수
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable buttonInteractable;
        private Rigidbody buttonRb;
        private BoxCollider buttonCollider;
        private BoxCollider buttonTriggerCollider;
        private Vector3 buttonInitialPosition;
        private bool isButtonPressed = false;
        private bool isButtonTriggerActivated = false;

        // 안내도 상태 변수
        private bool hasHighlightedMap = false;

        // UnityEvent로 버튼 눌림 이벤트 처리
        [SerializeField] public UnityEvent onFireAlarmActivated;

        private void Start()
        {
            // 버튼 초기화
            SetupButton();

            // 후처리 초기화
            InitPostProcessing();

            // 안내도 초기화
            SetupEvacuationMap();

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

            // 시퀀스 시작
            StartCoroutine(EvacuationSequence());
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

        void SetupEvacuationMap()
        {
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

        IEnumerator EvacuationSequence()
        {
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