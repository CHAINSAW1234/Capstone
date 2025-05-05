using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StartSceneUIManager : MonoBehaviour
{
    public enum StudyItem { FireEvacuation = 0, FireExtinguisher = 1, NULL };
    public enum Mode { Study = 0, Evaluation = 1, NULL };

    [SerializeField]
    private List<GameObject> step;
    int currentStepIndex = 0;

    private StudyItem item = StudyItem.NULL;
    private Mode mode = Mode.NULL;

    public StudyItem Item
    {
        get => item;
    }

    public Mode ModeType
    {
        get => mode;
    }


    void LateUpdate()
    {
        Vector3 direction = transform.position - Camera.main.transform.position;
        direction.y = 0;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = targetRotation;
        }
    }
    public void Next()
    {
        step[currentStepIndex].SetActive(false);
        ++currentStepIndex;
        step[currentStepIndex].SetActive(true);
    }
    public void Back()
    {
        step[currentStepIndex].SetActive(false);
        --currentStepIndex;
        step[currentStepIndex].SetActive(true);
    }

    public void SetStudyItem(int _item)
    {
        item = (StudyItem)_item;
    }

    public void SetMode(int _mode)
    {
        mode = (Mode)_mode;
    }

    public void ChangeScene()
    {

    }
}
