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
    private Transform FireExtinguisher;
    private Vector3 OriginPosition;
    private Quaternion OriginRotation;

    private bool isGrabbed = false;
    private bool isRestoring = false;
    private XRGrabInteractable grabInteractable;
    private IXRSelectInteractor currentInteractor;

    const float restoreSpeed = 5f;
    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(args =>
        {
            isGrabbed = true;
            isRestoring = false;
            currentInteractor = args.interactorObject;

        });
        grabInteractable.selectExited.AddListener(args =>
        {
            isGrabbed = false;
            isRestoring = true;
            currentInteractor = null;
        });

        OriginPosition = FireExtinguisher.InverseTransformPoint(NozzleBone.position);
        OriginRotation = Quaternion.Inverse(FireExtinguisher.rotation) * NozzleBone.rotation;
        if (NullCheck.Invoke(NozzleBone))
        {
            transform.SetWorldPose(NozzleBone.transform.GetWorldPose());
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (!isGrabbed)
        {
            if(isRestoring)
            {
                transform.position = Vector3.Lerp(transform.position, FireExtinguisher.TransformPoint(OriginPosition), Time.deltaTime * restoreSpeed);

                if (Vector3.Distance(transform.position, FireExtinguisher.TransformPoint(OriginPosition)) < 0.001f)
                {
                    isRestoring = false;

                }
            }
            else
            {
                transform.position = FireExtinguisher.TransformPoint(OriginPosition);
                transform.rotation = FireExtinguisher.rotation * OriginRotation;
            }
        }
    }

}
