using System.Collections.Generic;
using UnityEngine;

public class PathFinding : MonoBehaviour {

    // ***********************************
    // *** Public/Serialized variables ***
    public GameObject m_VisualPF;   // Prefab for path highlighting
    public Path_Node m_StartNode;   // Character starting position

    // *************************
    // *** Private variables ***
    private Dictionary<string, Path_Node> AllNodes;             // Lookup for all nodes, using uniqueName as key
    private Path_Node m_CurrentNode;                            // Current node character is at
    private Queue<Path_Node> m_Path = new Queue<Path_Node>();   // Queue to store final path result
    private SimpleMovement m_Character;                         // Access to character movement script

    private void Awake()
    {
        AllNodes = new Dictionary<string, Path_Node>();     // Creating Lookup to be filled
    }

    void Start () {
        m_Character = GetComponent<SimpleMovement>();       // Retrieving and setting movement script
        Path_Node[] Nodes = FindObjectsOfType<Path_Node>(); // Retrieving all Nodes on map        

        foreach (Path_Node node in Nodes)
        {
            if(!AllNodes.ContainsKey(node.uniqueID))
                AllNodes.Add(node.uniqueID, node);
        }  

        // Setting current node and characters position
        m_CurrentNode = m_StartNode;
        transform.position = m_StartNode.transform.position;
    }

    // Adds new dynamically made nodes to known nodes
    public void AddNode(Path_Node newNode)
    {
        if (!AllNodes.ContainsKey(newNode.uniqueID))
            AllNodes.Add(newNode.uniqueID, newNode);
    }

    // Sets current node to given Path_Node or given string via function overloading
    public void SetCurrentNode(Path_Node node) { m_CurrentNode = node; }
    public void SetCurrentNode(string name) { m_CurrentNode = GetNode(name); }

    // Returns current node's uniqueID as string
    public string GetCurrentNodeName() { return m_CurrentNode.uniqueID; }
    public Path_Node GetCurrentNode() { return m_CurrentNode; }
    
    // Returns node using given string name
    public Path_Node GetNode(string name)  
    {
        Path_Node temp;
        AllNodes.TryGetValue(name, out temp);
        return temp;
    }

    // Looks through all descendants of array given for a target node
    // Uses recursion to save long nested ifs or preset amount of "generations"
    private bool CheckChildren(Path_Node[] Children, Path_Node target)
    {
        foreach (Path_Node child in Children)
        {
            // Checks if current node is target, if not then checks it's children
            // When target is found it is added to Queue, and every node that led to it
            // is added aswell due to recursion stack
            if (child == target || CheckChildren(child.FirstGen, target))
            {
                m_Path.Enqueue(child);
                return true;
            }
        }
        // Couldn't find target in children, so return false
        return false;
    }

    // Another recursive method, except this one gets given the starting node. This is so I can prevent
    // checking the same node's children redundantly 
    private bool CheckParents(Path_Node start, Path_Node target)
    {        
        if (start.ParentNode == null)   // Check if there even is a parent node
            return false;   // If not return false, we've reached end of search now        
        else if (start.ParentNode == target)    // If there is, check if it is the target
        {            
            m_Path.Enqueue(start.ParentNode);   // If it is, add it to the Queue....
            return true;                        // and return true to inform previous stack calls
        }
        else   // Otherwise, we haven't found target yet...
        {
            // so search this nodes children...
            foreach (Path_Node child in start.ParentNode.FirstGen)
            {
                if (child != start) // Ignore the node if it called this function
                {
                    // Check if target, if not check it's children
                    if (child == target || CheckChildren(child.FirstGen, target))
                    {
                        m_Path.Enqueue(child);              // Target found either on this node or in children, so add this node to Queue
                        m_Path.Enqueue(start.ParentNode);   // Then add the parent of this node
                        return true;                        // Return true for previous stack calls
                    }
                }
            }
            // If we get to this point, we haven't found target in all descendants
            // so go up a level by passing the node we've just searched 
            // (explained why this node and not it's parent earlier on)
            if (CheckParents(start.ParentNode, target))
            {
                m_Path.Enqueue(start.ParentNode);   // Node found, so this node leads to goal, add it to Queue
                return true;                        // Return success
            }
            else
                return false; // All nodes searched, return fail
        }
    }

