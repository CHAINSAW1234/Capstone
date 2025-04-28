using UnityEngine;
using UnityEngine.XR;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class VoiceManager : MonoBehaviour
{
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI answerText;
    public bool playRecordedAudio = false; // Inspector에서 설정 가능

    private AudioClip recordedClip;
    private bool isRecording = false;

    private GeminiManager geminiManager;
    private GoogleTTSManager ttsManager;

    private InputDevice rightHandDevice;
    private bool previousButtonState = false; // 이전 버튼 상태 저장
    private AudioSource playbackSource;

    private void Start()
    {
        geminiManager = GetComponent<GeminiManager>();
        ttsManager = GetComponent<GoogleTTSManager>();

        playbackSource = gameObject.AddComponent<AudioSource>(); // 녹음 재생용 AudioSource 추가

        // 오른손 컨트롤러 디바이스 찾기
        var rightHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        if (rightHandDevices.Count > 0)
        {
            rightHandDevice = rightHandDevices[0];
            Debug.Log("[VoiceManager] 오른손 컨트롤러 연결 완료");
        }
        else
        {
            Debug.LogWarning("[VoiceManager] 오른손 컨트롤러를 찾을 수 없습니다.");
        }
    }

    private void Update()
    {
        if (!rightHandDevice.isValid)
        {
            // 연결 끊김 대비 재탐색
            var rightHandDevices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
            if (rightHandDevices.Count > 0)
                rightHandDevice = rightHandDevices[0];
            return;
        }

        bool isPrimaryButtonPressed = false;
        if (rightHandDevice.TryGetFeatureValue(CommonUsages.primaryButton, out isPrimaryButtonPressed))
        {
            if (isPrimaryButtonPressed && !previousButtonState)
            {
                Debug.Log("[VoiceManager] 🎤 녹음 시작 (버튼 누름)");
                StartRecording();
            }
            else if (!isPrimaryButtonPressed && previousButtonState)
            {
                Debug.Log("[VoiceManager] 🛑 녹음 종료 (버튼 뗌)");
                StopRecording();
            }

            previousButtonState = isPrimaryButtonPressed; // 상태 업데이트
        }
    }

    private void StartRecording()
    {
        if (isRecording) return;

        statusText.text = "🎤 녹음 중...";
        recordedClip = Microphone.Start(null, false, 10, 8000); // 16000Hz, 10초 녹음
        isRecording = true;
    }

    private void StopRecording()
    {
        if (!isRecording) return;

        Microphone.End(null);
        isRecording = false;

        if (recordedClip == null || recordedClip.length < 0.2f)
        {
            statusText.text = "녹음 실패: 너무 짧음";
            Debug.LogWarning("[VoiceManager] 녹음된 데이터가 너무 짧거나 없습니다.");
            return;
        }

        statusText.text = "녹음 종료, 처리 중...";

        if (playRecordedAudio && playbackSource != null)
        {
            playbackSource.clip = recordedClip;
            playbackSource.Play();
            Debug.Log("[VoiceManager] 녹음된 소리 재생 중...");
        }

        byte[] wavData = WavUtility.FromAudioClip(recordedClip);
        StartCoroutine(GoogleSpeechToText.SendAudio(wavData, OnSpeechRecognized));
    }

    private void OnSpeechRecognized(string recognizedText)
    {
        if (string.IsNullOrEmpty(recognizedText))
        {
            statusText.text = "음성 인식 실패";
            Debug.LogWarning("[VoiceManager] 음성 인식 실패 (텍스트 없음)");
            return;
        }

        // 🔵 1. 유저가 질문한 원본 텍스트는 그대로 표시
        statusText.text = $"🎤 인식된 텍스트: {recognizedText}";
        Debug.Log($"[VoiceManager] 인식된 텍스트: {recognizedText}");

        // 🔵 2. Gemini에 보낼 때는 제약사항 추가
        string preContext = @"당신은 가상현실 화재 대피 교육 훈련 시스템에 AI 어시스턴트입니다.
현재 진행 중인 시나리오 순서는 다음과 같습니다:

1. Room
- 불 발생 인지
- 헝겊으로 호흡 보호
- 문 온도 확인 후 탈출

2. Hall
- 대피도 확인
- 경보 울리기
- 포복 이동
- 비상문 사용

3. Safe Place
- 게임 종료 및 결과 보고

4. 소화기 사용
- 절차 및 불 끄기

이 시나리오에 기반해 답변을 해주세요. 그리고 모든 답변은 40자 이내로 진행해줘.
자기 소개 및 기능 소개는 할 필요 없어. 바로 사용자의 질문에 답만 해줘";

        string finalPrompt = $"{preContext}\n\n질문: {recognizedText}";

        StartCoroutine(geminiManager.SendPrompt(finalPrompt, OnAnswerReceived));
    }

    private void OnAnswerReceived(string answer)
    {
        if (string.IsNullOrEmpty(answer))
        {
            statusText.text = "답변 생성 실패";
            Debug.LogWarning("[VoiceManager] 답변 생성 실패");
            return;
        }

        answerText.text = $"🧠 AI 답변:\n{answer}";
        Debug.Log($"[VoiceManager] AI 답변 수신 완료: {answer}");

        StartCoroutine(ttsManager.Speak(answer));
    }
}
