using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class StepInfoPopup : MonoBehaviour
{
    [Header("����� ���� UI")]
    public GameObject stepPopup;

    private void Start()
    {
        if (stepPopup != null)
        {
            stepPopup.SetActive(false); // �⺻������ ��Ȱ��ȭ
        }

        UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (interactable != null)
        {
            interactable.hoverEntered.AddListener(OnHoverEnter);
            interactable.hoverExited.AddListener(OnHoverExit);
        }
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (stepPopup != null)
        {
            stepPopup.SetActive(true);
        }
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        if (stepPopup != null)
        {
            stepPopup.SetActive(false);
        }
    }
}
