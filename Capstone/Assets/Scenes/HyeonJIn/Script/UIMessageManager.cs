using System.Collections;
using TMPro;
using UnityEngine;

public class UIMessageManager : MonoBehaviour
{
    [Header("UI 설정")]
    [SerializeField]
    private TMP_Text subtitleText; // 자막 텍스트 UI

    [Header("각 컴포넌트 선택")]
    [SerializeField]
    private FireExtinguisherController fireExtinguisher;
    [SerializeField]
    private NozzleController nozzle;
    [SerializeField]
    private GameObject fire;
    [SerializeField]
    private PinController pin;

    private const float textDelay = 5f;
    enum STATE { Start, PinOff, Targeting, Shot, Finish};
    private STATE state = STATE.Start;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SequenceTargeting());
    }

    // Update is called once per frame
    void Update()
    {
    }

    void SendString(string message)
    {
        if (NullCheck.Invoke(subtitleText))
        {
            subtitleText.color = Color.black;
            subtitleText.text = message;
            Debug.Log($"자막 표시: {message}");
        }
    }

    void NextState()
    {
        state = state + 1;
        StartCoroutine("Sequence" + state.GetType().GetEnumName(state));
    }
    IEnumerator SequenceStart()
    {
        SendString("소화기 사용 훈련에 오신 것을 환영합니다!");
        yield return new WaitForSeconds(textDelay);

        SendString("이 훈련은 화재 상황에서 소화기 사용 방법을 익히는 과정입니다.");
        yield return new WaitForSeconds(textDelay);

        SendString("단계별로 따라와서 소화기 사용법을 익혀보세요!");
        yield return new WaitForSeconds(textDelay);

        NextState();
        yield break;
    }

    IEnumerator SequencePinOff()
    {
        SendString("소화기 사용은 총 네 단계로 나누어 집니다.");
        yield return new WaitForSeconds(textDelay);

        SendString("우선 소화기 핀을 뽑아봅시다.");
        yield return new WaitForSeconds(textDelay);

        SendString("왼손으로 소화기 손잡이를 잡고 오른손으로 핀을 잡아서 뽑아보세요!");
        yield return new WaitForSeconds(textDelay);

        while(true)
        {
            if (!fireExtinguisher.IsPinOff && fireExtinguisher.TrySpray())
            {
                SendString("아직 핀을 뽑지 않았어요!");
                yield return new WaitForSeconds(textDelay);
            }

            if(fireExtinguisher.IsPinOff)
            {
                SendString("핀을 뽑았어요!");
                yield return new WaitForSeconds(textDelay);
                NextState();
                yield break;
            }

            yield return null;
        }

    }
    IEnumerator SequenceTargeting()
    {
        SendString("다음 단계는 노즐을 잡고 불쪽을 조준하는거에요");
        yield return new WaitForSeconds(textDelay);

        SendString("오른손으로 노즐을 잡고 불쪽을 조준하세요!");
        yield return new WaitForSeconds(textDelay);

        while (true)
        {
            if (!nozzle.IsGrabbed && fireExtinguisher.TrySpray())
            {
                SendString("노즐을 잡아야되요!");
                yield return new WaitForSeconds(textDelay);
            }

            if (nozzle.IsGrabbed && CheckTargeting())
            {
                SendString("조준이 완료됐어요!");
                yield return new WaitForSeconds(textDelay / 2);
                NextState();
                yield break;
            }

            yield return null;
        }

    }

    bool CheckTargeting()
    {
        Vector3 nozzlePosition = nozzle.transform.position;
        Vector3 firePosition = fire.transform.position;

        Vector3 look = (firePosition - nozzlePosition).normalized;
        Vector3 fireLook = nozzle.transform.up.normalized;

        float dot = Vector3.Dot(fireLook, look);
        float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;

        return angle < 30f;
    }

    IEnumerator SequenceShot()
    {
        SendString("마지막으로 분사를 통해 불을 끄면 되요");
        yield return new WaitForSeconds(textDelay);

        SendString("소화기를 잡은손의 버튼을 눌러서 소화기를 분사하세요!");
        yield return new WaitForSeconds(textDelay);

        if(!fire.activeSelf)
        {
            SendString("화제를 진압했어요!");
            yield return new WaitForSeconds(textDelay);

            SendString("이상으로 소화기 사용 훈련을 종료하겠습니다!");
            yield return new WaitForSeconds(textDelay);

            yield break;
        }
    }
}
