using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PinController : MonoBehaviour
{

    public Transform fireExtinguisher;      // 소화기 본체
    [SerializeField]
    private float leftLimit = 0.0f;         // z축 음수 방향 최대치 (좌측)
    [SerializeField]
    private float rightLimit = 0.1f;        // z축 양수 방향 최대치 (우측)
    [SerializeField]
    private float detachDistance = 0.1f;    // 우측으로 이만큼 이동하면 분리됨

    private Vector3 transformBias;

    private XRGrabInteractable grabInteractable;
    private IXRSelectInteractor currentInteractor;
    private bool isPulled = false;
    private bool isGrabbed = false;

    [SerializeField]
    public UnityEvent PinOffEvent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transformBias = transform.localPosition;
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
        if (isPulled)
            return;

        if(isGrabbed)
        {
            OnPinGrabbed();
        }
    }

    void OnPinGrabbed()
    {
        Vector3 basePosition = fireExtinguisher.position + fireExtinguisher.rotation * transformBias;
        Vector3 pinToHand = currentInteractor.transform.position - basePosition;

        Vector3 moveAxis = fireExtinguisher.right;
        float projectedDistance = Vector3.Dot(pinToHand, moveAxis);

        // 거리 제한 적용
        float clamped = Mathf.Clamp(projectedDistance, leftLimit, rightLimit);
        Vector3 offset = moveAxis * clamped;

        // 최종 위치 적용
        transform.position = basePosition + offset;

        // 뽑힘 처리
        if (!isPulled && clamped >= detachDistance)
        {
            isPulled = true;
            OnPinPulled();
        }
    }

    void OnPinPulled()
    {
        PinOffEvent.Invoke();
        grabInteractable.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        NullCheck.Invoke(rb);
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearDamping = 0f;
    }

}
