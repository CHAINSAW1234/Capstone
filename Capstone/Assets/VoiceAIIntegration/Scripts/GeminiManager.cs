using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class GeminiManager : MonoBehaviour
{
    public string geminiApiKey;

    public IEnumerator SendPrompt(string question, System.Action<string> onResponse)
    {
        int retryCount = 0;
        const int maxRetries = 3;

        while (retryCount <= maxRetries)
        {
            using (UnityWebRequest request = BuildRequest(question))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string answer = ParseGeminiResponse(request.downloadHandler.text);
                    onResponse?.Invoke(answer);
                    yield break;
                }
                else if (request.responseCode == 429) // Too Many Requests
                {
                    Debug.LogWarning("Gemini API Rate Limit, 재시도 중...");
                    retryCount++;
                    yield return new WaitForSeconds(2 * retryCount);
                }
                else
                {
                    Debug.LogError($"Gemini API Error: {request.error}\n{request.downloadHandler.text}");
                    yield break;
                }
            }
        }

        Debug.LogError("Gemini API 재시도 실패");
    }

    private UnityWebRequest BuildRequest(string question)
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-pro:generateContent?key={geminiApiKey}";

        // ✅ StringBuilder로 안전하게 JSON 생성
        StringBuilder sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"contents\": [");
        sb.Append("{");
        sb.Append("\"role\": \"user\",");
        sb.Append("\"parts\": [");
        sb.Append("{");
        sb.Append("\"text\": \"" + EscapeJson(question) + "\"");
        sb.Append("}");
        sb.Append("]");
        sb.Append("}");
        sb.Append("]");
        sb.Append("}");

        string jsonBody = sb.ToString();

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        return request;
    }

    private string ParseGeminiResponse(string json)
    {
        // 간단 파싱: "text" 필드 추출
        if (json.Contains("\"text\":"))
        {
            int index = json.IndexOf("\"text\":") + "\"text\":".Length;
            int start = json.IndexOf("\"", index) + 1;
            int end = json.IndexOf("\"", start);
            string text = json.Substring(start, end - start);
            Debug.Log($"[GeminiManager] AI 답변 수신: {text}");
            return text;
        }
        else
        {
            Debug.LogWarning("[GeminiManager] AI 답변 없음");
            return "답변을 가져올 수 없습니다.";
        }
    }

    private string EscapeJson(string str)
    {
        return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}
