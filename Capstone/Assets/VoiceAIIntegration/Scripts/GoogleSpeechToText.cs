using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public static class GoogleSpeechToText
{
    private static readonly string googleApiKey = "AIzaSyBzzzqf6HywzXTxGjq8VFE1IpUW_KG-KNk"; // TODO: 여기에 본인 키 넣기

    public static IEnumerator SendAudio(byte[] wavData, System.Action<string> onResult)
    {
        string url = $"https://speech.googleapis.com/v1/speech:recognize?key={googleApiKey}";

        string base64Audio = System.Convert.ToBase64String(wavData);
        Debug.Log($"[GoogleSpeechToText] Base64 문자열 길이: {base64Audio.Length} characters");

        string jsonBody = $@"
    {{
        ""config"": {{
            ""encoding"": ""LINEAR16"",
            ""sampleRateHertz"": 8000,
            ""languageCode"": ""ko-KR""
        }},
        ""audio"": {{
            ""content"": ""{base64Audio}""
        }}
    }}";

        Debug.Log($"[GoogleSpeechToText] 전송할 JSON Body: {jsonBody.Substring(0, Mathf.Min(500, jsonBody.Length))}...");
        // 너무 길면 앞부분 500자만 보기

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string recognizedText = ParseGoogleSTTResponse(request.downloadHandler.text);
            onResult?.Invoke(recognizedText);
        }
        else
        {
            Debug.LogError($"Google STT Error: {request.error}\nResponse Body: {request.downloadHandler.text}");
            onResult?.Invoke(null);
        }
    }



    private static string ParseGoogleSTTResponse(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[GoogleSpeechToText] 빈 응답");
            return null;
        }

        Debug.Log($"[GoogleSpeechToText] 응답 JSON: {json}");

        if (json.Contains("\"transcript\":"))
        {
            int index = json.IndexOf("\"transcript\":") + "\"transcript\":".Length;
            int start = json.IndexOf("\"", index) + 1;
            int end = json.IndexOf("\"", start);
            string transcript = json.Substring(start, end - start);
            Debug.Log($"[GoogleSpeechToText] 인식된 텍스트: {transcript}");
            return transcript;
        }
        else
        {
            Debug.LogWarning("[GoogleSpeechToText] transcript 항목 없음");
            return null;
        }
    }


    [System.Serializable]
    public class GoogleSTTResponse
    {
        public Result[] results;
    }

    [System.Serializable]
    public class Result
    {
        public Alternative[] alternatives;
    }

    [System.Serializable]
    public class Alternative
    {
        public string transcript;
    }
}
