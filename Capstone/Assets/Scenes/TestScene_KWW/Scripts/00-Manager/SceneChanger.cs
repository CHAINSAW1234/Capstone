using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneChanger : MonoBehaviour
{
    [Header("이동할 씬 오브젝트들")]
    [SerializeField] private Object[] targetSceneAssets; // SceneAsset을 저장할 배열

    // 버튼 클릭 시 호출할 함수
    public void ChangeScene()
    {
        if (targetSceneAssets != null && targetSceneAssets.Length > 0)
        {
            // 배열에서 랜덤으로 하나의 SceneAsset 선택
            Object randomSceneAsset = targetSceneAssets[Random.Range(0, targetSceneAssets.Length)];

            // SceneAsset에서 씬 이름 추출
            string sceneName = GetSceneName(randomSceneAsset);

            if (!string.IsNullOrEmpty(sceneName))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning("선택된 씬 이름이 유효하지 않습니다!");
            }
        }
        else
        {
            Debug.LogWarning("이동할 씬 오브젝트 목록이 설정되지 않았거나 비어있습니다!");
        }
    }

    // SceneAsset에서 씬 이름을 추출하는 함수
    private string GetSceneName(Object sceneAsset)
    {
#if UNITY_EDITOR
        if (sceneAsset is SceneAsset)
        {
            string assetPath = AssetDatabase.GetAssetPath(sceneAsset);
            return System.IO.Path.GetFileNameWithoutExtension(assetPath);
        }
#endif
        Debug.LogWarning("런타임에서는 SceneAsset의 이름이 필요합니다!");
        return string.Empty;
    }
}