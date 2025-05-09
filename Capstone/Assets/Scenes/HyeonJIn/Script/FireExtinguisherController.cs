using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.InputSystem;
using TMPro;

public class FireExtinguisherController : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem sprayParticle;
    [SerializeField]
    private InputActionReference rightTrigger;
    [SerializeField]
    private InputActionReference leftTrigger;
    private InputAction targetAction;

    private bool isGrabbed = false;

    public bool IsPinOff {
        get  => isPinOff;
    }
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
        if(isPinOff && TrySpray())
        {
            StartSpray();
        }
        else
        {
            StopSpray();
        }
    }

    public bool TrySpray()
    {
        if(isGrabbed && NullCheck.Invoke(targetAction)) {
            return targetAction.ReadValue<float>() > 0.1;
        }

        return false;
    }

    private void StartSpray()
    {
        if (!sprayParticle.isPlaying)
            sprayParticle.Play();
    }
    private void StopSpray()
    {
        if (sprayParticle.isPlaying)
            sprayParticle.Stop();
    }
    public void PinOff()
    {
        isPinOff = true;
    }

}
