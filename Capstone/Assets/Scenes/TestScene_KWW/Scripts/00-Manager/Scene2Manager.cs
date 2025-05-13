using UnityEngine;

public class Scene2Manager : MonoBehaviour
{
    public enum Mode { Study = 0, Evaluation = 1, NULL }; // StartSceneUIManager와 동일한 enum 정의
    private Mode currentMode;

    [SerializeField]
    [Header("평가 모드에서 비활성화할 UI 오브젝트")]
    private GameObject uiObject1; // 첫 번째 UI 오브젝트
    [SerializeField]
    private GameObject uiObject2; // 두 번째 UI 오브젝트

    void Awake()
    {
        // PlayerPrefs에서 mode 값을 읽어옴
        int modeValue = PlayerPrefs.GetInt("mode", (int)Mode.NULL); // 기본값은 NULL
        currentMode = (Mode)modeValue;

        Debug.Log($"현재 모드: {currentMode}"); // 디버깅용

        // 평가 모드일 때 UI 비활성화
        if (currentMode == Mode.Evaluation)
        {
            if (uiObject1 != null)
                uiObject1.SetActive(false);
            if (uiObject2 != null)
                uiObject2.SetActive(false);
        }
    }

    // 다른 스크립트에서 현재 모드를 확인할 수 있도록 public 메서드 제공
    public Mode GetCurrentMode()
    {
        return currentMode;
    }
}