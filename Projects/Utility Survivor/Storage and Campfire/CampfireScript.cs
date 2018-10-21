using UnityEngine;
using UnityEngine.UI;

public class CampfireScript : MonoBehaviour {

    public int m_MaxLogs, m_WolfAttackOdds;
    public float m_TimePerLog, m_WolfThreatTime;
    public Text m_CountText, m_TimerText, m_WolfThreatText;
    public WolfScript m_Wolf;

    private bool m_IsLit, m_WolfTriggered = false;
    private int m_CurrentWood;
    private float m_TimeLeftOnLog;
    private FireHandler m_ChildScript;
    private float m_TimeUnlit;
    private AudioManager m_AudioManager;
    private Sound m_ActiveSound;

	// Use this for initialization
	void Start () {
        m_ChildScript = GetComponentInChildren<FireHandler>();
        m_CurrentWood = 0;
        m_TimeLeftOnLog = 0;
        m_IsLit = false;
        m_AudioManager = FindObjectOfType<AudioManager>();
        m_WolfThreatText.enabled = false;
        UpdateCountText();
    }
	
	// Update is called once per frame
	void Update () {
        if (m_IsLit)
        {
            m_TimeLeftOnLog -= Time.deltaTime;
            if (m_TimeLeftOnLog <= 0.0f)
            {
                if (m_CurrentWood > 0)
                {
                    m_CurrentWood -= 1;
                    m_TimeLeftOnLog = m_TimePerLog;
                }
                else
                {
                    m_ChildScript.Extinguish(false);
                    m_TimeLeftOnLog = 0.0f;
                    m_IsLit = false;
                }
                UpdateCountText();
            }
        }
        else
        {
            m_TimeUnlit += Time.deltaTime;
            if(m_TimeUnlit >= m_WolfThreatTime && !m_WolfTriggered)
            {
                if (!m_WolfThreatText.isActiveAndEnabled)
                    m_WolfThreatText.enabled = true;
                int chanceLimit = m_WolfAttackOdds - (((int)m_TimeUnlit - (int)m_WolfThreatTime) * 5);
                int randomRoll = Random.Range(0, chanceLimit);
                if(randomRoll == chanceLimit - 1)
                {
                    m_WolfThreatText.enabled = false;
                    m_Wolf.TriggerAttack();
                    m_WolfTriggered = true;
                }
            }
        }
            
        UpdateUITimer();
    }
    
    private void UpdateCountText()
    {
        m_CountText.text = m_CurrentWood.ToString() + "/" + m_MaxLogs.ToString();
        m_CountText.color = Color.Lerp(Color.red, Color.green, (float)m_CurrentWood / m_MaxLogs);
    }

    private void UpdateUITimer()
    {
        if(m_IsLit)
            m_TimerText.text = m_TimeLeftOnLog.ToString("0") + "s";
        else
        {
            m_TimerText.text = "-" + m_TimeUnlit.ToString("0") + "s";
            m_TimerText.color = Color.Lerp(Color.white, Color.red, Mathf.Min(m_TimeUnlit / m_WolfThreatTime, 1.0f));
        }
            
    }

    public bool AddLogs(int amount)
    {
        if (m_CurrentWood + amount <= m_MaxLogs)
        {
            m_CurrentWood += amount;
            if (m_CurrentWood == 1)
                m_ChildScript.Built();
            UpdateCountText();
            return true;
        }
        else
            return false;
    }
    
    public bool Light()
    {
        if (!m_IsLit && m_CurrentWood > 0)
        {
            m_CurrentWood -= 1;
            m_TimeLeftOnLog = m_TimePerLog;
            m_ChildScript.Light();
            m_TimerText.color = Color.green;
            m_IsLit = true;
            m_TimeUnlit = 0.0f;
            m_ActiveSound = m_AudioManager.Play("Firecrackle", gameObject);
            UpdateCountText();
            if(m_WolfTriggered)
            {
                m_Wolf.CancelAttack();
                m_WolfTriggered = false;
            }
            return true;
        }
        else
            return false;
    }

    public void BlownOut()
    {
        if(m_IsLit)
        {
            m_AudioManager.Stop(m_ActiveSound);
            m_TimeLeftOnLog = 0.0f;
            m_CurrentWood += 1;
            m_IsLit = false;
            m_ChildScript.Extinguish(true);
        }
    }

    public void ForceLight()
    {
        if (!m_IsLit)
        {
            if(m_CurrentWood == 0)
            {
                m_CurrentWood += 1;
            }
            CharacterTaskDeployer character = FindObjectOfType<CharacterTaskDeployer>();
            if (character.GetCurrentTaskID() == CharacterTaskDeployer.Task.Firelighting)
            {
                character.InteruptTask();
            }
            Light();
        }
    }

    public bool IsLit() { return m_IsLit; }
    public float GetUnlitTime() { return m_TimeUnlit; }
    public float GetTimeRemaining()
    {
        if (m_IsLit)
            return m_TimeLeftOnLog + (m_TimePerLog * m_CurrentWood);
        else
            return 0.0f;
    }
    public int GetLogsRemaining() { return m_CurrentWood; }
    public float GetAccurateLogsRemaining() { return (float)m_CurrentWood + ( m_TimeLeftOnLog / m_TimePerLog); }
}
