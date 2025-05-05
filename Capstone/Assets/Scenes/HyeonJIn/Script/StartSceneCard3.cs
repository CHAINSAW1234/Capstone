using TMPro;
using UnityEngine;
using static StartSceneUIManager;

public class StartSceneCard3 : MonoBehaviour
{
    [SerializeField]
    private StartSceneUIManager startSceneUIManager;

    private TextMeshProUGUI proUGUI;

    private void Start()
    {
        proUGUI = GetComponentInChildren<TextMeshProUGUI>();
    }
    void OnEnable()
    {
        StartSceneUIManager.StudyItem item = startSceneUIManager.Item;
        StartSceneUIManager.Mode mode = startSceneUIManager.ModeType;

        string FirstLine = item switch
        {
            StudyItem.FireEvacuation => "  선택하신 학습 항목은 화재 대피입니다.",
            StudyItem.FireExtinguisher => "  선택하신 학습 항목은 소화기 사용입니다.",
            _ => ""
        };

        string SecondLine = mode switch
        {
            Mode.Study => "선택하신 모드는 연습 모드입니다.",
            Mode.Evaluation => "선택하신 모드는 평가 모드입니다.",
            _ => ""
        };

        string[] lines = proUGUI.text.Split('\n');
        lines[0] = FirstLine;
        lines[1] = SecondLine;
        proUGUI.text = string.Join("\n", lines);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
