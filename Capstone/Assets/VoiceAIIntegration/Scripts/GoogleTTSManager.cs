using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class GoogleTTSManager : MonoBehaviour
{
    public string ttsApiKey;
    public Transform headsetTransform; // ✅ VR 헤드셋(카메라) Transform을 Inspector에서 연결

    public IEnumerator Speak(string text)
    {
        string url = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={ttsApiKey}";

        string safeText = SanitizeText(text);

        var requestBody = new TTSRequest
        {
            input = new Input { text = safeText },
            voice = new Voice(),
            audioConfig = new AudioConfig()
        };

        string jsonBody = JsonUtility.ToJson(requestBody);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string base64Audio = ParseTTSResponse(request.downloadHandler.text);

            if (string.IsNullOrEmpty(base64Audio))
            {
                Debug.LogError("[TTS] base64Audio가 비어있습니다.");
                yield break;
            }

            PlayAudio(base64Audio);
        }
        else
        {
            Debug.LogError($"TTS Error: {request.error}\nResponse Body: {request.downloadHandler.text}");
        }
    }

    private void PlayAudio(string base64)
    {
        byte[] audioBytes = System.Convert.FromBase64String(base64);
        Debug.Log($"[TTS] 디코딩된 오디오 크기: {audioBytes.Length} bytes");

        WAV wav = new WAV(audioBytes);

        if (wav.SampleCount == 0)
        {
            Debug.LogError("[TTS] WAV 변환 실패 (샘플 수 0)");
            return;
        }

        AudioClip audioClip = AudioClip.Create("TTS_Audio", wav.SampleCount, 1, wav.Frequency, false);
        audioClip.SetData(wav.LeftChannel, 0);

        GameObject audioObj = new GameObject("TTS_AudioPlayer");

        if (headsetTransform != null)
        {
            audioObj.transform.SetParent(headsetTransform);
            audioObj.transform.localPosition = Vector3.zero;
        }
        else
        {
            Debug.LogWarning("[TTS] 헤드셋 Transform이 설정되지 않았습니다. 월드에 생성합니다.");
        }

        AudioSource audioSource = audioObj.AddComponent<AudioSource>();
        audioSource.clip = audioClip;
        audioSource.volume = 2.0f;  // 🔥 볼륨 업
        audioSource.spatialBlend = 0.0f; // 🔵 2D로 (헤드셋 양쪽 스피커 균등)
        audioSource.Play();

        Destroy(audioObj, audioClip.length);
        Debug.Log("[TTS] VR 헤드셋 기준으로 오디오 재생 완료");
    }

    private string SanitizeText(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        return input.Replace("\n", " ").Replace("*", "").Replace("_", "").Replace("#", "");
    }


    [System.Serializable]
    public class TTSRequest
    {
        public Input input;
        public Voice voice;
        public AudioConfig audioConfig;
    }

    [System.Serializable]
    public class Input
    {
        public string text;
    }

    [System.Serializable]
    public class Voice
    {
        public string languageCode = "ko-KR";
        public string ssmlGender = "NEUTRAL";
    }

    [System.Serializable]
    public class AudioConfig
    {
        public string audioEncoding = "LINEAR16";
        public float volumeGainDb = 0.0f; // 추가로 볼륨 서버단 세팅도 가능
    }

    private string ParseTTSResponse(string json)
    {
        var response = JsonUtility.FromJson<TTSResponse>(json);
        return response.audioContent;
    }

    [System.Serializable]
    public class TTSResponse
    {
        public string audioContent;
    }
}
