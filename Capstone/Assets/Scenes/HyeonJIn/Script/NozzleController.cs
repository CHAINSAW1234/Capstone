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

    const float restoreSpeed = 5f;
    void Start()
    {
        var grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(args =>
        {
            isGrabbed = true;
            isRestoring = false;

        });
        grabInteractable.selectExited.AddListener(args =>
        {
            isGrabbed = false;
            isRestoring = true;
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
                transform.rotation = Quaternion.Lerp(transform.rotation, FireExtinguisher.rotation * OriginRotation, Time.deltaTime * restoreSpeed);
                if (Vector3.Distance(transform.position, FireExtinguisher.TransformPoint(OriginPosition)) < 0.01f)
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
