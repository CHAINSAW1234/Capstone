using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class NozzleController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]
    private Transform NozzleBone;
    [SerializeField]
    private Transform NozzleBoneEnd;

    private bool isGrabbed = false;
    private XRGrabInteractable grabInteractable;
    private IXRSelectInteractor currentInteractor;

    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
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

        if (NullCheck.Invoke(NozzleBone) && NullCheck.Invoke(NozzleBoneEnd))
        {
            Pose pose = NozzleBone.transform.GetWorldPose();
            pose.position = (pose.position + NozzleBoneEnd.transform.position) / 2;
            transform.SetWorldPose(pose);
        }

    }

    // Update is called once per frame
    void Update()
    {

        //if (isGrabbed)
        //{
        //    OnNozzleGrabbed();
        //}
        //Pose pose = NozzleBone.transform.GetWorldPose();
        //pose.position = (pose.position + NozzleBoneEnd.transform.position) / 2;
        //transform.SetWorldPose(pose);
    }

    private void OnNozzleGrabbed()
    {
        if (NullCheck.Invoke(currentInteractor))
        {
            NozzleBone.position = currentInteractor.transform.position;
            NozzleBone.forward = currentInteractor.transform.forward;
        }

    }
}
