using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Events;

namespace FireEvacuation
{
#pragma warning disable 0618 // Suppress obsolete warning for XRBaseController

    public class Section1 : MonoBehaviour
    {
        [Header("UI 설정")]
        public TMP_Text subtitleText;
        public float textDelay = 5f;

        [Header("사운드 설정")]
        public int sirenGroupIndex = 0; // 사운드 그룹 인덱스 (Inspector에서 설정)
        public int sirenClipIndex = 0;

        [Header("후처리 효과")]
        public Volume globalVolume;
        private Vignette vignette;

        [Header("Rag 설정")]
        public GameObject ragObject; // Inspector에서 할당할 Rag 오브젝트
        public GameObject waterObject; // Inspector에서 할당할 Water 오브젝트
        public Transform headTransform; // Reference to XR Rig's Main Camera
        public float protectionDistance = 0.2f; // Detection distance from head

        // Timer-related variables
        public float waterSearchTime = 15f; // User-configurable time to find water
        private float searchTimer = 0f;
        private bool isSearchingForWater = false;
        private float wetTimeRequired = 5f; // Time required to wet the rag
        private float wetTimer = 0f;
        private bool isWetting = false;

        // UnityEvent로 변경하여 Inspector에서 설정 가능
        [SerializeField] public UnityEvent onProtectionActivated; // Inspector에서 설정 가능한 이벤트
        [SerializeField] public UnityEvent onProtectionDeactivated; // Inspector에서 설정 가능한 이벤트

        // Rag 관련 변수
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
        private Renderer cubeRenderer;
        private bool isWet = false;
        private bool isProtecting = false;
        private bool hasRagBeenGrabbed = false; // 헝겊이 처음 그랩되었는지 추적

        // 이벤트가 한 번만 호출되도록 추적
        private bool hasProtectionActivated = false; // 활성화 이벤트가 호출되었는지
        private bool hasProtectionDeactivated = false; // 비활성화 이벤트가 호출되었는지

        private void Start()
        {
            // Rag 초기화
            InitializeRag();

            // Post-Processing 초기화
            InitPostProcessing();

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
                cubeRenderer.material = new Material(cubeRenderer.material); // Material instantiation
                cubeRenderer.material.color = Color.red; // Initial color (red)
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
                headTransform = GameObject.Find("Main Camera").transform; // XR Rig's camera
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

        IEnumerator DrillSequence()
        {
            ShowSubtitle("화재 대피 시스템에 온 걸 환영해!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("이 교육에서는 아파트 상황을 전제로 화재 대피 시 주의할 점들을 배워볼 거야.");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("천천히 교육을 따라오면서 안전한 대피 지식을 쌓아보자.");
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

            yield return FlashingSubtitle("현재 건물에 화재가 발생해 경보가 울렸어!", 0.6f);

            ShowSubtitle("먼저 대피 시 호흡을 보호할 도구가 필요해!");
            yield return new WaitForSeconds(textDelay);

            ShowSubtitle("주변에 헝겊으로 사용할 물건이 있는지 찾아보자.");
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
            if (!hasRagBeenGrabbed) // 헝겊이 처음 그랩되었을 때만 실행
            {
                hasRagBeenGrabbed = true;
                if (subtitleText != null)
                {
                    subtitleText.text = "이제 수건을 물에 적셔야 해요!";
                    isSearchingForWater = true;
                    searchTimer = waterSearchTime;
                }
            }
        }

        void Update()
        {
            // Handle water search timer
            if (isSearchingForWater)
            {
                searchTimer -= Time.deltaTime;
                if (searchTimer <= 0)
                {
                    HighlightWater();
                    isSearchingForWater = false;
                }
            }

            // Handle wetting timer
            if (isWetting)
            {
                Debug.Log($"Wetting in progress: wetTimer = {wetTimer}, isWet = {isWet}");
                wetTimer += Time.deltaTime;
                if (wetTimer >= wetTimeRequired && !isWet)
                {
                    WetRag();
                    isWetting = false;
                }
            }

            // Breathing protection check
            if (isWet && headTransform != null)
            {
                float distanceToHead = Vector3.Distance(ragObject.transform.position, headTransform.position);
                if (!isProtecting && distanceToHead <= protectionDistance)
                {
                    SetProtectionState(true);
                }
                else if (isProtecting && distanceToHead > protectionDistance)
                {
                    SetProtectionState(false);
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            Debug.Log($"OnTriggerEnter called with {other.gameObject.name}");
            if (other.gameObject == waterObject && !isWet)
            {
                Debug.Log("Water detected, starting to wet the rag.");
                isSearchingForWater = false;
                isWetting = true;
                wetTimer = 0f;
                if (subtitleText != null)
                {
                    subtitleText.text = "잘했어! 충분한 시간동안 수건을 적시자.";
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            Debug.Log($"OnTriggerExit called with {other.gameObject.name}");
            if (other.gameObject == waterObject && !isWet)
            {
                Debug.Log("Water exited, stopping wetting process.");
                isWetting = false;
                if (subtitleText != null)
                {
                    subtitleText.text = "수건을 충분히 적셔야해! 5초 이상 유지해보자!";
                }
            }
        }

        void ShowSubtitle(string message)
        {
            if (subtitleText != null)
            {
                subtitleText.color = Color.black; // 자막 색상을 검정색으로 변경
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
                subtitleText.color = Color.black; // 깜빡임 종료 후 검정색으로 변경
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

        void WetRag()
        {
            isWet = true;
            if (cubeRenderer != null)
            {
                cubeRenderer.material.color = Color.green;
            }
            if (subtitleText != null)
            {
                subtitleText.text = "수건이 젖었어요! 이제 입 주변에 대보세요.";
            }
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
            if (subtitleText != null)
            {
                subtitleText.text = "물이 어디 있는지 찾아보세요!";
            }
        }

        void SetProtectionState(bool state)
        {
            if (isProtecting == state)
            {
                return;
            }

            isProtecting = state;
            if (subtitleText != null)
            {
                if (isProtecting)
                {
                    subtitleText.text = "호흡 보호가 활성화되었어요!";
                    if (!hasProtectionActivated)
                    {
                        onProtectionActivated?.Invoke();
                        hasProtectionActivated = true;
                        hasProtectionDeactivated = false;
                        // 5초 뒤에 다음 텍스트를 표시하기 위해 코루틴 시작
                        StartCoroutine(ShowNextSubtitleAfterDelay());
                    }
                    if (vignette != null)
                    {
                        vignette.intensity.Override(0f);
                    }
                }
                else
                {
                    // 비활성화 텍스트 제거
                    if (!hasProtectionDeactivated)
                    {
                        onProtectionDeactivated?.Invoke();
                        hasProtectionDeactivated = true;
                        hasProtectionActivated = false;
                    }
                    if (vignette != null)
                    {
                        vignette.intensity.Override(0.5f);
                    }
                }
            }
        }

        // 5초 뒤에 다음 텍스트를 표시하는 코루틴
        private IEnumerator ShowNextSubtitleAfterDelay()
        {
            yield return new WaitForSeconds(5f);
            if (subtitleText != null)
            {
                subtitleText.text = "다음은 탈출을 위해 밖으로 나갈 수 있는 출입문을 찾아보자";
            }
        }

        public void SetWaterSearchTime(float time)
        {
            waterSearchTime = time;
            searchTimer = waterSearchTime;
        }
    }
}