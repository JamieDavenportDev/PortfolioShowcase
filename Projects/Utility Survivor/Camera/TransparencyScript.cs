using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransparencyScript : MonoBehaviour {

    private Material m_OriginalMaterial, m_NewMaterial;
    private bool m_Started = false, m_Stopped = false;
    private Renderer m_Renderer;
    private Color m_OriginalColour, m_CurrentColour;
    private CameraClearView m_Camera;

    private void IncreaseTransparency()
    {
        m_CurrentColour = m_Renderer.material.color;
        if (m_CurrentColour.a > 0.2f)
        {
            float newAlpha = Mathf.Max(m_CurrentColour.a - (5.0f * Time.deltaTime), 0.2f);
            m_CurrentColour.a = newAlpha;
            m_Renderer.material.color = m_CurrentColour;
        }
    }

    private void DecreaseTransparency()
    {
        m_CurrentColour = m_Renderer.material.color;
        if (m_CurrentColour.a < 1.0f)
        {
            float newAlpha = m_CurrentColour.a + (5.0f * Time.deltaTime);
            m_CurrentColour.a = newAlpha;
            m_Renderer.material.color = m_CurrentColour;
        }
        if (m_CurrentColour.a > 1.0f)
        {
            m_Renderer.material = m_OriginalMaterial;
            m_Renderer.material.color = m_OriginalColour;
            m_Camera.AlertDeactive(gameObject);
            Destroy(this);
        }
    }

	// Update is called once per frame
	void Update () {        
        if (m_Started)
            IncreaseTransparency();
        else if(m_Stopped)
            DecreaseTransparency();
	}

    public void TransparencyStart(Material shadowMat)
    {
        if(!m_Started)
        {
            m_Camera = Camera.main.GetComponent<CameraClearView>();
            m_Renderer = gameObject.GetComponent<Renderer>();
            m_OriginalMaterial = m_Renderer.material;
            m_OriginalColour = m_Renderer.material.color;
            m_NewMaterial = new Material(shadowMat)
            {
                mainTexture = m_OriginalMaterial.mainTexture,
                mainTextureOffset = m_OriginalMaterial.mainTextureOffset,
                mainTextureScale = m_OriginalMaterial.mainTextureScale,
                color = m_OriginalColour
            };
            m_Renderer.material = m_NewMaterial;
            IncreaseTransparency();
            m_Started = true;
            m_Stopped = false;
            m_Camera.AlertActive(gameObject);
        }
    }

    public void TransparencyEnd()
    {
        m_Started = false;
        m_Stopped = true;
    }
}
