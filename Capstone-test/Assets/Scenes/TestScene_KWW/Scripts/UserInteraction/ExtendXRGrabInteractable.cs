using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable), typeof(Rigidbody))]
public class ExtendXRGrabInteractable : MonoBehaviour
{
    [Header("Custom Grab Events")]
    public UnityEvent onGrabbed = new UnityEvent();
    public UnityEvent onReleased = new UnityEvent();

    [Header("Audio Settings")]
    [SerializeField] private bool enableAudio = true;
    [SerializeField] private AudioClip grabAudioClip;
    [SerializeField] private AudioClip releaseAudioClip;
    private AudioSource audioSource;

    [Header("VFX Settings")]
    [SerializeField] private bool enableVFX = true;
    [SerializeField] private ParticleSystem grabVFX;
    [SerializeField] private ParticleSystem releaseVFX;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private Transform interactorTransform;

    private void Awake()
    {
        SetupComponents();
        SetupEvents();
    }

    private void SetupComponents()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        rb.mass = 1f;
        rb.angularDamping = 0.05f;
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.constraints = RigidbodyConstraints.None;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        // ✅ 기본적으로 손 회전을 따라가도록 설정
        grabInteractable.trackRotation = true;
    }

    private void SetupEvents()
    {
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        onGrabbed?.Invoke();

        if (enableAudio && grabAudioClip != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(grabAudioClip);
        }

        if (enableVFX && grabVFX != null && !grabVFX.isPlaying)
        {
            grabVFX.Play();
        }

        interactorTransform = args.interactorObject.transform;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        onReleased?.Invoke();

        if (enableAudio && releaseAudioClip != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(releaseAudioClip);
        }

        if (enableVFX && releaseVFX != null && !releaseVFX.isPlaying)
        {
            releaseVFX.Play();
        }

        interactorTransform = null;
    }
}
