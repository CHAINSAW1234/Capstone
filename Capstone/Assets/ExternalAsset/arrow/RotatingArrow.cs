using UnityEngine;

public class RotatingArrow : MonoBehaviour
{
    [Header("회전 설정")]
    public float rotationSpeed = 20f; // 회전 속도 (초당 도 단위)

    [Header("위아래 움직임 설정")]
    public float moveAmplitude = 0.5f; // 위아래 움직임의 진폭
    public float moveSpeed = 1f; // 위아래 움직임 속도

    [Header("빛나는 효과 설정")]
    public float glowSpeed = 2f; // 깜빡임 속도
    public Color glowColor = Color.yellow; // 빛나는 색상
    public float glowIntensity = 2f; // 빛나는 강도 (기본 2배)
    private Color originalColor;
    private Renderer arrowRenderer;

    private float timeOffset;
    private float initialY;

    void Start()
    {
        // Renderer 컴포넌트 가져오기
        arrowRenderer = GetComponent<Renderer>();
        if (arrowRenderer == null)
        {
            Debug.LogError("RotatingArrow 스크립트가 붙은 오브젝트에 Renderer 컴포넌트가 없습니다!");
            return;
        }

        // 원래 색상 저장
        originalColor = arrowRenderer.material.color;
        // 시간 오프셋을 랜덤으로 설정하여 동기화되지 않도록 함
        timeOffset = Random.Range(0f, 2f * Mathf.PI);

        initialY = transform.localPosition.y;  // 초기 Y 위치 저장
    }

    void Update()
    {
        // 회전 효과
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);

        // 위아래 움직임 (Sine 함수 사용)
        float newY = initialY + moveAmplitude * Mathf.Sin(Time.time * moveSpeed + timeOffset);
        transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);

        // 빛나는 효과 (더 강한 깜빡임)
        float glow = 0.5f + 0.5f * Mathf.Sin(Time.time * glowSpeed + timeOffset);
        Color newColor = Color.Lerp(originalColor, glowColor * glowIntensity, glow);
        arrowRenderer.material.color = newColor;
    }
}
