using UnityEngine;

namespace FireEvacuation
{
    public class SequenceManager : MonoBehaviour
    {
        public static SequenceManager Instance { get; private set; }

        // 시나리오 단계별 순서 오류 플래그
        private bool[] sequenceErrors = new bool[7]; // 7단계: 상황 인지, 호흡 보호, 문 탈출, 대피도 확인, 경보 울리기, 포복, 비상문 사용
        private bool[] sequenceCompleted = new bool[7]; // 각 단계 완료 여부
        private int currentStep = 0; // 현재 진행 중인 단계 (0-based index)

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // 특정 단계에서 순서 오류를 기록
        public void RecordSequenceError(int stepIndex)
        {
                sequenceErrors[stepIndex] = true;
                Debug.Log($"Sequence Error: Step {stepIndex} attempted before completing step {currentStep}");
        }

        // 단계 완료 기록
        public void CompleteStep(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= sequenceCompleted.Length) return;

            sequenceCompleted[stepIndex] = true;
            if (stepIndex == currentStep)
            {
                currentStep++;
            }
        }

        // 특정 단계의 순서 오류 여부 반환
        public bool HasSequenceError(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= sequenceErrors.Length) return false;
            return sequenceErrors[stepIndex];
        }

        // 특정 단계의 완료 여부 반환
        public bool IsStepCompleted(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= sequenceCompleted.Length) return false;
            return sequenceCompleted[stepIndex];
        }
    }
}