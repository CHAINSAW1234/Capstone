using System.Collections.Generic;
using UnityEngine;

public class ObjectLocator : MonoBehaviour
{
    [System.Serializable]
    public class KeywordTarget
    {
        public string keyword;           // 예: "문"
        public OutlineTarget target;     // 연결할 오브젝트
    }

    public List<KeywordTarget> keywordMappings;
    public Transform playerHeadTransform; // XR Rig의 Main Camera 또는 Center Eye

    // 위치 질문으로 인식할 키워드 (확장 가능)
    private readonly string[] locationHints = {
    "어디", "위치", "못 찾겠어", "보이지", "찾을 수 없어", "안 보여"
};


    public void CheckAndHighlight(string userInput)
    {
        if (string.IsNullOrEmpty(userInput)) return;

        foreach (var mapping in keywordMappings)
        {
            string keyword = mapping.keyword;

            // 오브젝트 키워드가 포함되고 + 위치 질문 관련 키워드도 포함될 경우
            if (userInput.Contains(keyword))
            {
                foreach (var hint in locationHints)
                {
                    if (userInput.Contains(hint))
                    {
                       // Debug.Log($"[ObjectLocator] '{keyword}' + 위치 관련 단어 '{hint}' 발견 → 강조 실행");
                        mapping.target?.Highlight(3f);
                        return;
                    }
                }
            }
        }
    }
    public bool IsLocationQuestion(string userInput, out string matchedKeyword)
    {
        matchedKeyword = null;
        if (string.IsNullOrEmpty(userInput)) return false;

        foreach (var mapping in keywordMappings)
        {
            string keyword = mapping.keyword;

            if (userInput.Contains(keyword))
            {
                foreach (var hint in locationHints)
                {
                    if (userInput.Contains(hint))
                    {
                        matchedKeyword = keyword;
                        return true;
                    }
                }
            }
        }
        return false;
    }
    public string GetRelativeDirectionTo(string keyword)
    {
        if (string.IsNullOrEmpty(keyword) || playerHeadTransform == null)
            return "주변에 위치해 있습니다";

        // 대상 찾기
        var target = keywordMappings.Find(k => k.keyword == keyword)?.target;
        if (target == null) return "주변에 위치해 있습니다";

        Vector3 toTarget = (target.transform.position - playerHeadTransform.position).normalized;
        Vector3 forward = playerHeadTransform.forward;

        // 평면 방향 (수평 판단만)
        toTarget.y = 0;
        forward.y = 0;

        float angle = Vector3.SignedAngle(forward, toTarget, Vector3.up);

        if (angle >= -45f && angle < 45f) return "앞";
        if (angle >= 45f && angle < 135f) return "오른쪽";
        if (angle >= -135f && angle < -45f) return "왼쪽";
        return "뒤";
    }
}
