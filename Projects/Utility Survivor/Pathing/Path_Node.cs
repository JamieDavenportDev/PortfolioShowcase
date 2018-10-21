using UnityEngine;

public class Path_Node : MonoBehaviour {

    public string uniqueID;
    public Path_Node ParentNode;
    public Path_Node[] FirstGen;

    private int m_Rank;
    private GameObject m_Marker;
    private Renderer m_MarkerRend;
    private bool m_IsRendered;
    private readonly float m_Speed = 5.0f;
    private Vector3 m_MarkerOrigin;
    private Material m_Material;
    private Color m_Colour;
    private readonly Color[] m_Colours =
    {
        new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, 1.0f),
        new Color(Color.white.r, Color.white.g, Color.white.b, 1.0f),
    };

    private void Awake()
    {
        if (ParentNode == null && uniqueID != "CenterCamp")
            ParentNode = transform.parent.gameObject.GetComponent<Path_Node>();
    }

    private void Start()
    {
        Path_Node temp = this;
        m_Rank = 0;
        while (temp.ParentNode != null)
        {
            m_Rank++;
            temp = temp.ParentNode;
        }

        m_Material = Resources.Load<Material>("Materials/PathingVisualiserMat");
        m_Marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        m_Marker.transform.parent = transform;
        m_MarkerOrigin = new Vector3(transform.position.x, transform.position.y + 5.0f, transform.position.z);
        m_Marker.transform.position = m_MarkerOrigin;
        m_Marker.transform.localScale = new Vector3(5.0f, 5.0f, 5.0f);
        m_MarkerRend = m_Marker.GetComponent<Renderer>();
        m_MarkerRend.enabled = m_IsRendered = false;
        m_MarkerRend.material = m_Material;
        float ratio = Mathf.Min(m_Rank / 5.0f, 1.0f);
        m_Colour = Color.Lerp(m_Colours[0], m_Colours[1], ratio);
        m_MarkerRend.material.color = m_Colour;
        m_MarkerRend.material.EnableKeyword("_EMISSION");
        m_MarkerRend.material.SetColor("_EmissionColor", m_Colour * (1.3f - ratio));
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.P) && GlobalTracker.GetStartSequence())
        {
            m_IsRendered = !m_IsRendered;
            m_MarkerRend.enabled = m_IsRendered;
        }
        float offsetY = Mathf.Sin(Time.time * m_Speed);
        m_Marker.transform.position = m_MarkerOrigin + (Vector3.up * offsetY);
        m_Marker.transform.Rotate(Vector3.up, 100.0f * Time.deltaTime);
    }
}
