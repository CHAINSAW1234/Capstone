using UnityEngine;

public class ArrowFeedback : MonoBehaviour
{
    [Header("Target Objects")]
    [SerializeField]
    private Transform targetObjectA; // ȭ��ǥ ������ (�⺻��: ��ũ��Ʈ�� �߰��� ������Ʈ)
    [SerializeField]
    private Transform targetObjectB; // ȭ��ǥ ����

    [Header("Arrow Settings")]
    [SerializeField]
    private Color arrowColor = Color.blue; // ȭ��ǥ ����
    [SerializeField]
    private float arrowWidth = 0.05f; // ȭ��ǥ �� �β�
    [SerializeField]
    private float arrowHeadSize = 0.2f; // ȭ���� ũ�� (����)
    [SerializeField]
    private float arrowHeadWidth = 0.1f; // ȭ���� �ʺ�
    [SerializeField]
    private Material arrowMaterial; // ȭ��ǥ�� ����� Material (������ �⺻ Material ����)

    [Header("Distance Settings")]
    [SerializeField]
    [Tooltip("�� ��ü�� �� �Ÿ� �̳��� ������ ȭ��ǥ�� ����� (����: meter)")]
    private float disappearDistance = 1.0f; // ȭ��ǥ�� ������� �Ÿ� �Ӱ谪 (�⺻��: 1����)

    private LineRenderer lineRenderer; // ȭ��ǥ ��ü�� ���� LineRenderer
    private GameObject arrowHead; // ȭ������ ���� ������Ʈ
    private MeshFilter arrowHeadMeshFilter; // ȭ���� Mesh
    private MeshRenderer arrowHeadMeshRenderer; // ȭ���� ������

    void Awake()
    {
        // targetObjectA�� �������� �ʾ����� ���� ������Ʈ�� ����
        if (targetObjectA == null)
        {
            targetObjectA = transform;
            Debug.Log("Target Object A set to: " + gameObject.name);
        }

        // LineRenderer ����
        SetupLineRenderer();

        // ȭ���� ������Ʈ ����
        CreateArrowHead();
    }

    void Update()
    {
        // targetObjectA�� targetObjectB�� ��� �����Ǿ� ���� ���� ȭ��ǥ ������Ʈ
        if (targetObjectA == null || targetObjectB == null)
        {
            Debug.LogWarning("Target Object A or B is not assigned! ArrowFeedback will not work.");
            lineRenderer.enabled = false;
            arrowHead.SetActive(false);
            return;
        }

        // �� ��ü �� �Ÿ� ���
        float distance = Vector3.Distance(targetObjectA.position, targetObjectB.position);

        // �Ÿ� üũ: �Ӱ谪 �̳��� ��� ȭ��ǥ ��Ȱ��ȭ
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
        // ȭ���� ������Ʈ ����
        if (arrowHead != null)
        {
            Destroy(arrowHead);
        }
    }

    // LineRenderer �ʱ�ȭ
    private void SetupLineRenderer()
    {
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // LineRenderer ����
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = arrowWidth;
        lineRenderer.endWidth = arrowWidth;
        lineRenderer.startColor = arrowColor;
        lineRenderer.endColor = arrowColor;

        // Material ����
        if (arrowMaterial == null)
        {
            arrowMaterial = new Material(Shader.Find("Sprites/Default")); // �⺻ Material
        }
        lineRenderer.material = arrowMaterial;
    }

    // ȭ���� ������Ʈ ����
    private void CreateArrowHead()
    {
        arrowHead = new GameObject("ArrowHead");
        arrowHead.transform.SetParent(transform, false);

        // MeshFilter�� MeshRenderer �߰�
        arrowHeadMeshFilter = arrowHead.AddComponent<MeshFilter>();
        arrowHeadMeshRenderer = arrowHead.AddComponent<MeshRenderer>();

        // ȭ���� Mesh ����
        UpdateArrowHeadMesh();

        // Material ����
        if (arrowMaterial == null)
        {
            arrowMaterial = new Material(Shader.Find("Sprites/Default"));
        }
        arrowHeadMeshRenderer.material = arrowMaterial;
        arrowHeadMeshRenderer.material.color = arrowColor;
    }

    // ȭ���� Mesh ���� �� ������Ʈ
    private void UpdateArrowHeadMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[3];
        int[] triangles = new int[3];

        // ȭ���� �ﰢ�� ����
        vertices[0] = Vector3.zero; // ȭ���� ��
        vertices[1] = new Vector3(-arrowHeadSize, arrowHeadWidth, 0); // ���� �Ʒ�
        vertices[2] = new Vector3(-arrowHeadSize, -arrowHeadWidth, 0); // ������ �Ʒ�

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        arrowHeadMeshFilter.mesh = mesh;
    }

    // ȭ��ǥ ��ü ������Ʈ
    private void UpdateArrowBody()
    {
        // �������� ���� ����
        Vector3 startPos = targetObjectA.position;
        Vector3 endPos = targetObjectB.position;

        // ȭ���� ũ�⸸ŭ ������ ���� (ȭ���˰� ��ġ�� �ʵ���)
        Vector3 direction = (endPos - startPos).normalized;
        endPos -= direction * arrowHeadSize;

        // LineRenderer ��ġ ����
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

        // ���� �� �β� ������Ʈ
        lineRenderer.startWidth = arrowWidth;
        lineRenderer.endWidth = arrowWidth;
        lineRenderer.startColor = arrowColor;
        lineRenderer.endColor = arrowColor;
        lineRenderer.material.color = arrowColor;
    }

    // ȭ���� ��ġ �� ���� ������Ʈ
    private void UpdateArrowHead()
    {
        // ȭ���� ��ġ ����
        Vector3 startPos = targetObjectA.position;
        Vector3 endPos = targetObjectB.position;
        Vector3 direction = (endPos - startPos).normalized;
        arrowHead.transform.position = endPos;

        // ȭ���� ���� ����
        if (direction != Vector3.zero)
        {
            arrowHead.transform.rotation = Quaternion.LookRotation(direction);
        }

        // ȭ���� ���� �� Mesh ������Ʈ
        UpdateArrowHeadMesh();
        arrowHeadMeshRenderer.material.color = arrowColor;
    }

    // Inspector���� Ȯ�ο�
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

        // ������ ����
        arrowWidth = Mathf.Max(0.01f, arrowWidth);
        arrowHeadSize = Mathf.Max(0.01f, arrowHeadSize);
        arrowHeadWidth = Mathf.Max(0.01f, arrowHeadWidth);
        disappearDistance = Mathf.Max(0.01f, disappearDistance);
    }
}