    // Creates lines of prefabricated objects to show path character is going to take
    // Prefabs destroy themselves when character get near enough
    private void DrawPath()
    {        
        Vector3[] posOnPath = new Vector3[m_Path.Count + 1];  // Create a new array of positions the size of the path plus one for character
        posOnPath[0] = m_Character.transform.position;        // Set first position to character position
        float dist = 0.0f;                                    // Setup a total distance of path variable
        int i = 1;                                            // Setup a counter for indexing
        foreach(Path_Node node in m_Path)                     // Loop through all path nodes
        {
            posOnPath[i] = node.transform.position;                 // Add the nodes position to the array
            dist += Vector3.Distance(posOnPath[i-1], posOnPath[i]); // Add onto the total distance of path
            i++;                                                    // Increment counter
        }
        float distAlongPath = 0.0f;
        for(int k = 0; k < posOnPath.Length - 1; k++)
        {
            Vector3 node1 = posOnPath[k];
            Vector3 node2 = posOnPath[k + 1];
            int amountToSpawn = Mathf.CeilToInt(Vector3.Distance(node1, node2) / 5.0f) - 1;     // Amount needed based on spacing 5.0f, amount-=1 to prevent overshooting
            Vector3 dirSpawn = Vector3.Normalize(node2 - node1);                                // Normalise vector between points to length 1
            Vector3 previousPoint = posOnPath[k];
            // For loop since dynamic amount
            for (int j = 0; j <= amountToSpawn; j++)
            {
                Vector3 SpawnPos = (node1 + (dirSpawn * (j * 5.0f))) + (Vector3.up * 5.0f);             // Calculate a spawn position using first points location, the normalised vector, and set it's y to 5.0f
                distAlongPath += Vector3.Distance(previousPoint, SpawnPos);                             // Increment distance along path variable
                GameObject newPoint = Instantiate(m_VisualPF, SpawnPos, Quaternion.Euler(dirSpawn));    // Instantiate a new prefab with pointer to it, vector to set it's look rotation incase I change model to arrow, or make it move a certain way
                PathingVisualiser script = newPoint.GetComponent<PathingVisualiser>();                  // Couldn't cast straight to it's script type when instantiating so get it here
                script.Character = transform;                                                           // Pointer to character distance checking
                script.m_DebugMode = m_Character.GetDebugMode();                                        // Setting debug bool to be consistent with rest of game, this controls whether it's visible or not
                script.SetPathPercent(distAlongPath / dist);                                            // Set the visualisers colour using progress along path for it's lerp use
            }
        }
        
    }

    // Due to recursive methods, queue ends up being backwards...
    // so this function reverses it and calls the DrawPath function when done
    private void PreparePath()
    {        
        Path_Node[] temp = m_Path.ToArray();        // Copy Queue to an Array for temporary storage and easy iteration over
        m_Path.Clear();                             // Clear the Queue ready for correct order
        for (int i = temp.Length - 1; i >= 0; i--)  // Start at end of array and work backwards
        {
            m_Path.Enqueue(temp[i]);                                // Add the node to the Queue
        }    
        DrawPath();        // Now call the DrawPath method as the path is in correct order
    }                                                                             

    // This function starts the path search, all it needs is the target node unique ID, and returns a complete Queue
    public Queue<Path_Node> PathSearch(string TargetID, bool forWolf = false)
    {
        Path_Node targetNode;  // Need somewhere to output target node as Path_Node type
        m_Path.Clear();        // Reset path ready for use
        if (AllNodes.TryGetValue(TargetID, out targetNode)) // Attempt to find the Path_Node in all known nodes
        {
            if (m_CurrentNode != targetNode)    // Check we're not already at the target node
            {          
                if (CheckChildren(m_CurrentNode.FirstGen, targetNode) // First check children recursively, checks all descendants not just first level
                    || CheckParents(m_CurrentNode, targetNode))       // Then check parents recursively, also checks each parent's children
                {
                    if(!forWolf)
                        PreparePath();  // Path has been found, reverse it to correct order and draw the debug path
                    return m_Path;  // return the path
                }
                else    // Path hasn't been found, so...
                {
                    m_Path.Enqueue(targetNode);                                     // Cheat and just set target as only node on path
                    if(!forWolf)
                        DrawPath();    // Draw a line to it
                    return m_Path;  // And return it
                    // This /should/ never be called if the nodes are set up correctly
                }
            }
        }
        // Path_Node not on our list of known nodes or we're already at said node, so return null and let call handle it
        return null;    
    }
}
