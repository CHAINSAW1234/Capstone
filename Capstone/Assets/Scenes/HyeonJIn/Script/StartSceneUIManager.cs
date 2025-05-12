using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static StartSceneUIManager;

public class StartSceneUIManager : MonoBehaviour
{
    public enum StudyItem { FireEvacuation = 0, FireExtinguisher = 1, NULL };
    public enum Mode { Study = 0, Evaluation = 1, NULL };

    [System.Serializable]
    public class NamedString
    {
#if UNITY_EDITOR
        public UnityEditor.SceneAsset reference;
#endif
        public string str;
    }

    [System.Serializable]
    public class SceneGroupEntry
    {
        public StudyItem item;
        public List<NamedString> scenes = new();
    }

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

    [SerializeField]
    [Header("이동할 씬 이름 지정")]
    private List<SceneGroupEntry> sceneNames;
    private Dictionary<StudyItem, List<NamedString>> sceneNamesDict;

    private void Awake()
    {
        sceneNamesDict = new Dictionary<StudyItem, List<NamedString>>();
        foreach (var entry in sceneNames)
        {
            if (!sceneNamesDict.ContainsKey(entry.item))
                sceneNamesDict[entry.item] = new List<NamedString>();

            sceneNamesDict[entry.item].AddRange(entry.scenes);
        }
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
        PlayerPrefs.SetInt("mode", (int)mode);

        if(sceneNamesDict.TryGetValue(item, out var Target))
        {
            SceneManager.LoadScene(Target[Random.Range(0, Target.Count)].str);
        }
    }
}
