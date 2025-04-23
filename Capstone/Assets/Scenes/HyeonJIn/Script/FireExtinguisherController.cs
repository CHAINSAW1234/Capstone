using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class FireExtinguisherController : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private IXRSelectInteractor currentInteractor;
    private bool isGrabbed = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        NullCheck.Invoke(grabInteractable);
        grabInteractable.selectEntered.AddListener(args =>
        {
            isGrabbed = true;
            currentInteractor = args.interactorObject;
        });
        grabInteractable.selectExited.AddListener(args =>
        {
            isGrabbed = false;
            currentInteractor = null;
        });
    }

    // Update is called once per frame
    void Update()
    {
        if(isGrabbed)
        {
            if(NullCheck.Invoke(currentInteractor))
            {
                transform.forward = currentInteractor.transform.forward;
            }

        }
    }
}
