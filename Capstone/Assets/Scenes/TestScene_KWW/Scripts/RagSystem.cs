using UnityEngine;
using TMPro; // TextMeshPro namespace
using UnityEngine.UI; // For UI if needed
using UnityEngine.Events; // UnityEvent를 사용하기 위해
using UnityEngine.Rendering; // URP/HDRP의 Volume 시스템
using UnityEngine.Rendering.Universal; // URP 전용 (HDRP라면 HighDefinition 사용)

#pragma warning disable 0618 // Suppress obsolete warning for XRBaseController

public class RagSystem : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Renderer cubeRenderer;
    private bool isWet = false;
    private bool isProtecting = false;
    public Transform headTransform; // Reference to XR Rig's Main Camera
    public float protectionDistance = 0.2f; // Detection distance from head
    public TMP_Text subtitleText;

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

    // URP의 Volume과 Vignette 설정
    public Volume volume; // URP/HDRP의 Volume 참조
    private Vignette vignette; // URP/HDRP의 Vignette

    // 이벤트가 한 번만 호출되도록 추적
    private bool hasProtectionActivated = false; // 활성화 이벤트가 호출되었는지
    private bool hasProtectionDeactivated = false; // 비활성화 이벤트가 호출되었는지

    void Start()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        cubeRenderer = GetComponent<Renderer>();
        cubeRenderer.material = new Material(cubeRenderer.material); // Material instantiation
        cubeRenderer.material.color = Color.red; // Initial color (red)

        if (headTransform == null)
        {
            headTransform = GameObject.Find("Main Camera").transform; // XR Rig's camera
        }

        if (subtitleText != null)
        {
            subtitleText.text = "이제 수건을 물에 적셔야 해요!"; // Initial instruction
            isSearchingForWater = true; // Start searching for water
            searchTimer = waterSearchTime; // Set timer
        }

        // URP/HDRP의 Volume에서 Vignette 설정 가져오기
        if (volume != null)
        {
            if (volume.profile.TryGet(out vignette))
            {
                vignette.active = true; // URP/HDRP에서는 active 속성 사용
                vignette.intensity.Override(0.5f); // 초기 강도 설정 (호흡 보호 없음 상태)
            }
            else
            {
                Debug.LogError("Vignette 설정을 Volume에서 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.LogError("Volume이 지정되지 않았습니다!");
        }

        // 초기 상태 설정: 메시지 출력 없이 호흡 보호 비활성화 상태로 시작
        isProtecting = false;
        if (vignette != null)
        {
            vignette.intensity.Override(0.5f); // 초기 Vignette 강도 설정
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
                HighlightWater(); // Highlight water after time runs out
                isSearchingForWater = false; // Stop the search timer
            }
        }

        // Handle wetting timer
        if (isWetting)
        {
            wetTimer += Time.deltaTime;
            if (wetTimer >= wetTimeRequired && !isWet)
            {
                WetRag(); // Successfully wet the rag
                isWetting = false;
            }
        }

        // Breathing protection check
        if (isWet)
        {
            float distanceToHead = Vector3.Distance(transform.position, headTransform.position);
            if (!isProtecting && distanceToHead <= protectionDistance)
            {
                SetProtectionState(true); // Activate breathing protection
            }
            else if (isProtecting && distanceToHead > protectionDistance)
            {
                SetProtectionState(false); // Deactivate breathing protection
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water") && !isWet)
        {
            isSearchingForWater = false; // Stop searching for water
            isWetting = true; // Start wetting process
            wetTimer = 0f; // Reset wetting timer
            if (subtitleText != null)
            {
                subtitleText.text = "잘했어! 충분한 시간동안 수건을 적시자.";
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Water") && !isWet)
        {
            isWetting = false; // Stop wetting process
            if (subtitleText != null)
            {
                subtitleText.text = "수건을 충분히 적셔야해! 5초 이상 유지해보자!";
            }
        }
    }

    // Wet the rag
    void WetRag()
    {
        isWet = true;
        cubeRenderer.material.color = Color.green; // Change to green
        if (subtitleText != null)
        {
            subtitleText.text = "수건이 젖었어요! 이제 입 주변에 대보세요.";
        }
    }

    // Highlight water (visual feedback)
    void HighlightWater()
    {
        GameObject[] waterObjects = GameObject.FindGameObjectsWithTag("Water");
        foreach (GameObject water in waterObjects)
        {
            Renderer waterRenderer = water.GetComponent<Renderer>();
            if (waterRenderer != null)
            {
                waterRenderer.material.color = Color.yellow; // Example: Highlight water in yellow
            }
        }
        if (subtitleText != null)
        {
            subtitleText.text = "물이 어디 있는지 찾아보세요!";
        }
    }

    // Set breathing protection state with UnityEvents and Vignette control
    void SetProtectionState(bool state)
    {
        // 상태가 변경되지 않았다면 아무 작업도 하지 않음 (이벤트 중복 호출 방지)
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
                if (!hasProtectionActivated) // 한 번만 호출되도록 체크
                {
                    onProtectionActivated?.Invoke(); // UnityEvent 호출
                    hasProtectionActivated = true; // 활성화 이벤트 호출 완료
                    hasProtectionDeactivated = false; // 비활성화 플래그 리셋
                }
                if (vignette != null)
                {
                    vignette.intensity.Override(0f); // Vignette 제거 (호흡 보호 활성화)
                }
            }
            else
            {
                subtitleText.text = "호흡 보호가 비활성화되었어요.";
                if (!hasProtectionDeactivated) // 한 번만 호출되도록 체크
                {
                    onProtectionDeactivated?.Invoke(); // UnityEvent 호출
                    hasProtectionDeactivated = true; // 비활성화 이벤트 호출 완료
                    hasProtectionActivated = false; // 활성화 플래그 리셋
                }
                if (vignette != null)
                {
                    vignette.intensity.Override(0.5f); // Vignette 적용 (호흡 보호 비활성화)
                }
            }
        }
    }

    // Allow user to set the water search time (call this from UI or elsewhere)
    public void SetWaterSearchTime(float time)
    {
        waterSearchTime = time;
        searchTimer = waterSearchTime; // Reset timer when updated
    }
}