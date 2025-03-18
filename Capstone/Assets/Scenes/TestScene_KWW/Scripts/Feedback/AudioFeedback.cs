using UnityEngine;

public class AudioFeedback : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField]
    private bool enableAudio = true; // ����� �ǵ�� On/Off
    [SerializeField]
    private AudioClip defaultClip; // �⺻���� ����� ����� Ŭ��
    [SerializeField]
    [Range(0f, 1f)]
    private float volumeScale = 1f; // ���� ���� (0~1)

    private AudioSource audioSource; // ���������� ������ AudioSource

    void Awake()
    {
        // AudioSource ������Ʈ�� ������ �ڵ� �߰�
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; // �ڵ� ��� ��Ȱ��ȭ
            audioSource.loop = false; // �ݺ� ��� ��Ȱ��ȭ
        }
    }

    // ���� ����� ��� �޼���
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

    // Ư�� AudioClip�� ����ϵ��� �����ε�� �޼���
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

    // Inspector���� Ȯ�ο�
    private void OnValidate()
    {
        if (enableAudio && defaultClip == null)
        {
            Debug.LogWarning("Default Audio Clip is not assigned!");
        }
        volumeScale = Mathf.Clamp01(volumeScale);
    }
}