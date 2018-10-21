using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UtilityScoring : MonoBehaviour {

    [Header(" - Game Object Pointers - ")]
    public CampfireScript m_Campfire;
    public StorageScript m_Woodstore, m_Fishstore, m_Berrystore;
    public ResourcePointer m_Forest, m_Pond, m_Bushes;

    [Header(" - Score Function Exponents & Bases - ")]
    [Range(0.01f, 1.0f)]
    public float TaskPercentile = 0.2f;
    public float LIGHT_FIRE_BASE = 0.4f;
    public float LIGHT_FIRE_EXPONENT = 6.0f, EAT_FISH_EXPONENT = 6.0f, EAT_BERRY_EXPONENT = 4.0f;
    public float REFILL_EXPONENT = 2.0f, FORAGE_BASE = 0.2f, REST_EXPONENT = 2.0f;
    public float CFBUCKET_WEIGHT = 1.0f, HBUCKET_WEIGHT = 0.9f, TBUCKET_WEIGHT = 0.9f, FBUCKET_WEIGHT = 0.8f;

    [Header(" - UI Outputs - ")]
    public Text[] m_CFBucketText;
    public Text[] m_HBucketText;
    public Text[] m_TBucketText;
    public Text[] m_FBucketText;
    public UIBucket m_CFBucketUI, m_HBucketUI, m_TBucketUI, m_FBucketUI;

    private class TaskScore
    {        
        public float score;
        public float weight;
        public CharacterTaskDeployer.Task task;
        public Resource resource;
        public int index;

        public TaskScore(float scr, int TextIndex, CharacterTaskDeployer.Task tsk, Resource rsc = Resource.None)
        {
            score = scr;
            index = TextIndex;
            task = tsk;
            resource = rsc;
        }
    }

    private class BucketScore
    {
        public float score;
        public int bucket;

        public BucketScore(float scr, int bkt)
        {
            score = scr;
            bucket = bkt;
        }
    }

    public enum Resource
    {
        Log,
        Fish,
        Berry,
        None,
        Size,
    }

    private BucketScore m_CampfireBkt   = new BucketScore(0.0f, 0),
                        m_HungerBkt     = new BucketScore(0.0f, 1),
                        m_ThirstBkt     = new BucketScore(0.0f, 2),
                        m_FatigueBkt    = new BucketScore(0.0f, 3);

    private List<TaskScore> m_Scores;
    private CharacterInventory m_Inventory;
    private CharacterStats m_Stats;
    private CharacterTaskDeployer m_TaskDeployer;
    private TaskScore m_NewTask;

	// Use this for initialization
	void Start () {
        m_Inventory = GetComponent<CharacterInventory>();
        m_Stats = GetComponent<CharacterStats>();
        m_TaskDeployer = GetComponent<CharacterTaskDeployer>();
        m_Scores = new List<TaskScore>();
	}
	
    private bool AbleToWork()
    {
        if (m_Stats.GetThirst() >= 85.0f)
            return false;
        if (m_Stats.GetHunger() >= 85.0f)
            return false;
        if (m_Stats.GetFatigue() >= 85.0f)
            return false;
        return true;
    }

    private void WeighScores(int numTasks, Text[] BucketText)
    {
        float scoresTotal = 0.0f;
        m_Scores.ForEach(scr => scoresTotal += scr.score);
        for(int i = 0; i < m_Scores.Count; i++)
        {
            float weight = m_Scores[i].weight = m_Scores[i].score / scoresTotal;
            BucketText[m_Scores[i].index + numTasks].text = (weight * 100).ToString("N0") + "%";
        }
    }

    private TaskScore SelectTask()
    {
        float randomValue = Random.value; // Create a random between 0.0 and 1.0
        int index = 0;                    // Track which element
        while(randomValue > 0.0f && index < m_Scores.Count) // When this val is 0 or less, select that task
        {
            randomValue -= m_Scores[index].weight;  // Take this tasks weight away from our random value
            if (randomValue > 0.0f)                 // Not been selected
                index++;                            // So increment index
            else
                return m_Scores[index];                       // Otherwise task has been selected
        }
        return m_Scores[0];   // In case of error (possibly due to floating point errors) just select best option
    }

    private bool HandleScores(int numTasks, Text[] BucketText)
    {
        if (m_Scores.Count <= 0)
            return false;
        m_Scores.Sort((v1, v2) => v1.score.CompareTo(v2.score));
        m_Scores.Reverse();
        float winningScore = m_Scores[0].score;
        float minScore = winningScore - (winningScore * TaskPercentile);

        Color red = new Color(0.9f, 0.5f, 0.5f, 1.0f);
        Color yel = new Color(0.9f, 0.9f, 0.4f, 1.0f);
        Color toSet;

        foreach (TaskScore task in m_Scores)
        {
            toSet = yel;
            if (task.score < minScore)
            {
                BucketText[task.index + numTasks].text = "Elim.";
                toSet = red;
            }
            BucketText[task.index + numTasks].color = toSet;
            BucketText[task.index].color = toSet;            
        }

        m_Scores.RemoveAll(task => task.score < minScore);

        if (m_Scores.Count == 1)
        {
            m_NewTask = m_Scores[0];
            BucketText[m_NewTask.index + numTasks].text = "100%";
            BucketText[m_NewTask.index].color = BucketText[m_NewTask.index + numTasks].color = Color.green;
            return true;
        }
        else if (m_Scores.Count > 1)
        {
            WeighScores(numTasks, BucketText);
            m_NewTask = SelectTask();
            BucketText[m_NewTask.index].color = BucketText[m_NewTask.index + numTasks].color = Color.green;
            return true;
        }
        else
            return false;
    }

    private bool CampfireBucket()
    {
        float score;
        TaskScore newScore;
        int TaskIndex = 1;
        m_Scores.Clear();
        m_CFBucketUI.ToggleTasks(true);

        // Light campfire
        // Only score it if the campfire isn't lit
        if (!m_Campfire.IsLit() && m_Inventory.QueryLog() + m_Woodstore.QueryAmount() + m_Campfire.GetLogsRemaining() > 0)
        {
            float TimeUnlit = m_Campfire.GetUnlitTime();
            float MaxTime = m_Campfire.m_WolfThreatTime;
            score = Mathf.Min(Mathf.Pow(TimeUnlit / MaxTime, LIGHT_FIRE_EXPONENT) + LIGHT_FIRE_BASE, 1.0f);            
            newScore = new TaskScore(score, TaskIndex, CharacterTaskDeployer.Task.Firelighting);
            m_Scores.Add(newScore);
            m_CFBucketText[TaskIndex].text = score.ToString("0.000");
        }
        TaskIndex++;

        // Add logs to fire
        // Only if we have logs AND the campfire isn't full
        if (m_Inventory.QueryLog() + m_Woodstore.QueryAmount() > 0 && m_Campfire.GetLogsRemaining() < m_Campfire.m_MaxLogs)
        {
            float FireRatio = 1.0f - (m_Campfire.GetAccurateLogsRemaining() / m_Campfire.m_MaxLogs);
            score = Mathf.Max(FireRatio, 0.0f);            
            newScore = new TaskScore(score, TaskIndex, CharacterTaskDeployer.Task.Firebuilding);
            m_Scores.Add(newScore);
            m_CFBucketText[TaskIndex].text = score.ToString("0.000");
        }
        TaskIndex++;

        // Chop wood
        // Only if there's space in inventory AND if theres trees available AND if the characters stats aren't too high
        if (m_Inventory.QuerySpace((int)Resource.Log) > 0 && m_Forest.GetActiveCount() > 0 && AbleToWork())
        {
            float CurrentWood = m_Woodstore.QueryAmount() + m_Inventory.QueryLog() + m_Campfire.GetLogsRemaining();
            float MaxWood = m_Woodstore.m_StorageLimit + m_Campfire.m_MaxLogs;
            score = Mathf.Min(1.0f - (CurrentWood / MaxWood), 1.0f);            
            newScore = new TaskScore(score, TaskIndex, CharacterTaskDeployer.Task.Chopping);
            m_Scores.Add(newScore);
            m_CFBucketText[TaskIndex].text = score.ToString("0.000");
        }
        TaskIndex++;

        // Deposit logs
        // Only score if the character has logs and there is space in the store
        if (m_Inventory.QueryLog() > 0 && m_Woodstore.QuerySpace() > 0)
        {
            score = (m_Inventory.QueryLog() * m_Inventory.m_LogWeight) / m_Inventory.QueryWeight();            
            newScore = new TaskScore(score, TaskIndex, CharacterTaskDeployer.Task.Depositing, Resource.Log);
            m_Scores.Add(newScore);
            m_CFBucketText[TaskIndex].text = score.ToString("0.000");
        }

        return HandleScores(TaskIndex, m_CFBucketText);
    }

    private bool HungerBucket()
    {
        float score;
        int TaskIndex = 1;
        TaskScore newScore;
        m_Scores.Clear();
        m_HBucketUI.ToggleTasks(true);

        float Hunger = m_Stats.GetHunger() / 100.0f;

        // Eat a fish
        // Only score if we have fish or space in inventory for fish from storage
        if (m_Inventory.QueryFish() > 0 || (m_Fishstore.QueryAmount() > 0 && m_Inventory.QuerySpace((int)Resource.Fish) > 0))
        {
            float FishStored = ((float)m_Inventory.QueryFish() + m_Fishstore.QueryAmount()) / (float)m_Fishstore.m_StorageLimit;
            float Restore = m_Stats.m_FishRestore / 100.0f;
            float Efficiency = Mathf.Pow(1.0f - Mathf.Max(1.0f - Hunger - Restore, 0.0f), EAT_FISH_EXPONENT);        
            score = Mathf.Min((Hunger * 2.0f + FishStored + Efficiency * 2.0f) / 5.0f, 1.0f);            
            newScore = new TaskScore(score, TaskIndex, CharacterTaskDeployer.Task.Eating, Resource.Fish);
            m_Scores.Add(newScore);
            m_HBucketText[TaskIndex].text = score.ToString("0.000");
        }
        TaskIndex++;

        // Eat berries
        // Only if there's some in inventory or store
        if (m_Inventory.QueryBerry() > 0 || (m_Berrystore.QueryAmount() > 0 && m_Inventory.QuerySpace((int)Resource.Berry) > 0))
        {
            float Berries = ((float)m_Inventory.QueryBerry() + m_Berrystore.QueryAmount()) / (float)m_Berrystore.m_StorageLimit;
            float Restore = m_Stats.m_BerryRestore / 100.0f;
            float Efficiency = Mathf.Pow(1.0f - Mathf.Max(1.0f - Hunger - Restore, 0.0f), EAT_BERRY_EXPONENT);
            score = Mathf.Min((Hunger * 2.0f + Berries + Efficiency * 2.0f) / 5.0f, 1.0f);            
            newScore = new TaskScore(score, TaskIndex, CharacterTaskDeployer.Task.Eating, Resource.Berry);
            m_Scores.Add(newScore);
            m_HBucketText[TaskIndex].text = score.ToString("0.000");
        }
        TaskIndex++;

        // Go fishing
        // Only if there's space in inventory AND if there is fishing spots available
        if (m_Inventory.QuerySpace((int)Resource.Fish) > 0 && m_Pond.GetActiveCount() > 0)
        {
            float Fish = 1.0f - ((m_Fishstore.QueryAmount() + m_Inventory.QueryFish()) / m_Fishstore.m_StorageLimit);
            score = Mathf.Max((Fish + Hunger) / 2.0f, 0.0f);            
            newScore = new TaskScore(score, TaskIndex, CharacterTaskDeployer.Task.Fishing);
            m_Scores.Add(newScore);
            m_HBucketText[TaskIndex].text = score.ToString("0.000");
        }
        TaskIndex++;

        // Forage
        if (m_Inventory.QuerySpace((int)Resource.Berry) > 0 && m_Stats.GetThirst() < 70.0f && m_Bushes.GetActiveCount() > 0)
        {
            float Berries = 1.0f - (((float)m_Berrystore.QueryAmount() + m_Inventory.QueryBerry()) / m_Berrystore.m_StorageLimit);
            score = Mathf.Max(((Berries * 2.0f - FORAGE_BASE) + Hunger) / 3.0f, 0.0f);            
            newScore = new TaskScore(score, TaskIndex, CharacterTaskDeployer.Task.Foraging);
            m_Scores.Add(newScore);
            m_HBucketText[TaskIndex].text = score.ToString("0.000");
        }
        TaskIndex++;

        // Deposit fish
        if (m_Inventory.QueryFish() > 0)
        {
            float FishRatio = (m_Inventory.QueryFish() * m_Inventory.m_FishWeight) / m_Inventory.QueryWeight();
            float InventoryRatio = (float)m_Inventory.QueryWeight() / m_Inventory.m_MaxWeight;
            score = (FishRatio + InventoryRatio) / 2.0f;            
            newScore = new TaskScore(score, TaskIndex, CharacterTaskDeployer.Task.Depositing, Resource.Fish);
            m_Scores.Add(newScore);
            m_HBucketText[TaskIndex].text = score.ToString("0.000");
        }
        TaskIndex++;

        // Deposit berry
        if (m_Inventory.QueryBerry() > 0)
        {
            float BerriesRatio = (m_Inventory.QueryBerry() * m_Inventory.m_BerryWeight) / m_Inventory.QueryWeight();
            float InventoryRatio = (float)m_Inventory.QueryWeight() / m_Inventory.m_MaxWeight;
            score = (BerriesRatio + InventoryRatio) / 2.0f;            
            newScore = new TaskScore(score, TaskIndex, CharacterTaskDeployer.Task.Depositing, Resource.Berry);
            m_Scores.Add(newScore);
            m_HBucketText[TaskIndex].text = score.ToString("0.000");
        }

        return HandleScores(TaskIndex, m_HBucketText);
    }

    private bool ThirstBucket()
    {
        float score;
        int TaskIndex = 1;
        TaskScore newScore;
        m_Scores.Clear();
        m_TBucketUI.ToggleTasks(true);

        // Drink from flask
        // Only score if we have water in flask
        if (m_Inventory.QueryFlask() > 0)
        {
            float Thirst = m_Stats.GetThirst();
            score = Thirst / 100.0f;            
            newScore = new TaskScore(score, TaskIndex, CharacterTaskDeployer.Task.Drinking);
            m_Scores.Add(newScore);
            m_TBucketText[TaskIndex].text = score.ToString("0.000");
        }
        TaskIndex++;

        // Refill flask
        if (m_Inventory.QueryFlask() < 100)
        {
            float Thirst = m_Stats.GetThirst() / 100.0f;
            float Flask = 1.0f - (float)m_Inventory.QueryFlask() / m_Inventory.m_MaxWaterFlask;
            score = Mathf.Pow((Flask + Thirst) / 2.0f, REFILL_EXPONENT);            
            newScore = new TaskScore(score, TaskIndex, CharacterTaskDeployer.Task.RefillingFlask);
            m_Scores.Add(newScore);
            m_TBucketText[TaskIndex].text = score.ToString("0.000");
        }

        return HandleScores(TaskIndex, m_TBucketText);
    }

    private bool FatigueBucket()
    {
        float score;
        int TaskIndex = 1;
        TaskScore newScore;
        m_Scores.Clear();
        m_FBucketUI.ToggleTasks(true);

        // Rest
        // Only score if hunger and thirst won't reach 90 or higher after completion
        if (   (m_Stats.GetHunger() + (m_Stats.m_HungerRate / m_Stats.m_DrainTickTime) * m_TaskDeployer.GetTaskTime(CharacterTaskDeployer.Task.Resting)) < 90.0f
            && (m_Stats.GetThirst() + (m_Stats.m_ThirstRate / m_Stats.m_DrainTickTime) * m_TaskDeployer.GetTaskTime(CharacterTaskDeployer.Task.Resting)) < 90.0f)
        {
            score = Mathf.Pow(m_Stats.GetFatigue() / 100.0f, REST_EXPONENT);            
            newScore = new TaskScore(score, TaskIndex, CharacterTaskDeployer.Task.Resting);
            m_Scores.Add(newScore);
            m_FBucketText[TaskIndex].text = score.ToString("0.000");
        }
        TaskIndex++;

        // Sleep in tent
        // Only score above 50 fatigue and if hunger and thirst won't reach 90 or higher after completion
        if (m_Stats.GetFatigue() > 50.0f
            && (m_Stats.GetHunger() + (m_Stats.m_HungerRate / m_Stats.m_DrainTickTime) * m_TaskDeployer.GetTaskTime(CharacterTaskDeployer.Task.Sleeping)) < 90.0f
            && (m_Stats.GetThirst() + (m_Stats.m_ThirstRate / m_Stats.m_DrainTickTime) * m_TaskDeployer.GetTaskTime(CharacterTaskDeployer.Task.Sleeping)) < 90.0f)
        {
            float Fatigue = m_Stats.GetFatigue() / 100.0f;
            float TimeLeft = m_Campfire.GetTimeRemaining();
            float CFScore = (TimeLeft - m_TaskDeployer.GetTaskTime(CharacterTaskDeployer.Task.Sleeping)) / 100.0f;
            score = ((Fatigue * 2.0f) + CFScore) / 3.0f;
            newScore = new TaskScore(score, TaskIndex, CharacterTaskDeployer.Task.Sleeping);
            m_Scores.Add(newScore);
            m_FBucketText[TaskIndex].text = score.ToString("0.000");
        }

        return HandleScores(TaskIndex, m_FBucketText);
    }

    public void CalculateScores()
    {        
        ResetUIText();
        List<BucketScore> Scores = new List<BucketScore>();        

        float InverseCampfire = 1.0f - (m_Campfire.GetTimeRemaining() / (m_Campfire.m_TimePerLog * m_Campfire.m_MaxLogs));
        m_CampfireBkt.score = InverseCampfire * CFBUCKET_WEIGHT;
        m_CFBucketText[0].text = m_CampfireBkt.score.ToString("0.000");
        Scores.Add(m_CampfireBkt);

        float Hunger = m_Stats.GetHunger() / 100.0f;
        m_HungerBkt.score = Hunger * HBUCKET_WEIGHT;
        m_HBucketText[0].text = m_HungerBkt.score.ToString("0.000");
        Scores.Add(m_HungerBkt);

        float Thirst = m_Stats.GetThirst() / 100.0f;
        m_ThirstBkt.score = Thirst * TBUCKET_WEIGHT;
        m_TBucketText[0].text = m_ThirstBkt.score.ToString("0.000");
        Scores.Add(m_ThirstBkt);

        m_FatigueBkt.score = (m_Stats.GetFatigue() / 100.0f) * FBUCKET_WEIGHT;
        m_FBucketText[0].text = m_FatigueBkt.score.ToString("0.000");
        Scores.Add(m_FatigueBkt);

        Scores.Sort((v1, v2) => v1.score.CompareTo(v2.score));
        Scores.Reverse();
        
        int counter = 0;
        bool taskFound = false;
        while(!taskFound && counter < Scores.Count)
        {
            int functionID = Scores[counter].bucket;
            switch(functionID)
            {
                case 0:
                    taskFound = CampfireBucket();
                    break;
                case 1:
                    taskFound = HungerBucket();
                    break;
                case 2:
                    taskFound = ThirstBucket();
                    break;
                case 3:
                    taskFound = FatigueBucket();
                    break;
            }
            counter++;
        }

        if(!taskFound)
        {
            // No task set
        }
        else
        {
            switch (m_NewTask.task)
            {
                case CharacterTaskDeployer.Task.Firelighting:
                    {
                        if (m_Campfire.GetLogsRemaining() == 0)
                        {
                            if (m_Inventory.QueryLog() == 0)
                                m_TaskDeployer.QueueNewTask(CharacterTaskDeployer.Task.Withdrawing, CharacterTaskDeployer.Resource.Log, 1);                            
                            m_TaskDeployer.QueueNewTask(CharacterTaskDeployer.Task.Firebuilding, CharacterTaskDeployer.Resource.Log, 1);
                        }
                        else
                            m_TaskDeployer.QueueNewTask(CharacterTaskDeployer.Task.Firelighting, CharacterTaskDeployer.Resource.Log);
                        break;
                    }
                case CharacterTaskDeployer.Task.Firebuilding:
                    {
                        int toAdd = 0;
                        if (m_Inventory.QueryLog() == 0)
                        {
                            toAdd = Mathf.Min(m_Inventory.QuerySpace((int)Resource.Log), m_Woodstore.QueryAmount());
                            m_TaskDeployer.QueueNewTask(CharacterTaskDeployer.Task.Withdrawing, CharacterTaskDeployer.Resource.Log, toAdd);
                        }
                        else
                            toAdd = m_Inventory.QueryLog();
                        m_TaskDeployer.QueueNewTask(CharacterTaskDeployer.Task.Firebuilding, CharacterTaskDeployer.Resource.Log, toAdd);
                        break;
                    }
                case CharacterTaskDeployer.Task.Chopping:
                    {
                        int amountToChop = Mathf.Min(m_Inventory.QuerySpace((int)Resource.Log), 5);
                        m_TaskDeployer.QueueNewTask(CharacterTaskDeployer.Task.Chopping, CharacterTaskDeployer.Resource.Log, amountToChop);
                        break;
                    }
                case CharacterTaskDeployer.Task.Eating:
                    {
                        if (m_NewTask.resource == Resource.Fish)
                        {
                            if (m_Inventory.QueryFish() == 0)
                            {
                                m_TaskDeployer.QueueNewTask(CharacterTaskDeployer.Task.Withdrawing, CharacterTaskDeployer.Resource.Fish, 1);
                            }
                            m_TaskDeployer.QueueNewTask(CharacterTaskDeployer.Task.Eating, CharacterTaskDeployer.Resource.Fish, 1);
                        }
                        else
                        {
                            if (m_Inventory.QueryBerry() == 0)
                            {
                                m_TaskDeployer.QueueNewTask(CharacterTaskDeployer.Task.Withdrawing, CharacterTaskDeployer.Resource.Berry, 1);
                            }
                            m_TaskDeployer.QueueNewTask(CharacterTaskDeployer.Task.Eating, CharacterTaskDeployer.Resource.Berry, 1);
                        }
                        break;
                    }
                case CharacterTaskDeployer.Task.Fishing:
                    {
                        m_TaskDeployer.QueueNewTask(CharacterTaskDeployer.Task.Fishing, CharacterTaskDeployer.Resource.Fish, 1);
                        break;
                    }
                case CharacterTaskDeployer.Task.Foraging:
                    {
                        int amountToForage = Mathf.Min(m_Inventory.QuerySpace((int)Resource.Berry), 5);
                        m_TaskDeployer.QueueNewTask(CharacterTaskDeployer.Task.Foraging, CharacterTaskDeployer.Resource.Berry, amountToForage);
                        break;
                    }
                case CharacterTaskDeployer.Task.Depositing:
                    {
                        if (m_Inventory.QueryAmount((int)m_NewTask.resource) > 0)
                        {
                            if(m_TaskDeployer.m_Stores[(int)m_NewTask.resource].QuerySpace() == 0)
                            {
                                // store is full and inventory is full, just discard excess
                                m_Inventory.TakeAmount(m_Inventory.QueryAmount((int)m_NewTask.resource), (int)m_NewTask.resource);
                            }
                            else
                            {
                                int amountToDeposit = Mathf.Min(m_Inventory.QueryAmount((int)m_NewTask.resource), m_TaskDeployer.m_Stores[(int)m_NewTask.resource].QuerySpace());
                                m_TaskDeployer.QueueNewTask(CharacterTaskDeployer.Task.Depositing, (CharacterTaskDeployer.Resource)m_NewTask.resource, amountToDeposit);
                            }
                        }
                        break;
                    }
                case CharacterTaskDeployer.Task.Drinking:
                    {
                        m_TaskDeployer.QueueNewTask(CharacterTaskDeployer.Task.Drinking, CharacterTaskDeployer.Resource.None);
                        break;
                    }
                case CharacterTaskDeployer.Task.RefillingFlask:
                    {
                        m_TaskDeployer.QueueNewTask(CharacterTaskDeployer.Task.RefillingFlask, CharacterTaskDeployer.Resource.None);
                        break;
                    }
                case CharacterTaskDeployer.Task.Resting:
                    {
                        m_TaskDeployer.QueueNewTask(CharacterTaskDeployer.Task.Resting, CharacterTaskDeployer.Resource.None);
                        break;
                    }
                case CharacterTaskDeployer.Task.Sleeping:
                    {
                        m_TaskDeployer.QueueNewTask(CharacterTaskDeployer.Task.Sleeping, CharacterTaskDeployer.Resource.None);
                        break;
                    }                
            }
        }
    }

    private void ResetUIText()
    {
        foreach (Text txt in m_CFBucketText)
        {
            txt.text = "-.-";
            txt.color = Color.white;
        }            
        foreach (Text txt in m_HBucketText)
        {
            txt.text = "-.-";
            txt.color = Color.white;
        }
        foreach (Text txt in m_TBucketText)
        {
            txt.text = "-.-";
            txt.color = Color.white;
        }
        foreach (Text txt in m_FBucketText)
        {
            txt.text = "-.-";
            txt.color = Color.white;
        }

        m_CFBucketUI.ToggleTasks(false);
        m_HBucketUI.ToggleTasks(false);
        m_FBucketUI.ToggleTasks(false);
        m_TBucketUI.ToggleTasks(false);
    }
}
