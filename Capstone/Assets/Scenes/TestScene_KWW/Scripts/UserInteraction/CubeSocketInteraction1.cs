using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class CubeSocketInteraction : MonoBehaviour
{
    [Header("Socket Settings")]
    [SerializeField]
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socketInteractor; // 소켓 인터랙터 컴포넌트

    [Header("Interactable Settings")]
    [SerializeField]
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable cubeInteractable; // 삽입 가능한 큐브

    [Header("Visual Feedback")]
    [SerializeField]
    private Color insertedColor = Color.red; // 삽입 시 색상
    [SerializeField]
    private Color defaultColor = Color.white; // 기본 색상

    private bool isCubeSocketed = false; // 소켓에 큐브가 삽입되었는지 추적
    private Renderer cubeRenderer; // 큐브의 렌더러 (색상 변경용)

    void Start()
    {
        // 소켓 인터랙터가 null인지 확인
        if (socketInteractor == null)
        {
            Debug.LogError("Socket Interactor is not assigned!");
            return;
        }

        // 큐브 인터랙터블이 null인지 확인
        if (cubeInteractable == null)
        {
            Debug.LogError("Cube Interactable is not assigned!");
            return;
        }

        // 큐브의 렌더러 가져오기
        cubeRenderer = cubeInteractable.GetComponent<Renderer>();
        if (cubeRenderer == null)
        {
            Debug.LogError("Cube Interactable does not have a Renderer component!");
            return;
        }

        // 초기 색상 설정
        cubeRenderer.material.color = defaultColor;

        // 이벤트 구독
        socketInteractor.selectEntered.AddListener(OnSelectEntered);
        socketInteractor.selectExited.AddListener(OnSelectExited);

        // Interaction Layers 설정 (interactionLayerMask 대신 interactionLayers 사용)
        socketInteractor.interactionLayers = InteractionLayerMask.NameToLayer("Socketable"); // "Socketable" 레이어에만 반응
        cubeInteractable.interactionLayers = InteractionLayerMask.NameToLayer("Socketable");
    }

    void OnDestroy()
    {
        // 이벤트 해제
        if (socketInteractor != null)
        {
            socketInteractor.selectEntered.RemoveListener(OnSelectEntered);
            socketInteractor.selectExited.RemoveListener(OnSelectExited);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // 소켓에 큐브가 삽입되었을 때 호출
        if (args.interactableObject == cubeInteractable)
        {
            isCubeSocketed = true;
            Debug.Log("Cube inserted into socket!");

            // 큐브 색상 변경
            if (cubeRenderer != null)
            {
                cubeRenderer.material.color = insertedColor;
            }
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        // 소켓에서 큐브가 제거되었을 때 호출
        if (args.interactableObject == cubeInteractable)
        {
            isCubeSocketed = false;
            Debug.Log("Cube removed from socket!");

            // 큐브 색상을 기본값으로 복원
            if (cubeRenderer != null)
            {
                cubeRenderer.material.color = defaultColor;
            }
        }
    }

    // Inspector에서 확인용
    public bool IsCubeSocketed => isCubeSocketed;
}