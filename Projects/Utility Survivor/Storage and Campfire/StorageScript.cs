using System;
using UnityEngine;
using UnityEngine.UI;

public class StorageScript : MonoBehaviour {

    // ***********************************
    // *** Public/Serialized variables ***
    public int m_StorageLimit;            // Max units this can hold
    public Mesh[] m_Meshes = new Mesh[3]; // 0 for empty, 1 for half, 2 for full
    public Material[] m_Materials = new Material[3]; // Corresponding materials for each mesh
    public Path_Node m_NearestNode;       // Node closest to this store. used by task deployer
    public Text m_UIText;

    // *************************
    // *** Private variables ***
    private ResourceTextHandler m_TextScript;
    private int m_CurrentHeld;
    private int m_CurrentMesh = 0;

    private void Start()
    {
        m_CurrentHeld = 0;
        m_TextScript = GetComponentInChildren<ResourceTextHandler>();
        string overheadOutput = m_CurrentHeld.ToString() + "/" + m_StorageLimit.ToString();
        m_TextScript.SetValue(overheadOutput);
        UpdateUIText();
    }

    private void UpdateUIText()
    {
        string overheadOutput = m_CurrentHeld.ToString() + "/" + m_StorageLimit.ToString();
        m_TextScript.SetValue(overheadOutput);
        m_UIText.text = overheadOutput;
        m_UIText.color = Color.Lerp(Color.red, Color.green, (float)m_CurrentHeld / m_StorageLimit);
    }

    public int QueryAmount() { return m_CurrentHeld; }
    public int QuerySpace() { return m_StorageLimit - m_CurrentHeld; }
    public string GetNodeString() { return m_NearestNode.uniqueID; }

    private void CheckModel()
    {
        // Putting this here simply because it gets called every time the amount changes        
        UpdateUIText();

        // Since amount has updated, want to check if mesh needs to update:
        float percentFull = (float)m_CurrentHeld / (float)m_StorageLimit;
        int correctMesh;

        // Turn percent result into int for easy mesh change
        if (percentFull >= 0.9f)
            correctMesh = 2;
        else if (percentFull > 0.0f)
            correctMesh = 1;
        else
            correctMesh = 0;

        // If mesh needs changing, change it...
        if(correctMesh != m_CurrentMesh)
        {
            gameObject.GetComponent<MeshFilter>().mesh = m_Meshes[correctMesh];
            gameObject.GetComponent<Renderer>().material = m_Materials[correctMesh];
            m_CurrentMesh = correctMesh;
        }            
    }

    public bool AddUnit(int amount)
    {
        if (m_CurrentHeld <= m_StorageLimit - amount)
        {
            m_CurrentHeld += amount;
            CheckModel();
            return true;
        }
        else
            return false;
    }

    public void ForceAdd()
    {
        AddUnit(1);
    }

    public bool TakeUnit(int amount)
    {
        if (m_CurrentHeld >= amount)
        {
            m_CurrentHeld -= amount;
            CheckModel();
            return true;
        }
        else
            return false;
    }

    public void ForceTake()
    {
        TakeUnit(1);
    }
}
