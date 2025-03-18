using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HapticFeedback : MonoBehaviour
{
    [Header("Haptic Settings")]
    [SerializeField]
    private bool enableHaptics = true; // ���� �ǵ�� On/Off
    [SerializeField]
    private XRBaseController controller; // ������ ���� ��Ʈ�ѷ�
    [SerializeField]
    private float hapticIntensity = 0.5f; // ���� ���� (0~1)
    [SerializeField]
    private float hapticDuration = 0.2f; // ���� ���� �ð� (��)

    // ���������� ȣ�� ������ �޼���
    public void TriggerHaptic()
    {
        if (enableHaptics && controller != null)
        {
            controller.SendHapticImpulse(hapticIntensity, hapticDuration);
        }
    }

    // ������ ���� �ð��� �������� ������ ȣ�� ������ �޼���
    public void TriggerHapticWithValues(float intensity, float duration)
    {
        if (enableHaptics && controller != null)
        {
            controller.SendHapticImpulse(intensity, duration);
        }
    }

    // Inspector���� Ȯ�ο�
    private void OnValidate()
    {
        if (enableHaptics && controller == null)
        {
            Debug.LogWarning("Haptic Feedback is enabled but Controller is not assigned!");
        }
        hapticIntensity = Mathf.Clamp01(hapticIntensity); // ������ 0~1 ���̷� ����
        hapticDuration = Mathf.Max(0.01f, hapticDuration); // ���� �ð��� �ּ� 0.01��
    }
}