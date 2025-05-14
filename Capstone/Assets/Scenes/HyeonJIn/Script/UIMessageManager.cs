using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using FireEvacuation;
using UnityEngine.UI;

namespace FireEvacuation
{
    public class UIMessageManager : MonoBehaviour
    {
        [Header("UI 설정")]
        [SerializeField] private TMP_Text subtitleText; // 자막 텍스트 UI
        [SerializeField] private TMP_Text elapsedTimeText; // 경과 시간 텍스트 UI
        [SerializeField] private GameObject endUI; // 훈련 종료 UI
        [SerializeField] private GameObject previousUI; // 비활성화할 이전 UI
        [SerializeField] private RawImage stepImages;
        [SerializeField] private Texture2D incorrectTextures;
        [SerializeField] private TMP_Text FeedbackText; // 텍스트 위치

        [Header("타이머 설정")]
        [SerializeField] private TimeManager timeManager; // TimeManager 참조

        [Header("각 컴포넌트 선택")]
        [SerializeField] private FireExtinguisherController fireExtinguisher;
        [SerializeField] private GameObject extinguisherObject; // 소화기 오브젝트
        [SerializeField] private NozzleController nozzle;
        [SerializeField] private GameObject fire;
        [SerializeField] private PinController pin;

        [Header("아웃라인 설정")]
        [SerializeField] private Color outlineColor = Color.red; // 아웃라인 색상
        [SerializeField] private float outlineWidth = 2f; // 아웃라인 두께
        [SerializeField] private GameObject outlineExtinguisherObject; // 소화기 아웃라인용 오브젝트
        [SerializeField] private GameObject outlinePinObject; // 핀 아웃라인용 오브젝트

        [Header("화살표 설정")]
        [SerializeField] private GameObject extinguisherArrow; // 소화기 집기용 화살표
        [SerializeField] private GameObject fireArrow; // 화재 진압용 화살표
        [SerializeField] private GameObject NozzleArrow; // 화재 진압용 화살표

        [Header("컨트롤러 전환 설정")]
        [SerializeField] private GameObject[] previousControllers; // 이전 컨트롤러들
        [SerializeField] private GameObject[] newControllers; // 전환할 컨트롤러들

        private const float textDelay = 5f;
        private enum STATE { Start, PickUp, PinOff, Targeting, Shot, Finish }
        private STATE state = STATE.Start;
        private bool isPracticeMode = true;
        private Mode currentMode;

        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable extinguisherGrabInteractable; // 소화기 grab 상태 관리
        private bool hasExtinguisherGrabbed = false; // 소화기 잡힌 상태 추적

        public enum Mode { Study = 0, Evaluation = 1, NULL };

        void Awake()
        {
            SetMode();
        }

        void Start()
        {
            endUI.SetActive(false);

            if (extinguisherArrow != null) extinguisherArrow.SetActive(false);
            if (fireArrow != null) fireArrow.SetActive(false);
            if (NozzleArrow != null) NozzleArrow.SetActive(false);

            if (!isPracticeMode)
            {
                if (subtitleText != null) subtitleText.gameObject.SetActive(false);
            }

            extinguisherGrabInteractable = extinguisherObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            extinguisherGrabInteractable.selectEntered.AddListener(OnExtinguisherGrabbed);

            StartCoroutine(SequenceStart());
        }

        void SetMode()
        {
            int modeValue = PlayerPrefs.GetInt("mode", (int)Mode.NULL);
            currentMode = (Mode)modeValue;

            if (currentMode == Mode.Study)
            {
                isPracticeMode = true;
            }
            else if (currentMode == Mode.Evaluation)
            {
                isPracticeMode = false;
            }
            else
            {
                Debug.Log("모드가 설정되지 않았습니다.");
            }
        }

        void OnExtinguisherGrabbed(SelectEnterEventArgs args)
        {
            if (!hasExtinguisherGrabbed)
            {
                hasExtinguisherGrabbed = true;
            }
        }

        void SendString(string message)
        {
            if (NullCheck.Invoke(subtitleText) && isPracticeMode)
            {
                subtitleText.color = Color.black;
                subtitleText.text = message;
            }
        }

        void NextState()
        {
            state = (STATE)((int)state + 1);
            StartCoroutine("Sequence" + state.ToString());
        }

