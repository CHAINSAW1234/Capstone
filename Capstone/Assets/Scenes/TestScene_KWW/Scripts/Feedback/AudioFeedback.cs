using UnityEngine;

public class AudioFeedback : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField]
    private bool enableAudio = true; // 오디오 피드백 On/Off
    [SerializeField]
    private AudioClip defaultClip; // 기본으로 재생할 오디오 클립
    [SerializeField]
    [Range(0f, 1f)]
    private float volumeScale = 1f; // 볼륨 조절 (0~1)

    private AudioSource audioSource; // 내부적으로 관리할 AudioSource

    void Awake()
    {
        // AudioSource 컴포넌트가 없으면 자동 추가
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; // 자동 재생 비활성화
            audioSource.loop = false; // 반복 재생 비활성화
        }
    }

    // 범용 오디오 재생 메서드
    public void PlayAudio()
    {
        if (enableAudio && audioSource != null && defaultClip != null)
        {
            audioSource.PlayOneShot(defaultClip, volumeScale);
        }
        else if (enableAudio && defaultClip == null)
        {
            Debug.LogWarning("No default Audio Clip assigned!");
        }
    }

    // 특정 AudioClip을 재생하도록 오버로드된 메서드
    public void PlayAudioWithClip(AudioClip clip)
    {
        if (enableAudio && audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volumeScale);
        }
        else if (enableAudio && clip == null)
        {
            Debug.LogWarning("Provided Audio Clip is null!");
        }
    }

    // Inspector에서 확인용
    private void OnValidate()
    {
        if (enableAudio && defaultClip == null)
        {
            Debug.LogWarning("Default Audio Clip is not assigned!");
        }
        volumeScale = Mathf.Clamp01(volumeScale);
    }
}