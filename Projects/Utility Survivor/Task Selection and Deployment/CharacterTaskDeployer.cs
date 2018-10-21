using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CharacterTaskDeployer : MonoBehaviour {
    // Any /**/ indicates that it is subject to being changed/not yet implemented

    // ***********************************
    // *** Public/Serialized variables ***
    [Header(" - Game Object Pointers - ")]
    public ResourcePointer[] m_Resources;   // Array of each resource parent/pointer
    public StorageScript[] m_Stores;        // Array of pointers to varying stores, uses Resource type as index
    public CampfireScript m_Campfire;       // Pointer to the campfire and it's script
    public GameObject m_WaterWell;

    [Header(" - UI Outputs - ")]
    public Image m_ProgressBar;
    public Image m_TimerBar;    
    public Text m_UITaskText;
    public ProgressBar m_ProgressScript;

    [Header(" - Other - ")]
    public List<Task> m_PoseAlternate;

    // Enums are more readable, so this is used to express a task
    public enum Task
    {
        Idle,       // No current task
        Chopping,       // Chopping trees
        Fishing,       // Fishing
        Firebuilding,  // <-Adding to fire
        Firelighting,  // <-Lighting fire
        Foraging,     // <-Gathering berries
        Resting,       // Resting by fire
        Sleeping,      // Sleeping in tent
        Drinking,      // Drinking from flask
        Eating,        // Eating food items
        RefillingFlask,     // Refilling flask
        Depositing,    // Deposit a resource to some store
        Withdrawing,   // Withdraw a resource from somewhere
        Size,       // Number of [Task]s, not a task itself
    }

    public enum Resource
    {
        Log,    // Index for Log(s) in m_Weights and m_Amounts
        Fish,   // Again for Fish..
        Berry,  // And Berry(s)
        None,   // For task deployer use, not massively important but nice to have
        Size,   // Tracks size of enum, could be useful
    }

    // *************************
    // *** Private variables ***
    private SimpleMovement m_MoveScript;        // For calling movement methods such as .IsMoving()
    private PathFinding m_PathFinder;           // For calling path finding methods such as .PathSearch() and .GetNode()
    private CharacterInventory m_Inventory;     // For accessing character inventory for adding and taking resources
    private CharacterStats m_Stats;
    private PlayerMesh m_Mesh;                  // For updating players mesh for actions and "animations"
    private ResourceScript m_CurrentResource;   // Pointer to the script of the current resource being used
    private UtilityScoring m_UtilityScript;

    private float m_Timer;   // Timer progress on current task
    private bool m_InPosition,   // Is in position for task?
        m_TimerStarted,          // Is timer started for task?
        m_TaskCompleted,          // Is task complete yet?
        m_CantComplete,      // Could the task not be completed?
        m_Alternate;            // Does the pose alternate on this task?
    private int m_TaskGoal,      // Target amount for a task, could be resources or time etc
        m_TaskProgress;          // Progress along current task
    private Task m_CurrentTask;  // Using enums allows for more readable use of indexs, this one tracks the current task
    private LinkedList<QueuedTask> m_Queue; // This is so one task can queue more to run straight after completion (i.e. Light fire after building if not lit)
    private Resource m_ResourceType; // Resource used in current task
    private string m_TargetNodeString; // Holds current target node string
    private AudioManager m_Audio;
    private string m_TaskSoundName;
    private Sound m_ActiveSound;
    

    private string[] m_TaskToNode =
    {   // Uses (int)[Task] as index to return node's string
        "CenterCamp",   // Idle
        "ChopSpot",     // Chopping
        "FishSpot",     // Fishing
        "Campfire",     // Firebuilding
        "Campfire",     // Firelighting
        "Bushes",       // Gathering
        "RestingSpot",  // Resting
        "SleepSpot",    // Sleeping
        "",             // Drinking
        "RestingSpot",  // Eating
        "RefillSpot",   // Refilling flask
        "#fetch#",      // Withdraw
        "#fetch#",      // Deposit
    };                  // "" = no node needed or node chosen dynamically

    private readonly float[] m_TaskTimes =
    {
        0.0f, 5.0f, 10.0f, 2.0f, 5.0f,
        2.0f, 20.0f, 120.0f, 3.0f, 5.0f,
        5.0f, 1.0f, 1.0f
    };

    // Simple class for holding information needed for a Queued task, uses default getters and setters
    private class QueuedTask
    {
        public Task TaskID { get; set; }  // Task identifier
        public int Amount { get; set; }   // Task amount
        public Resource Resource { get; set; } // Task resource

        // Constructor with default value as optional
        public QueuedTask(Task id, Resource res, int amount = 0)
        {
            TaskID = id;
            Resource = res;
            Amount = amount;            
        }
    }

    private void Start ()
    { 
        // Getting components and setting them to relevant pointers
        m_UtilityScript = GetComponent<UtilityScoring>();
        m_MoveScript = GetComponent<SimpleMovement>();
        m_PathFinder = GetComponent<PathFinding>();
        m_Inventory  = GetComponent<CharacterInventory>();
        m_Stats      = GetComponent<CharacterStats>();        
        m_Mesh       = GetComponentInChildren<PlayerMesh>(); // Mesh is actually in a child of the GameObject with this script attached to
        m_Queue = new LinkedList<QueuedTask>();
        m_Audio = FindObjectOfType<AudioManager>();
    }

    private void Update () {
        // Task functions are decided and called in here 
        // *
        if (m_CurrentTask != Task.Idle) // If not currently idle, i.e. on a task...
        {
            ProgressBar();
            if (m_TaskCompleted || m_CantComplete) // Check if task is complete or could not complete
            {
                ResetCharacterTransform();  // Return character to a pathable position and rotation, also default it's mesh
                ResetTaskVars();            // Reset all task variables ready for use again
                if (m_Queue.Count > 0)      // Check if there's queued tasks
                {
                    QueuedTask newTask = m_Queue.First.Value;                    // If there is, take the first one queued
                    m_Queue.RemoveFirst();                                       // then remove it
                    StartTask(newTask.TaskID, newTask.Resource, newTask.Amount); // And use StartTask to properly start it
                }                    
                else        // Nothing queued so...
                {
                    m_ProgressScript.Hide(); // Hide our progress bar
                    return;                  // Exit and remain idle
                }                    
            }            
            // Switch to choose function(s) based on current task
            switch (m_CurrentTask) 
            {
                case Task.Chopping: // Handles woodcutting for logs                    
                case Task.Fishing: // Handles fishing for... fish!
                case Task.Foraging: // Handles gathering berries
                    GatherResource();
                    break;
                case Task.Firebuilding: // Handles adding logs to fire, will automatically queued LightFire on completion if fire not lit
                    AddToFire();
                    break;
                case Task.Firelighting: // Handles lighting the fire
                    LightFire();
                    break;
                case Task.Resting: // Handles resting near campfire
                    Rest();
                    break;
                case Task.Sleeping: // Handles sleeping in tent
                    Sleep();
                    break;
                case Task.Drinking: // Handles drinking from water flask
                    Drink();
                    break;
                case Task.Eating:
                    Eat();
                    break;
                case Task.RefillingFlask:
                    RefillFlask();
                    break;
                case Task.Depositing: // Handles depositing resource to a store
                    StoreResource();
                    break;
                case Task.Withdrawing: // Handles withdrawing resource from store
                    TakeResource();
                    break;
                default:                        // In case of a task input that isn't valid
                    m_CurrentTask = Task.Idle;  // Return to idle
                    break;
            }
        }
        else
        {
            m_UtilityScript.CalculateScores();
        }
	} // End of Update() function

    // Returns the character to a pathable position and rotation, at the correct node
    // Some parts of this method may be redundant at times, but it's easier than checking each time
    private void ResetCharacterTransform()
    {
        m_Mesh.ResetMesh();                                             // Return mesh to default
    /**/Path_Node currentNode = m_PathFinder.GetCurrentNode();          // Get the current node as Path_Node
        transform.position = currentNode.transform.position;            // Set character position to current node position
        Vector3 lookPoint = currentNode.ParentNode.transform.position;  // Get the position of the parent node to current node
        lookPoint.y = transform.position.y;                             // Change the y component of it to character's
        transform.LookAt(lookPoint);                                    // then look at it; prevents the character looking at an angle other than 90 degrees which is how I want it
    }

    // Simple function to reset all task tracking variables
    // Again, some parts may be redundant at times, but still easier than alternative
    private void ResetTaskVars()
    {
        m_ActiveSound = null;
        m_CurrentTask = Task.Idle;
        m_Timer = 0.0f;
        m_InPosition =  m_TimerStarted = m_TaskCompleted = m_CantComplete = false;
        m_TaskGoal = m_TaskProgress = 0 ;
        m_ResourceType = Resource.None;
        m_CurrentResource = null;
        m_Stats.ResetMultipliers();
        UpdateTaskText();
    }

    private void ProgressBar()
    {
        if (m_TaskGoal == 0)
            m_ProgressBar.transform.localScale = new Vector2(0.0f, 1.0f);
        else
            m_ProgressBar.transform.localScale = new Vector2((float)m_TaskProgress / m_TaskGoal, 1.0f);        
    }

    private void UpdateTaskText()
    {
        string taskOutput = m_CurrentTask.ToString();
        if (m_CurrentTask == Task.Depositing || m_CurrentTask == Task.Withdrawing || m_CurrentTask == Task.Eating)
            taskOutput += " " + m_ResourceType.ToString();
        m_UITaskText.text = taskOutput;
    }

    // A (currently) private function to start a task, takes a task as mandatory parameter, then an amount and resource type as optional ones
    // as not all tasks might use these variables, which is why they default as 0, and "None"
    public bool StartTask(Task task, Resource resourceType, int amount = 0)
    {        
        if (m_CurrentTask == Task.Idle) // Check we're Idle and not already on a task
        {
            m_CurrentTask = task;    // Set task
            m_TaskGoal = amount;     // Set task target
            m_TaskProgress = 0;      // Reset progress (potentially redundant because of ResetTaskVars()) but just being safe            
            m_ResourceType = resourceType;                // Set resource type
            m_TargetNodeString = m_TaskToNode[(int)task]; // Set target node using task ID
            m_TaskSoundName = task.ToString();

            if (m_TargetNodeString == "")
                m_TargetNodeString = m_PathFinder.GetCurrentNodeName();

            if (task == Task.Depositing || task == Task.Withdrawing)
                m_TargetNodeString = m_Stores[(int)m_ResourceType].m_NearestNode.uniqueID;

            if (m_PoseAlternate.Contains(m_CurrentTask))
                m_Alternate = true;
            else
                m_Alternate = false;

            m_ProgressScript.Show(); // Show our progress bar
            UpdateTaskText();
            return true;             // Return success (task has started)
        }
        return false; // Otherwise we're already on task, so return false to caller so it knows       
    }

    // Again a (currently) private class to queue a new task to run straight after current task
    public void QueueNewTask(Task task, Resource resourceType, int amount = 0)
    {
        if (m_CurrentTask == Task.Idle)             // If not doing a task
            StartTask(task, resourceType, amount);  // Then just start the task instead of queueing
        else                                        // Otherwise...
            m_Queue.AddLast(new QueuedTask(task, resourceType, amount)); // Add a new QueuedTask object to List using passed values
    }

    // Forces a task to be next instead of back of the queue, useful for automatically lighting a fire or storing resources
    public void QueueNextTask(Task task, Resource resourceType, int amount = 0)
    {
        if (m_CurrentTask == Task.Idle)             // If not doing a task
            StartTask(task, resourceType, amount);  // Then just start the task instead of queueing
        else                                        // Otherwise...
            m_Queue.AddFirst(new QueuedTask(task, resourceType, amount)); // Add a new QueuedTask object to FRONT of list
    }

    // Saving repeated code by extracting movement and pathing checks to this function
    // Returns true for ready to proceed, and false for still moving
    private bool MovementChecker()
    {
        if (m_MoveScript.IsMoving())    // If character is currently moving...
            return false;               // Then return NOT ready flag
        if (m_PathFinder.GetCurrentNodeName() != m_TargetNodeString) // If not moving, and not at desired node..
        {
            m_MoveScript.TryPath(m_TargetNodeString);   // Try to start pathing
            return false;                        // Return NOT ready flag
        }        
        return true; // Otherwise, not moving and at target node so return ready
    } // End of MovementChecker() function

    // Another function saving repeated code. Checks if timer needs starting, starts it if needed,
    // updates it, and returns true if the timer is completed or false if it isn't finished
    private bool TimerChecker()
    {
        if (!m_TimerStarted) // Check if timer has not been started
        {
            m_Timer = m_TaskTimes[(int)m_CurrentTask]; // Setup timer
            m_TimerStarted = true;    // Set timer flag to true
        }

        m_Timer -= Time.deltaTime;  // Update timer

        // Update timer progress bar
        m_TimerBar.transform.localScale = new Vector2(1.0f - (m_Timer / m_TaskTimes[(int)m_CurrentTask]), 1.0f);

        if (m_Timer <= 0.0f) // If timer has completed
        {
            m_TimerBar.transform.localScale = new Vector2(0.0f, 1.0f); // Reset timer bar
            return true;                                               // Then return true
        }            
        return false;               // Otherwise return false
    }

    // Method to attempt adding logs to campfire
    private void AddToFire()
    {
        // Step 1) Movement and pathing checks
        if (!MovementChecker()) // Check if position ready and movement finished
            return;                       // False returned = not ready, so exit function

        // Step 2) Check and set: timer, rotation and mesh
        if(!m_InPosition) // Use position flag to track rotation and mesh
        {
            transform.LookAt(new Vector3(m_Campfire.transform.position.x,   // Look at resource
                transform.position.y, m_Campfire.transform.position.z));    // Ignore y component
            m_Mesh.SetMeshAlternate((int)m_CurrentTask, m_Alternate);                             // Set appropriate mesh
            m_InPosition = true;                                            // Set ready flag
        }
        
        if (TimerChecker()) // Run timer checks and see if it's completed
        {
            if(m_Inventory.TakeAmount(1, (int)Resource.Log)) // Attempt to take a log from character inventory
            { 
                if(m_Campfire.AddLogs(1)) // Attempt to add a log to campfire
                {
                    m_TaskProgress += 1;  // Increment task progress
                    if(m_TaskProgress == m_TaskGoal) // Check if task goal now met
                    {
                        m_TaskCompleted = true;            // Set success flag
                        if(!m_Campfire.IsLit())            // Check if campfire is NOT lit 
                            QueueNextTask(Task.Firelighting, Resource.Log); // Queue the light fire task at front of queue
                        return;                            // Exit function
                    }
                    m_Timer = m_TaskTimes[(int)m_CurrentTask]; // Otherwise reset timer
                    return;                     // and exit function
                }
                m_Inventory.AddAmount(1, (int)Resource.Log); // Otherwise couldn't add to fire so return log to inventory      
            }
            // Failed log withdrawal so quit task
            m_CantComplete = true; // Set failure flag
            return;                // and exit
        } // Otherwise timer not complete so wait till it is
    } // End of AddLog() function

    // Method to attempt to light the campfire
    private void LightFire()
    {
        // Step 1) Movement and pathing checks
        if (!MovementChecker()) // Check if position ready and movement finished
            return;             // False return, not ready, exit function

        // Step 2) Check and set: rotation, mesh and then timer
        if (!m_InPosition) // Use position flag to track rotation and mesh
        {
            transform.LookAt(new Vector3(m_Campfire.transform.position.x, // Look at resource
                transform.position.y, m_Campfire.transform.position.z));  // Ignore y component
            m_Mesh.SetMeshAlternate((int)m_CurrentTask, m_Alternate);                           // Set appropriate mesh
            m_ActiveSound = m_Audio.Play(m_TaskSoundName, gameObject);
            m_InPosition = true;                                          // Set ready flag
        }

        if(TimerChecker()) // Timer setup, update, and check returns if completed
        {
            m_Audio.Stop(m_ActiveSound);
            if(m_Campfire.Light()) // Try to light fire
            {
                m_TaskCompleted = true; // Successful so set success flag
                return;                 // and exit function
            }
            m_CantComplete = true; // Lighting failed, set failure flag
            return;                // and exit function
        }       
    }

    // Handles task of storing a resource in given store
    private void StoreResource()
    {
        // Step 1) Handle movement, pathing, and timer
        if (!MovementChecker())  // Check movement and pathing on desired node
            return;              // If false returned, not ready so exit function

        if (!m_InPosition)
        {
            // Otherwise we're at the right node, so...
            transform.LookAt(new Vector3(m_Stores[(int)m_ResourceType].transform.position.x,        // Look at the resource position .x and .z
                        transform.position.y, m_Stores[(int)m_ResourceType].transform.position.z)); // (keeping y though to avoid weird rotations)
            m_Mesh.SetMeshAlternate((int)m_CurrentTask, m_Alternate);                                 // Set the mesh to alternate between default and chopping to resemble an animation 
            m_InPosition = true;                                                        // Set InPosition flag to true
            // Don't need to return, as we can carry on to setting up timers and start task
        }

        if (!TimerChecker())
            return;

        // Step 2) Check validity and handle deposit
        if (m_Inventory.QueryAmount((int)m_ResourceType) >= m_TaskGoal   // Check inventory contains enough to fulfill request...
            && m_Stores[(int)m_ResourceType].AddUnit(m_TaskGoal))        // ...then try to store - using "&&" saves another "if { }"
        {            
            if(m_Inventory.TakeAmount(m_TaskGoal, (int)m_ResourceType))  // Take amount from inventory, but check if successful (just in case)
            {                
                m_TaskCompleted = true; // If so, task is complete
                return;                 // So exit function
            }
            // TakeAmount() failed...
            // This shouldn't run since inventory is check twice, and storage has success check but just incase of a bug...
            m_Stores[(int)m_ResourceType].TakeUnit(m_TaskGoal); // (Try to) Restore store to how it was
            m_CantComplete = true;                              // and set cant complete flag
            return;                                             // Finally, exit function            
        }
        // Either inventory doesn't contain enough resources, or AddUnit() failed
        m_CantComplete = true; // Couldn't fulfill request so set cant complete flag
        return;                // and exit function        
    } // End of StoreResource() function

    // Handles the task of taking wood out of Woodstore
    private void TakeResource()
    {
        // Step 1) Handle movement, pathing, and timer
        if (!MovementChecker())  // Check movement and pathing on desired node
            return;              // If false returned, not ready so exit function
                                 // Step 3) Check if mesh has been setup for "animation"
        if (!m_InPosition)
        {
            if (m_Stores[(int)m_ResourceType].QueryAmount() < m_TaskGoal || // Query store to check if can NOT fulfil request
            m_Inventory.QuerySpace((int)m_ResourceType) < m_TaskGoal)   // Query inventory space for this resource, if either == false then...         
            {
                m_CantComplete = true;  // Set can't complete flag
                return;                 // and exit function
            }
            // Otherwise we're at the right node, so...
            transform.LookAt(new Vector3(m_Stores[(int)m_ResourceType].transform.position.x,        // Look at the resource position .x and .z
                        transform.position.y, m_Stores[(int)m_ResourceType].transform.position.z)); // (keeping y though to avoid weird rotations)
            m_Mesh.SetMeshAlternate((int)m_CurrentTask, m_Alternate);                                 // Set the mesh to alternate between default and chopping to resemble an animation 
            m_InPosition = true;                                                        // Set InPosition flag to true
            // Don't need to return, as we can carry on to setting up timers and start task
        }

        if (!TimerChecker())
            return;
     
        if(!m_Stores[(int)m_ResourceType].TakeUnit(m_TaskGoal)) // If withdrawal fails
        {
            m_CantComplete = true; // Set failure flag
            return;                // Exit function
        }
        if(!m_Inventory.AddAmount(m_TaskGoal, (int)m_ResourceType)) // If adding to inventory fails
        {
            m_Stores[(int)m_ResourceType].AddUnit(m_TaskGoal); // Restore wood to store
            m_CantComplete = true;  // Set failure flag
            return;                 // Exit function
        }
        // Otherwise, if we get to this point everything has been successful so..
        m_TaskCompleted = true; // Set failure flag
        return;                    // Exit function (in case of expansion beneath in future)
    } // End of TakeWood() function

    private void GatherResource()
    {
        // Step 1) Path to current target node
        if (!MovementChecker())   // Check movement and pathing on desired node
            return;                                 // If false returned then not ready, so exit function

        // Step 2) Check we have a resource
        if (m_CurrentResource == null)
        {
            m_CurrentResource = m_Resources[(int)m_ResourceType].GetTargetResource(); // If not, request a resource from the pointer
            if (m_CurrentResource == null)   // Check if the resource returned is null
            {   // If it is, then we have no resources available yet
                m_CantComplete = true;  // So set can't complete flag
                return;                     // And exit function
            }
            // Otherwise, we've got a valid resource, so...
            m_TargetNodeString = m_CurrentResource.GetNodeName(); // Reset our target to suit new resource           
            return;                                               // Need to start pathing though so exit function for MovementChecker to run
        }

        // Step 3) Check if mesh has been setup for "animation"
        if (!m_InPosition)
        {
            // Otherwise we're at the right node, so...
            transform.LookAt(new Vector3(m_CurrentResource.transform.position.x,        // Look at the resource position .x and .z
                        transform.position.y, m_CurrentResource.transform.position.z)); // (keeping y though to avoid weird rotations)
            m_Mesh.SetMeshAlternate((int)m_CurrentTask, m_Alternate);                                 // Set the mesh to alternate between default and chopping to resemble an animation 
            m_ActiveSound = m_Audio.Play(m_TaskSoundName, m_CurrentResource.gameObject);
            m_InPosition = true;                                                        // Set InPosition flag to true
            switch(m_ResourceType)
            {
                case Resource.Log:
                    m_Stats.SetHungerMultiplier(1.25f);
                    break;
                case Resource.Berry:
                    m_Stats.SetFatigueMultiplier(1.25f);
                    break;
                case Resource.Fish:
                    m_Stats.SetThirstMultiplier(1.25f);
                    break;
            }
            // Don't need to return, as we can carry on to setting up timers and start task
        }

        // Step 4) Setup or Update task timer
        if (TimerChecker()) // Handle timer, returns true when timer finished
        {
            // Step 5) Handle resouce unit transfer
            bool resourceRemaining = m_CurrentResource.Gathered(); // Call get unit method on resource; bool to track if it's depleted, will use bool later on
            if (!m_Inventory.AddAmount(1, (int)m_ResourceType))        // Add one unit to inventory - use (int) cast on Resource enum (more readable than a const int)
            {   // Something went wrong, either invalid parameters or inventory is full
                Debug.Log("ERROR: Couldn't add item '" + m_ResourceType.ToString() + "' to inventory in GatherResource()"); // Debug a warning
                m_CantComplete = true;  // Just cancel task, this may be changed to have a better handler of full inventory etc
                m_Audio.Stop(m_ActiveSound);
                m_ActiveSound = null;
                return;                     // Quit the function
            }
            else                            // Otherwise add to inventory was successful...
                m_TaskProgress += 1;        // so increase task progress tracker

            // Step 6) Check and handle task progress
            if (m_TaskProgress == m_TaskGoal)
            {                           // If task has now been completed
                m_TaskCompleted = true;  // Bool flag to tell Update() we're done 
                m_Audio.Stop(m_ActiveSound);
                m_ActiveSound = null;
                return;                 // Exit function
            }   // Otherwise if the task isn't complete...

            // Step 7) Handle resource validity
            if (!resourceRemaining)     // Check if resource is depleted (true = not depleted, false = depleted)
            {
                m_Audio.Stop(m_ActiveSound);
                m_ActiveSound = null;
                m_CurrentResource = m_Resources[(int)m_ResourceType].GetTargetResource();  // Try to get a new resource                
                if (m_CurrentResource == null)  // Check if resource is valid
                {
                    m_CantComplete = true;  // No more resources left, set can't complete to true
                    return;                 // Exit function
                }
                m_Timer = m_TaskTimes[(int)m_CurrentTask];    // Otherwise resource is valid, so reset timer,
                m_TargetNodeString = m_CurrentResource.GetNodeName(); // Set the required node variable
                m_InPosition = false;       // And set flag for NOT in position
                m_Mesh.ResetMesh();         // Return mesh to default
                m_Stats.ResetMultipliers();// Set multiplier back to false till back to gathering
                return;                     // Exit function
            } // Otherwise, resource not depleted and task not finished so...            
            m_Timer = m_TaskTimes[(int)m_CurrentTask];    // Reset timer
            return;                     // Exit function                                    
        }
    }

    private void Rest()
    {
        if (!MovementChecker())
            return;

        if (!m_InPosition)
        {
            transform.LookAt(new Vector3(m_Campfire.transform.position.x, transform.position.y, m_Campfire.transform.position.z));
            m_Mesh.SetMesh((int)m_CurrentTask);
            m_Stats.SetFatigueMultiplier(0.0f);
            m_InPosition = true;
        }

        if(TimerChecker())
        {
            m_Stats.Rested();
            m_Stats.SetFatigueMultiplier(1.0f);
            m_TaskCompleted = true;
        }
    }

    private void Sleep()
    {
        if (!MovementChecker())
            return;

        if (!m_InPosition)
        {
            transform.SetPositionAndRotation(new Vector3(-9.0f, 3.0f, -155.0f), Quaternion.identity);
            m_Mesh.SetMesh((int)m_CurrentTask);
            m_Stats.SetFatigueMultiplier(0.0f);
            m_InPosition = true;
            Time.timeScale = 3.0f;
        }

        if (TimerChecker())
        {
            m_Stats.Slept();
            m_TaskCompleted = true;
            Time.timeScale = 1.0f;
            m_PathFinder.SetCurrentNode("Tent");
            m_Stats.SetFatigueMultiplier(1.0f);
        }
    }

    private void Drink()
    {        
        if(!m_InPosition)
        {
            if (m_Inventory.QueryFlask() < 20.0f)
            {
                m_CantComplete = true;
                return;
            }

            m_Mesh.SetMesh((int)m_CurrentTask);
            m_InPosition = true;
        }
        if(TimerChecker())
        {
            m_Inventory.TakeWater(20);
            m_Stats.Drank();
            m_TaskCompleted = true;
        }
    }

    private void Eat()
    {
        if (!MovementChecker())
            return;

        if(!m_InPosition)
        {
            if((m_ResourceType != Resource.Berry && m_ResourceType != Resource.Fish) ||
                m_Inventory.QueryAmount((int)m_ResourceType) < 1)
            {
                m_CantComplete = true;
                return;
            }
            transform.LookAt(new Vector3(m_Campfire.transform.position.x, transform.position.y, m_Campfire.transform.position.z));
            m_Mesh.SetMesh((int)m_CurrentTask);
            m_InPosition = true;
        }

        if(TimerChecker())
        {
            m_Inventory.TakeAmount(1, (int)m_ResourceType);
            m_Stats.Ate(m_ResourceType);
            m_TaskCompleted = true;
        }
    }

    private void RefillFlask()
    {
        if (!MovementChecker())
            return;

        if(!m_InPosition)
        {
            if(m_Inventory.QueryFlask() == 100)
            {
                m_TaskCompleted = true;
                return;
            }
            //transform.LookAt(new Vector3(m_WaterWell.transform.position.x, transform.position.y, m_WaterWell.transform.position.z));
            m_Mesh.SetMesh((int)m_CurrentTask);
            m_ActiveSound = m_Audio.Play(m_TaskSoundName, gameObject);
            m_InPosition = true;
        }

        if(TimerChecker())
        {
            m_Inventory.FillFlask();
            m_Audio.Stop(m_ActiveSound);
            m_TaskCompleted = true;            
        }
    }

    public float GetTaskTime(Task task) { return m_TaskTimes[(int)task]; }
    public ResourceScript GetCurrentResource() { return m_CurrentResource; }
    public Task GetCurrentTaskID() { return m_CurrentTask; }
    public void InteruptTask()
    {
        m_CantComplete = true;
        if(m_ActiveSound != null)
        {
            m_Audio.Stop(m_ActiveSound);
            m_ActiveSound = null;
        }
    }
}