        IEnumerator SequenceStart()
        {
            Debug.Log("✅ Targeting Start");
            if (isPracticeMode)
            {
                SendString("소화기 사용 훈련에 오신 것을 환영합니다!");
                yield return new WaitForSeconds(textDelay);

                SendString("이 훈련은 화재 상황에서 소화기 사용 방법을 익히는 과정입니다.");
                yield return new WaitForSeconds(textDelay);

                SendString("단계별로 따라와서 소화기 사용법을 익혀봅시다!");
                yield return new WaitForSeconds(textDelay);
            }

            NextState();
            yield break;
        }

        IEnumerator SequencePickUp()
        {
            Debug.Log("✅ Targeting PickUp");
            if (isPracticeMode)
            {
                SendString("먼저 소화기를 들어야 합니다! 왼손으로 잡아 들어 올려 봅시다!");
                if (outlineExtinguisherObject != null) AddOutline(outlineExtinguisherObject);
                if (extinguisherArrow != null) extinguisherArrow.SetActive(true);
                yield return new WaitForSeconds(textDelay);
            }

            while (true)
            {
                if (!hasExtinguisherGrabbed && (pin != null && fireExtinguisher.TrySpray()))
                {
                    SequenceManagerEx.Instance.RecordSequenceError(0);
                    if (isPracticeMode)
                    {
                        SendString("소화기를 먼저 들어야 합니다!");
                        yield return new WaitForSeconds(textDelay);
                    }
                    yield return null;
                }
                else if (hasExtinguisherGrabbed)
                {
                    SequenceManagerEx.Instance.CompleteStep(0);
                    if (isPracticeMode)
                    {
                        SendString("소화기를 들었습니다!");
                        if (extinguisherArrow != null) extinguisherArrow.SetActive(false);
                        yield return new WaitForSeconds(textDelay);
                    }
                    if (outlineExtinguisherObject != null) RemoveOutline(outlineExtinguisherObject);
                    NextState();
                    yield break;
                }
                yield return null;
            }
        }

        IEnumerator SequencePinOff()
        {
            Debug.Log("✅ Targeting PinOff");
            if (isPracticeMode)
            {
                SendString("다음으로 소화기 핀 제거입니다.");
                yield return new WaitForSeconds(textDelay);

                SendString("오른손으로 다음 핀을 잡아서 뽑아봅시다!");
                if (outlinePinObject != null) AddOutline(outlinePinObject);
                yield return new WaitForSeconds(textDelay);
            }

            while (true)
            {
                if (pin != null && !fireExtinguisher.IsPinOff && fireExtinguisher.TrySpray())
                {
                    SequenceManagerEx.Instance.RecordSequenceError(1);
                    if (isPracticeMode)
                    {
                        SendString("아직 핀을 뽑지 않았습니다!");
                        yield return new WaitForSeconds(textDelay);
                    }
                    FeedbackText.text = "핀 뽑기를 생략했습니다.";
                    stepImages.texture = incorrectTextures;
                    yield return null;
                }
                else if (pin != null && fireExtinguisher.IsPinOff)
                {
                    SequenceManagerEx.Instance.CompleteStep(1);
                    if (isPracticeMode)
                    {
                        SendString("핀을 뽑았습니다!");
                        yield return new WaitForSeconds(textDelay);
                    }
                    if (outlinePinObject != null) RemoveOutline(outlinePinObject);
                    NextState();
                    yield break;
                }
                yield return null;
            }
        }

        IEnumerator SequenceTargeting()
        {
            Debug.Log("✅ Targeting Sequence");
            if (isPracticeMode)
            {
                SendString("다음 소화기를 분사하는 방법에 대해 알아봅시다.");
                yield return new WaitForSeconds(textDelay);

                SendString("오른손으로 노즐을 잡아 뽑은 뒤 불쪽을 향해봅시다!");
                if (NozzleArrow != null) NozzleArrow.SetActive(true);
                yield return new WaitForSeconds(textDelay);
            }

            while (true)
            {
                if (!nozzle.IsGrabbed && (pin != null && fireExtinguisher.TrySpray()))
                {
                    SequenceManagerEx.Instance.RecordSequenceError(2);
                    if (isPracticeMode)
                    {
                        SendString("노즐을 잡아야 합니다!");
                        yield return new WaitForSeconds(textDelay);
                    }
                }
                else if (nozzle.IsGrabbed && CheckTargeting())
                {
                    SequenceManagerEx.Instance.CompleteStep(2);
                    if (isPracticeMode)
                    {
                        SendString("소화기를 분사할 준비가 되었습니다!"); 
                        yield return new WaitForSeconds(textDelay);
                    }
                    if (NozzleArrow != null) NozzleArrow.SetActive(false);
                    NextState();
                    yield break;
                }
                yield return null;
            }
        }

