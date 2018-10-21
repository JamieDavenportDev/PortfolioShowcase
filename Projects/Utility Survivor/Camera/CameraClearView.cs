using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraClearView : MonoBehaviour {

    public GameObject m_Character;
    public Material m_ShadowMaterial;

    private List<GameObject> m_CurrentlyActive, m_IsBlocking, m_ToDeactivate;
    private int m_LayerMask;

	// Use this for initialization
	void Start () {
        m_LayerMask = LayerMask.GetMask("DoRaycast");
        m_IsBlocking      = new List<GameObject>();
        m_ToDeactivate    = new List<GameObject>();
        m_CurrentlyActive = new List<GameObject>();
    }

	void FixedUpdate () {
        
        // Setup values for raycast
        Vector3 Origin    = transform.position;
        Vector3 Direction = transform.forward;
        
        Origin -= Direction * 100.0f;
        float Distance = Vector3.Distance(Origin, m_Character.transform.position);

        // Send out raycast to gather array
        RaycastHit[] hits = Physics.RaycastAll(Origin, Direction, Distance, m_LayerMask);
        // Clear list for new objects
        m_IsBlocking.Clear();
        foreach (RaycastHit hit in hits)
        {
            // Each object hit gets added to current list
            GameObject objHit = hit.transform.gameObject;
            m_IsBlocking.Add(objHit);
        }

        // Clear the unblock list and find objects to unblock
        m_ToDeactivate.Clear();
        foreach(GameObject obj in m_CurrentlyActive)
        {
            // If it's in currently active list, but not currently IsBlocking one then it needs to be deactivated
            if(!m_IsBlocking.Contains(obj))
            {
                m_ToDeactivate.Add(obj);
            }
        }

        // Handle current list to set transparency on
        foreach(GameObject obj in m_IsBlocking)
        {
            // If our list of known active objects doesn't contain this object, we need to activate it
            if(!m_CurrentlyActive.Contains(obj))
            {
                TransparencyScript script;
                if ((script = obj.GetComponent<TransparencyScript>()) == null)
                    script = obj.AddComponent<TransparencyScript>();
                script.TransparencyStart(m_ShadowMaterial);
            }            
        }

        // Handle unblock list to set transparency off
        foreach(GameObject obj in m_ToDeactivate)
        {
            TransparencyScript script = obj.GetComponent<TransparencyScript>();
            if (script != null)
                script.TransparencyEnd();            
        }
	}

    public void AlertActive(GameObject toAdd)
    {
        if (!m_CurrentlyActive.Contains(toAdd))
            m_CurrentlyActive.Add(toAdd);
    }

    public void AlertDeactive(GameObject toDelete)
    {
        if (m_CurrentlyActive.Contains(toDelete))
            m_CurrentlyActive.Remove(toDelete);
    }
}
