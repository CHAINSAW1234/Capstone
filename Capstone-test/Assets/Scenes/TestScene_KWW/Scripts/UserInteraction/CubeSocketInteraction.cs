using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace MyGame
{
    [RequireComponent(typeof(Renderer))]
    public class SocketInteraction : MonoBehaviour
    {
        [Header("Socket Settings")]
        [SerializeField]
        private GameObject socketObject; // 소켓으로 사용할 오브젝트

        private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socketInteractor; // 내부적으로 관리

        [Header("Interactable Settings")]
        [SerializeField]
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable itemInteractable;

        [Header("Feedback Events")]
        [SerializeField]
        private UnityEvent onItemInserted;
        [SerializeField]
        private UnityEvent onItemRemoved;

        [Header("Color Feedback Settings")]
        [SerializeField]
        private bool enableColorFeedback = true;
        [SerializeField]
        private Color grabbedColor = Color.red;
        [SerializeField]
        private Color correctPositionColor = Color.green;
        [SerializeField]
        private Color incorrectPositionColor = Color.red;
        [SerializeField]
        [Range(0f, 1f)]
        private float colorBlendAmount = 0.5f;

        private bool isItemSocketed = false;
        private bool isGrabbed = false;
        private Renderer itemRenderer;
        private Renderer socketRenderer;
        private Color originalItemColor;
        private Color originalSocketColor;

        void Reset()
        {
            SetupComponents();
        }

        private void OnValidate()
        {
            colorBlendAmount = Mathf.Clamp01(colorBlendAmount);
            SetupComponents();
        }

        private void SetupComponents()
        {
            // itemInteractable을 현재 오브젝트로 자동 설정
            itemInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (itemInteractable == null)
            {
                itemInteractable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                Debug.Log("XRGrabInteractable component added to: " + gameObject.name);
            }

            // Rigidbody 확인 및 추가
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;
                Debug.Log("Rigidbody component added and configured on: " + gameObject.name);
            }
            else
            {
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            // BoxCollider 확인 및 trigger 끄기 (Interactable 오브젝트)
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.isTrigger = false;
                Debug.Log("BoxCollider component added and isTrigger set to false on: " + gameObject.name);
            }
            else
            {
                boxCollider.isTrigger = false;
            }

            // socketObject가 할당된 경우, XRSocketInteractor와 BoxCollider 설정
            if (socketObject != null)
            {
                socketInteractor = socketObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
                if (socketInteractor == null)
                {
                    socketInteractor = socketObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
                    Debug.Log("XRSocketInteractor component added to: " + socketObject.name);
                }

                BoxCollider socketCollider = socketObject.GetComponent<BoxCollider>();
                if (socketCollider == null)
                {
                    socketCollider = socketObject.AddComponent<BoxCollider>();
                    socketCollider.isTrigger = true;
                    Debug.Log("BoxCollider component added and isTrigger set to true on: " + socketObject.name);
                }
                else
                {
                    socketCollider.isTrigger = true;
                }
            }

            // 렌더러 가져오기
            itemRenderer = GetComponent<Renderer>();
            if (itemRenderer != null)
            {
                originalItemColor = itemRenderer.material.color;
            }
            else
            {
                Debug.LogError("Interactable object has no Renderer component!");
            }
        }

        void Awake()
        {
            if (itemInteractable != null)
            {
                itemRenderer = GetComponent<Renderer>();
                if (itemRenderer != null)
                {
                    originalItemColor = itemRenderer.material.color;
                }
            }

            if (socketInteractor != null)
            {
                socketRenderer = socketInteractor.GetComponent<Renderer>();
                if (socketRenderer != null)
                {
                    originalSocketColor = socketRenderer.material.color;
                }
                else
                {
                    Debug.LogWarning("Socket object has no Renderer component. Assign one for color feedback.");
                }
            }
        }

        void Start()
        {
            if (socketInteractor == null)
            {
                Debug.LogError("Socket Interactor is not assigned! Please assign a socket object.");
                return;
            }

            if (itemInteractable == null)
            {
                Debug.LogError("Interactable object is not assigned!");
                return;
            }

            socketInteractor.interactionLayers = InteractionLayerMask.NameToLayer("Socketable");
            itemInteractable.interactionLayers = InteractionLayerMask.NameToLayer("Socketable");

            socketInteractor.selectEntered.AddListener(OnSelectEntered);
            socketInteractor.selectExited.AddListener(OnSelectExited);
            itemInteractable.selectEntered.AddListener(OnGrabbed);
            itemInteractable.selectExited.AddListener(OnReleased);
        }

        void OnDestroy()
        {
            if (socketInteractor != null)
            {
                socketInteractor.selectEntered.RemoveListener(OnSelectEntered);
                socketInteractor.selectExited.RemoveListener(OnSelectExited);
            }

            if (itemInteractable != null)
            {
                itemInteractable.selectEntered.RemoveListener(OnGrabbed);
                itemInteractable.selectExited.RemoveListener(OnReleased);
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactableObject == itemInteractable)
            {
                isItemSocketed = true;
                Debug.Log("Item inserted into socket!");
                onItemInserted?.Invoke();
                UpdateColors();
            }
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (args.interactableObject == itemInteractable)
            {
                isItemSocketed = false;
                Debug.Log("Item removed from socket!");
                onItemRemoved?.Invoke();
                UpdateColors();
            }
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            isGrabbed = true;
            UpdateColors();
            Debug.Log("Item grabbed, color updated.");
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            isGrabbed = false;
            UpdateColors();
            Debug.Log("Item released, color reset.");
        }

        private void UpdateColors()
        {
            if (!enableColorFeedback) return;

            if (itemRenderer != null)
            {
                if (isGrabbed)
                {
                    Color targetColor = isItemSocketed ? correctPositionColor : grabbedColor;
                    itemRenderer.material.color = Color.Lerp(originalItemColor, targetColor, colorBlendAmount);
                }
                else
                {
                    itemRenderer.material.color = originalItemColor;
                }
                Debug.Log($"Item color updated. Grabbed: {isGrabbed}, Socketed: {isItemSocketed}, Color: {itemRenderer.material.color}");
            }

            if (socketRenderer != null)
            {
                if (isGrabbed)
                {
                    Color targetColor = isItemSocketed ? correctPositionColor : incorrectPositionColor;
                    socketRenderer.material.color = Color.Lerp(originalSocketColor, targetColor, colorBlendAmount);
                }
                else
                {
                    socketRenderer.material.color = originalSocketColor;
                }
                Debug.Log($"Socket color updated. Grabbed: {isGrabbed}, Socketed: {isItemSocketed}, Color: {socketRenderer.material.color}");
            }
        }

        public bool IsItemSocketed => isItemSocketed;

        public void ToggleColorFeedback(bool enable)
        {
            enableColorFeedback = enable;
            if (!enable && itemRenderer != null && socketRenderer != null)
            {
                itemRenderer.material.color = originalItemColor;
                socketRenderer.material.color = originalSocketColor;
            }
            else
            {
                UpdateColors();
            }
        }
    }
}