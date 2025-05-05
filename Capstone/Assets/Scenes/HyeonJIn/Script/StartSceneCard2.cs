using TMPro;
using UnityEngine;
using static StartSceneUIManager;

public class StartSceneCard2 : MonoBehaviour
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

        string FirstLine = item switch
        {
            StudyItem.FireEvacuation => "  선택하신 학습 항목은 화재 대피입니다.",
            StudyItem.FireExtinguisher => "  선택하신 학습 항목은 소화기 사용입니다.",
            _ => ""
        };


        string[] lines = proUGUI.text.Split('\n');
        lines[0] = FirstLine;
        proUGUI.text = string.Join("\n", lines);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