        IEnumerator SequenceShot()
        {
            Debug.Log("✅ Targeting Shot");
            if (isPracticeMode)
            {
                SendString("마지막으로 소화기 분사를 통해 불을 끄면 됩니다!");
                yield return new WaitForSeconds(textDelay);

                if (fireArrow != null) fireArrow.SetActive(true);

                SendString("소화기를 잡은 왼 손의 버튼을 눌러서 소화기를 분사해봅시다!");
                yield return new WaitForSeconds(textDelay);
            }


            while (true)
            {
                if (fire.activeSelf)
                {
                    if (!nozzle.IsGrabbed || (pin != null && !fireExtinguisher.IsPinOff))
                    {
                        SequenceManagerEx.Instance.RecordSequenceError(3);
                        if (isPracticeMode)
                        {
                            SendString("노즐을 잡고 핀이 뽑힌 상태에서 분사해야 합니다!");
                        }
                    }
                    else
                    {
                        if (isPracticeMode)
                        {
                            SendString("화재가 진압되고 있습니다. 계속 분사해서 불을 꺼주세요!");
                            yield return new WaitForSeconds(textDelay);
                        }
                    }
                    yield return null;
                }
                else
                {
                    if (isPracticeMode)
                    {
                        SendString("화재를 진압했습니다!");
                        SequenceManagerEx.Instance.CompleteStep(3);
                        if (fireArrow != null) fireArrow.SetActive(false);
                        yield return new WaitForSeconds(textDelay);
                    }
                    NextState();
                    yield break;
                }
                yield return null;
            }
        }

        IEnumerator SequenceFinish()
        {
            Debug.Log("✅ Targeting Finish");
            if (isPracticeMode)
            {
                SendString("훈련이 완료되었습니다! 수고하셨습니다!");
                yield return new WaitForSeconds(textDelay);
            }

            previousUI.SetActive(false);
            endUI.SetActive(true);

            if (timeManager != null && elapsedTimeText != null)
            {
                timeManager.StopTimer();
                float elapsedTime = timeManager.GetElapsedTime();
                int minutes = Mathf.FloorToInt(elapsedTime / 60f);
                int seconds = Mathf.FloorToInt(elapsedTime % 60f);
                elapsedTimeText.text = $"총 경과 시간: {minutes:00}:{seconds:00}";
            }
            else
            {
                Debug.LogWarning("TimeManager 또는 ElapsedTimeText가 지정되지 않았습니다!");
            }

            foreach (var controller in previousControllers)
            {
                if (controller != null) controller.SetActive(false);
            }
            foreach (var controller in newControllers)
            {
                if (controller != null) controller.SetActive(true);
            }

            yield break;
        }

        bool CheckTargeting()
        {
            Debug.Log("타겟 되었습니다.");
            Vector3 nozzlePosition = nozzle.transform.position;
            Vector3 firePosition = fire.transform.position;

            Vector3 look = (firePosition - nozzlePosition).normalized;
            Vector3 fireLook = nozzle.transform.up.normalized;

            float dot = Vector3.Dot(fireLook, look);
            float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;

            return angle < 30f;
        }

        void AddOutline(GameObject target)
        {
            if (target == null || !isPracticeMode) return;

            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.AddComponent<Outline>();
            }

            outline.OutlineMode = Outline.Mode.OutlineAll;
            outline.OutlineColor = outlineColor;
            outline.OutlineWidth = outlineWidth;
            outline.enabled = true;
        }

        void RemoveOutline(GameObject target)
        {
            if (target == null) return;

            Outline outline = target.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = false;
            }
        }
    }
}