using UnityEngine;

public class SceneChanger: MonoBehaviour
{
    [Header("이동할 씬 이름")]
    public string targetSceneName;

    // 버튼 클릭 시 호출할 함수
    public void ChangeScene()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("이동할 씬 이름이 설정되지 않았습니다!");
        }
    }
}
