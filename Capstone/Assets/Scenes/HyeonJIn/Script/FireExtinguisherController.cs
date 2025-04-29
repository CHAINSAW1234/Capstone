using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.InputSystem;

public class FireExtinguisherController : MonoBehaviour
{
    [SerializeField]
    private InputActionReference rightTrigger;
    [SerializeField]
    private InputActionReference leftTrigger;
    [SerializeField]
    private ParticleSystem sprayParticle;
    private InputAction targetAction;

    private XRGrabInteractable grabInteractable;
    private bool isGrabbed = false;

    private bool isPinOff = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var grabInteractable = GetComponent<XRGrabInteractable>();
        NullCheck.Invoke(grabInteractable);

        grabInteractable.selectEntered.AddListener(args =>
        {
            isGrabbed = true;
            if (args.interactorObject.transform.name.Contains("Left"))
            {
                targetAction = leftTrigger.action;
            }
            else
            {
                targetAction = rightTrigger.action;
            }
        });

        grabInteractable.selectExited.AddListener(args =>
        {
            isGrabbed = false;
            targetAction = null;
        });
    }

    // Update is called once per frame
    void Update()
    {
        if(isGrabbed && isPinOff && NullCheck.Invoke(targetAction))
        {
            float value = targetAction.ReadValue<float>();
            if(value > 0.1)
            {
                StartSpray();
            }
            else
            {
                StopSpray();
            }

        }
    }

    public void PinOff()
    {
        isPinOff = true;
    }

    void StartSpray()
    {
        if (!sprayParticle.isPlaying)
            sprayParticle.Play();
    }
    void StopSpray()
    {
        if (sprayParticle.isPlaying)
            sprayParticle.Stop();
    }

}
