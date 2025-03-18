using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace MyGame
{
    public class CubeSocketInteraction : MonoBehaviour
    {
        [Header("Socket Settings")]
        [SerializeField]
        private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socketInteractor;

        [Header("Interactable Settings")]
        [SerializeField]
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable cubeInteractable;

        [Header("Feedback Events")]
        [SerializeField]
        private UnityEvent onCubeInserted;
        [SerializeField]
        private UnityEvent onCubeRemoved;

        private bool isCubeSocketed = false;

        void Start()
        {
            if (socketInteractor == null)
            {
                Debug.LogError("Socket Interactor is not assigned!");
                return;
            }

            if (cubeInteractable == null)
            {
                Debug.LogError("Cube Interactable is not assigned!");
                return;
            }

            socketInteractor.selectEntered.AddListener(OnSelectEntered);
            socketInteractor.selectExited.AddListener(OnSelectExited);

            socketInteractor.interactionLayers = InteractionLayerMask.NameToLayer("Socketable");
            cubeInteractable.interactionLayers = InteractionLayerMask.NameToLayer("Socketable");
        }

        void OnDestroy()
        {
            if (socketInteractor != null)
            {
                socketInteractor.selectEntered.RemoveListener(OnSelectEntered);
                socketInteractor.selectExited.RemoveListener(OnSelectExited);
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactableObject == cubeInteractable)
            {
                isCubeSocketed = true;
                Debug.Log("Cube inserted into socket!");

                onCubeInserted?.Invoke();
            }
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (args.interactableObject == cubeInteractable)
            {
                isCubeSocketed = false;
                Debug.Log("Cube removed from socket!");

                onCubeRemoved?.Invoke();
            }
        }

        public bool IsCubeSocketed => isCubeSocketed;
    }
}