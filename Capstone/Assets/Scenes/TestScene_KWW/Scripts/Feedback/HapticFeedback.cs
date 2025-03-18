using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HapticFeedback : MonoBehaviour
{
    [Header("Haptic Settings")]
    [SerializeField]
    private bool enableHaptics = true; // 진동 피드백 On/Off
    [SerializeField]
    private XRBaseController controller; // 진동을 보낼 컨트롤러
    [SerializeField]
    private float hapticIntensity = 0.5f; // 진동 세기 (0~1)
    [SerializeField]
    private float hapticDuration = 0.2f; // 진동 지속 시간 (초)

    // 범용적으로 호출 가능한 메서드
    public void TriggerHaptic()
    {
        if (enableHaptics && controller != null)
        {
            controller.SendHapticImpulse(hapticIntensity, hapticDuration);
        }
    }

    // 강도와 지속 시간을 동적으로 설정해 호출 가능한 메서드
    public void TriggerHapticWithValues(float intensity, float duration)
    {
        if (enableHaptics && controller != null)
        {
            controller.SendHapticImpulse(intensity, duration);
        }
    }

    // Inspector에서 확인용
    private void OnValidate()
    {
        if (enableHaptics && controller == null)
        {
            Debug.LogWarning("Haptic Feedback is enabled but Controller is not assigned!");
        }
        hapticIntensity = Mathf.Clamp01(hapticIntensity); // 강도는 0~1 사이로 제한
        hapticDuration = Mathf.Max(0.01f, hapticDuration); // 지속 시간은 최소 0.01초
    }
}