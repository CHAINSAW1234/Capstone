using UnityEngine;

public class ArrowFeedback : MonoBehaviour
{
    [Header("Target Objects")]
    [SerializeField]
    private Transform targetObjectA; // 화살표 시작점 (기본값: 스크립트가 추가된 오브젝트)
    [SerializeField]
    private Transform targetObjectB; // 화살표 끝점

    [Header("Arrow Settings")]
    [SerializeField]
    private Color arrowColor = Color.blue; // 화살표 색상
    [SerializeField]
    private float arrowWidth = 0.05f; // 화살표 선 두께
    [SerializeField]
    private float arrowHeadSize = 0.2f; // 화살촉 크기 (길이)
    [SerializeField]
    private float arrowHeadWidth = 0.1f; // 화살촉 너비
    [SerializeField]
    private Material arrowMaterial; // 화살표에 사용할 Material (없으면 기본 Material 생성)

    [Header("Distance Settings")]
    [SerializeField]
    [Tooltip("두 물체가 이 거리 이내에 있으면 화살표가 사라짐 (단위: meter)")]
    private float disappearDistance = 1.0f; // 화살표가 사라지는 거리 임계값 (기본값: 1미터)

    private LineRenderer lineRenderer; // 화살표 몸체를 위한 LineRenderer
    private GameObject arrowHead; // 화살촉을 위한 오브젝트
    private MeshFilter arrowHeadMeshFilter; // 화살촉 Mesh
    private MeshRenderer arrowHeadMeshRenderer; // 화살촉 렌더러

    void Awake()
    {
        // targetObjectA가 설정되지 않았으면 현재 오브젝트로 설정
        if (targetObjectA == null)
        {
            targetObjectA = transform;
            Debug.Log("Target Object A set to: " + gameObject.name);
        }

        // LineRenderer 설정
        SetupLineRenderer();

        // 화살촉 오브젝트 생성
        CreateArrowHead();
    }

    void Update()
    {
        // targetObjectA와 targetObjectB가 모두 설정되어 있을 때만 화살표 업데이트
        if (targetObjectA == null || targetObjectB == null)
        {
            Debug.LogWarning("Target Object A or B is not assigned! ArrowFeedback will not work.");
            lineRenderer.enabled = false;
            arrowHead.SetActive(false);
            return;
        }

        // 두 물체 간 거리 계산
        float distance = Vector3.Distance(targetObjectA.position, targetObjectB.position);

        // 거리 체크: 임계값 이내일 경우 화살표 비활성화
        if (distance <= disappearDistance)
        {
            lineRenderer.enabled = false;
            arrowHead.SetActive(false);
            Debug.Log("Objects are within disappear distance (" + disappearDistance + "m). Arrow hidden.");
        }
        else
        {
            lineRenderer.enabled = true;
            arrowHead.SetActive(true);
            UpdateArrowBody();
            UpdateArrowHead();
            Debug.Log("Distance between objects: " + distance + "m. Arrow visible.");
        }
    }

    void OnDestroy()
    {
        // 화살촉 오브젝트 제거
        if (arrowHead != null)
        {
            Destroy(arrowHead);
        }
    }

    // LineRenderer 초기화
    private void SetupLineRenderer()
    {
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // LineRenderer 설정
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = arrowWidth;
        lineRenderer.endWidth = arrowWidth;
        lineRenderer.startColor = arrowColor;
        lineRenderer.endColor = arrowColor;

        // Material 설정
        if (arrowMaterial == null)
        {
            arrowMaterial = new Material(Shader.Find("Sprites/Default")); // 기본 Material
        }
        lineRenderer.material = arrowMaterial;
    }

    // 화살촉 오브젝트 생성
    private void CreateArrowHead()
    {
        arrowHead = new GameObject("ArrowHead");
        arrowHead.transform.SetParent(transform, false);

        // MeshFilter와 MeshRenderer 추가
        arrowHeadMeshFilter = arrowHead.AddComponent<MeshFilter>();
        arrowHeadMeshRenderer = arrowHead.AddComponent<MeshRenderer>();

        // 화살촉 Mesh 생성
        UpdateArrowHeadMesh();

        // Material 설정
        if (arrowMaterial == null)
        {
            arrowMaterial = new Material(Shader.Find("Sprites/Default"));
        }
        arrowHeadMeshRenderer.material = arrowMaterial;
        arrowHeadMeshRenderer.material.color = arrowColor;
    }

    // 화살촉 Mesh 생성 및 업데이트
    private void UpdateArrowHeadMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[3];
        int[] triangles = new int[3];

        // 화살촉 삼각형 정의
        vertices[0] = Vector3.zero; // 화살촉 끝
        vertices[1] = new Vector3(-arrowHeadSize, arrowHeadWidth, 0); // 왼쪽 아래
        vertices[2] = new Vector3(-arrowHeadSize, -arrowHeadWidth, 0); // 오른쪽 아래

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        arrowHeadMeshFilter.mesh = mesh;
    }

    // 화살표 몸체 업데이트
    private void UpdateArrowBody()
    {
        // 시작점과 끝점 설정
        Vector3 startPos = targetObjectA.position;
        Vector3 endPos = targetObjectB.position;

        // 화살촉 크기만큼 끝점을 조정 (화살촉과 겹치지 않도록)
        Vector3 direction = (endPos - startPos).normalized;
        endPos -= direction * arrowHeadSize;

        // LineRenderer 위치 설정
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

        // 색상 및 두께 업데이트
        lineRenderer.startWidth = arrowWidth;
        lineRenderer.endWidth = arrowWidth;
        lineRenderer.startColor = arrowColor;
        lineRenderer.endColor = arrowColor;
        lineRenderer.material.color = arrowColor;
    }

    // 화살촉 위치 및 방향 업데이트
    private void UpdateArrowHead()
    {
        // 화살촉 위치 설정
        Vector3 startPos = targetObjectA.position;
        Vector3 endPos = targetObjectB.position;
        Vector3 direction = (endPos - startPos).normalized;
        arrowHead.transform.position = endPos;

        // 화살촉 방향 설정
        if (direction != Vector3.zero)
        {
            arrowHead.transform.rotation = Quaternion.LookRotation(direction);
        }

        // 화살촉 색상 및 Mesh 업데이트
        UpdateArrowHeadMesh();
        arrowHeadMeshRenderer.material.color = arrowColor;
    }

    // Inspector에서 확인용
    private void OnValidate()
    {
        if (targetObjectA == null)
        {
            targetObjectA = transform;
        }

        if (targetObjectB == null)
        {
            Debug.LogWarning("Target Object B is not assigned!");
        }

        // 설정값 검증
        arrowWidth = Mathf.Max(0.01f, arrowWidth);
        arrowHeadSize = Mathf.Max(0.01f, arrowHeadSize);
        arrowHeadWidth = Mathf.Max(0.01f, arrowHeadWidth);
        disappearDistance = Mathf.Max(0.01f, disappearDistance);
    }
}