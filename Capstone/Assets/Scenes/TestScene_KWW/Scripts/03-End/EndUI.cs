using UnityEngine;
using TMPro;
using UnityEngine.UI;
using FireEvacuation;

namespace FireEvacuation
{
    public class EndUI : MonoBehaviour
    {
        [Header("UI 설정")]
        [SerializeField] private GameObject previousUI; // 비활성화할 이전 UI
        [SerializeField] private GameObject endUI; // 활성화할 종료 UI
        [SerializeField] private TMP_Text elapsedTimeText; // 경과 시간을 표시할 TMP_Text
        [SerializeField] private RawImage[] stepImages; // 단계별 출력 이미지 오브젝트 (총 7개, RawImage)
        [SerializeField] private Texture2D[] incorrectTextures; // 틀렸을 때 사용할 텍스처들 (총 7개)

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
            if (stepImages == null || stepImages.Length != 7)
            {
                Debug.LogError("StepImages가 정확히 7개가 아닙니다!", this);
                return;
            }
            if (incorrectTextures == null || incorrectTextures.Length != 7)
            {
                Debug.LogError("IncorrectTextures가 정확히 7개가 아닙니다!", this);
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
            for (int i = 0; i < stepImages.Length; i++)
            {
                if (SequenceManager.Instance.HasSequenceError(i))
                {
                    stepImages[i].texture = incorrectTextures[i];
                }
            }
        }
    }
}
