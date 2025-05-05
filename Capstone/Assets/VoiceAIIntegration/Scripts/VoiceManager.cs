using UnityEngine;
using UnityEngine.XR;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class VoiceManager : MonoBehaviour
{
    [Header("UI 설정")]
    [SerializeField] private GameObject previousUI; // 녹음 전 UI
    [SerializeField] private GameObject recordUI;   // 녹음 중 UI

    [Header("텍스트 UI")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI answerText;
    public bool playRecordedAudio = false;

    private AudioClip recordedClip;
    private bool isRecording = false;

    private GeminiManager geminiManager;
    private GoogleTTSManager ttsManager;

    private InputDevice rightHandDevice;
    private bool previousButtonState = false;
    private AudioSource playbackSource;

    public ObjectLocator objectLocator;

    private void Start()
    {
        geminiManager = GetComponent<GeminiManager>();
        ttsManager = GetComponent<GoogleTTSManager>();
        playbackSource = gameObject.AddComponent<AudioSource>();

        var rightHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        if (rightHandDevices.Count > 0)
        {
            rightHandDevice = rightHandDevices[0];
        }

        SetUIState(true); // 초기 상태는 previous UI
    }

    private void Update()
    {
        if (!rightHandDevice.isValid)
        {
            var rightHandDevices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
            if (rightHandDevices.Count > 0)
                rightHandDevice = rightHandDevices[0];
            return;
        }

        if (rightHandDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool isPrimaryButtonPressed))
        {
            if (isPrimaryButtonPressed && !previousButtonState)
            {
                StartRecording();
            }
            else if (!isPrimaryButtonPressed && previousButtonState)
            {
                StopRecording();
            }

            previousButtonState = isPrimaryButtonPressed;
        }
    }

    private void StartRecording()
    {
        if (isRecording) return;

        isRecording = true;
        statusText.text = "🎤 녹음 중...";
        SetUIState(false);

        recordedClip = Microphone.Start(null, false, 10, 8000);
    }

    private void StopRecording()
    {
        if (!isRecording) return;

        Microphone.End(null);
        isRecording = false;

        if (recordedClip == null || recordedClip.length < 0.2f)
        {
            statusText.text = "녹음 실패: 너무 짧음";
            SetUIState(true);
            return;
        }

        statusText.text = "녹음 종료, 처리 중...";

        if (playRecordedAudio && playbackSource != null)
        {
            playbackSource.clip = recordedClip;
            playbackSource.Play();
        }

        byte[] wavData = WavUtility.FromAudioClip(recordedClip);
        StartCoroutine(GoogleSpeechToText.SendAudio(wavData, OnSpeechRecognized));
    }

    private void OnSpeechRecognized(string recognizedText)
    {
        if (string.IsNullOrEmpty(recognizedText))
        {
            statusText.text = "음성 인식 실패";
            SetUIState(true);
            return;
        }

        statusText.text = $"질문 : {recognizedText}";
        objectLocator?.CheckAndHighlight(recognizedText);

        if (objectLocator.IsLocationQuestion(recognizedText, out string matchedKeyword))
        {
            string direction = objectLocator.GetRelativeDirectionTo(matchedKeyword);
            string fixedAnswer = $"{matchedKeyword}은 당신 기준 {direction} 방향에 있습니다!";

            answerText.text = $"AI :\n<color=#007BFF>{fixedAnswer}</color>";
            StartCoroutine(FinalizeAfterSpeaking(fixedAnswer));
            return;
        }

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

이 시나리오에 기반해 답변을 해주세요. 그리고 모든 답변은 50자 이내로 진행해줘.
자기 소개 및 기능 소개는 할 필요 없어. 바로 사용자의 질문에 답만 해줘";

        string finalPrompt = $"{preContext}\n\n질문: {recognizedText}";
        StartCoroutine(geminiManager.SendPrompt(finalPrompt, OnAnswerReceived));
    }

    private void OnAnswerReceived(string answer)
    {
        if (string.IsNullOrEmpty(answer))
        {
            statusText.text = "답변 생성 실패";
            SetUIState(true);
            return;
        }

        answerText.text = $"AI:\n<color=#007BFF>{answer}</color>";
        StartCoroutine(FinalizeAfterSpeaking(answer));
    }

    private IEnumerator FinalizeAfterSpeaking(string textToSpeak)
    {
        yield return ttsManager.Speak(textToSpeak);
        yield return new WaitForSeconds(7f); // 5초 대기 후 UI 전환
        SetUIState(true);
    }

    private void SetUIState(bool showPrevious)
    {
        if (previousUI != null)
            previousUI.SetActive(showPrevious);

        if (recordUI != null)
            recordUI.SetActive(!showPrevious);
    }
}