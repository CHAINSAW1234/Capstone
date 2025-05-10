using UnityEngine;
using UnityEngine.Events;

public class SceneChangeButton : MonoBehaviour
{
    [Header("이벤트 설정")]
    public UnityEvent onHandEnter;

    private void OnTriggerEnter(Collider other)
    {
        // Hand 태그가 있는 오브젝트가 트리거에 들어올 때
        if (other.CompareTag("Hand"))
        {
            Debug.Log("Hand 객체가 트리거에 진입했습니다.");
            onHandEnter?.Invoke();
        }
    }

}
