using UnityEngine;

public class ColliderVisualizerRuntime : MonoBehaviour
{
    private Collider colliderToVisualize;
    private GameObject visualRepresentation;

    private void Start()
    {
        colliderToVisualize = GetComponent<Collider>();
        if (colliderToVisualize == null)
        {
            Debug.LogWarning("Collider가 이 오브젝트에 없습니다!");
            return;
        }

        CreateVisualRepresentation();
    }

    private void CreateVisualRepresentation()
    {
        if (colliderToVisualize is BoxCollider boxCollider)
        {
            visualRepresentation = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualRepresentation.transform.parent = transform;
            visualRepresentation.transform.localPosition = boxCollider.center;
            visualRepresentation.transform.localRotation = Quaternion.identity;
            visualRepresentation.transform.localScale = boxCollider.size;

            // 반투명 재질 설정
            Renderer renderer = visualRepresentation.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = new Color(0, 1, 0, 0.5f); // 초록색 반투명
            renderer.material.SetFloat("_Mode", 2); // Fade 모드
            renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            renderer.material.SetInt("_ZWrite", 0);
            renderer.material.EnableKeyword("_ALPHABLEND_ON");
            renderer.material.renderQueue = 3000;

            // Collider 비활성화 (시각화용 오브젝트이므로 충돌하지 않도록)
            Destroy(visualRepresentation.GetComponent<Collider>());
        }
        else if (colliderToVisualize is SphereCollider sphereCollider)
        {
            visualRepresentation = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visualRepresentation.transform.parent = transform;
            visualRepresentation.transform.localPosition = sphereCollider.center;
            visualRepresentation.transform.localRotation = Quaternion.identity;
            visualRepresentation.transform.localScale = Vector3.one * sphereCollider.radius * 2; // 반지름을 지름으로 변환

            Renderer renderer = visualRepresentation.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = new Color(0, 1, 0, 0.5f);
            renderer.material.SetFloat("_Mode", 2);
            renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            renderer.material.SetInt("_ZWrite", 0);
            renderer.material.EnableKeyword("_ALPHABLEND_ON");
            renderer.material.renderQueue = 3000;

            Destroy(visualRepresentation.GetComponent<Collider>());
        }
        // CapsuleCollider 등 다른 타입도 유사하게 처리 가능
    }

    private void Update()
    {
        // 필요 시 위치 및 크기 동기화
        if (visualRepresentation != null)
        {
            visualRepresentation.transform.localPosition = (colliderToVisualize as BoxCollider)?.center ?? Vector3.zero;
        }
    }

    private void OnDestroy()
    {
        if (visualRepresentation != null)
        {
            Destroy(visualRepresentation);
        }
    }
}