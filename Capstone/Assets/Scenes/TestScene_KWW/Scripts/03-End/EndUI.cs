using UnityEngine;
using TMPro;
using FireEvacuation;

namespace FireEvacuation
{
    public class EndUI : MonoBehaviour
    {
        [Header("UI 설정")]
        [SerializeField] private GameObject previousUI; // 비활성화할 이전 UI
        [SerializeField] private GameObject endUI; // 활성화할 종료 UI
        [SerializeField] private TMP_Text elapsedTimeText; // 경과 시간을 표시할 TMP_Text
        [SerializeField] private TMP_Text sequenceText; // 시나리오 순서 텍스트

        [Header("타이머 설정")]
        [SerializeField] private TimeManager timeManager; // TimeManager 참조

        [Header("트리거 설정")]
        [SerializeField] private GameObject triggerObject; // 트리거 오브젝트
        [SerializeField] private string triggerTag = "Player"; // 트리거 태그
        [SerializeField] private bool isTriggered = false; // 트리거 상태

        private void Start()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            if (previousUI == null)
            {
                Debug.LogError("Previous UI가 지정되지 않았습니다!", this);
                return;
            }
            if (endUI == null)
            {
                Debug.LogError("End UI가 지정되지 않았습니다!", this);
                return;
            }
            if (elapsedTimeText == null)
            {
                Debug.LogError("Elapsed Time Text (TMP_Text)가 지정되지 않았습니다!", this);
                return;
            }
            if (sequenceText == null)
            {
                Debug.LogError("Sequence Text (TMP_Text)가 지정되지 않았습니다!", this);
                return;
            }
            if (timeManager == null)
            {
                Debug.LogError("TimeManager가 지정되지 않았습니다!", this);
                return;
            }
            if (triggerObject == null)
            {
                Debug.LogError("Trigger Object가 지정되지 않았습니다!", this);
                return;
            }

            Collider triggerCollider = triggerObject.GetComponent<Collider>();
            if (triggerCollider == null || !triggerCollider.isTrigger)
            {
                Debug.LogError("Trigger Object에 Collider가 없거나 IsTrigger가 활성화되지 않았습니다!", this);
                return;
            }

            endUI.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerObject == gameObject && !isTriggered && other.CompareTag(triggerTag))
            {
                isTriggered = true;
                Debug.Log("Trigger activated by: " + other.name);
                ActivateEndUI();
            }
        }

        private void Update()
        {
            if (isTriggered || triggerObject == gameObject) return;

            Collider triggerCollider = triggerObject.GetComponent<Collider>();
            if (triggerCollider != null)
            {
                Collider[] hitColliders = Physics.OverlapBox(triggerCollider.bounds.center, triggerCollider.bounds.extents, triggerCollider.transform.rotation);
                foreach (var hitCollider in hitColliders)
                {
                    if (hitCollider.CompareTag(triggerTag))
                    {
                        isTriggered = true;
                        Debug.Log("Trigger activated by: " + hitCollider.name);
                        ActivateEndUI();
                        break;
                    }
                }
            }
        }

        private void ActivateEndUI()
        {
            previousUI.SetActive(false);
            endUI.SetActive(true);

            timeManager.StopTimer();
            float elapsedTime = timeManager.GetElapsedTime();
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            elapsedTimeText.text = $"총 경과 시간: {minutes:00}:{seconds:00}";

            DisplaySequenceStatus();
        }

        private void DisplaySequenceStatus()
        {
            string[] steps = {
                "상황 인지",
                "호흡 보호",
                "문 탈출",
                "대피도 확인",
                "경보 울리기",
                "포복",
                "비상문 사용"
            };

            string sequenceDisplay = "";
            for (int i = 0; i < steps.Length; i++)
            {
                bool hasError = SequenceManager.Instance.HasSequenceError(i);
                string colorTag = hasError ? "<color=red>" : "<color=blue>";
                sequenceDisplay += $"{colorTag}{steps[i]}</color>";
                if (i < steps.Length - 1)
                {
                    sequenceDisplay += " - ";
                }
            }
            sequenceText.text = sequenceDisplay;
        }
    }
}