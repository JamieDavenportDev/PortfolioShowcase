using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FlockWithGroup : MonoBehaviour
{
    [SerializeField]
    private GroupTag.Group GroupCode;

    [SerializeField]
    private float Speed;

    [SerializeField]
    private float BuddyDistance = 100.0f;

    [SerializeField]
    private float AvoidDistance = 1.0f;

    [SerializeField]
    private float CheckForBuddiesInterval = 10.0f;

    [SerializeField]
    private float maxSpeed;

    [SerializeField]
    private float flockSpeed;

    [SerializeField]
    private float resistSpeed;

    public List<GroupTag> mCurrentBuddies;
    private Rigidbody mBody;
    private float mCountDownToCheck;
    private GameManager game;

    void Awake()
    {
        mCurrentBuddies = new List<GroupTag>();
        mBody = GetComponent<Rigidbody>();
        mCountDownToCheck = 0.0f;
        game = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        mCountDownToCheck -= Time.deltaTime;
        if (mCountDownToCheck <= 0.0f)
        {
            UpdateBuddyList();
            mCountDownToCheck = CheckForBuddiesInterval;
        }

        FlockWithBuddies();
    }
    

    void DeleteSelf()
    {
        Destroy(gameObject);
    }

    private void UpdateBuddyList()
    {
        GroupTag[] individuals = FindObjectsOfType<GroupTag>();

        for(int i = 0; i < mCurrentBuddies.Count; i++)  // Checking for null list members
        {                                               // As it is easier than checking every list for a reference to an object on it's destruction
            if (mCurrentBuddies[i] == null)
                mCurrentBuddies.Remove(mCurrentBuddies[i]);
        }

        for (int count = 0; count < individuals.Length; ++count)
        {
            if (individuals[count].gameObject != gameObject && individuals[count].Affiliation == GroupCode )
            {
                Vector3 difference = individuals[count].transform.position - transform.position;
                if (difference.magnitude <= BuddyDistance)
                {
                    if (!mCurrentBuddies.Contains(individuals[count]))
                    {
                        mCurrentBuddies.Add(individuals[count]);
                    }
                }
                else if (mCurrentBuddies.Contains(individuals[count]))
                {
                    mCurrentBuddies.Remove(individuals[count]);
                }
            }
        }

        if(mCurrentBuddies.Count >= 4)
        {
            Vector3 location = new Vector3();
            location = mCurrentBuddies[0].transform.position;
            GameObject[] objArray = new GameObject[5];
            int count = 0;
            foreach(var star in mCurrentBuddies)
            {
                objArray[count] = star.gameObject;
                count++;
            }
            mCurrentBuddies.Clear();

            for (int i = 0; i < 5; i++)
            {
                Destroy(objArray[i]);
            }

            game.Instance.SendMessage("SpawnMegaStar", location);
            Destroy(gameObject);
        }
    }

    private void FlockWithBuddies()
    {
        if (mCurrentBuddies.Count > 0)
        {
            Vector3 align = Vector3.zero;
            Vector3 cohesion = Vector3.zero; 
            Vector3 avoid = Vector3.zero;
            
            for (int count = 0; count < mCurrentBuddies.Count; ++count)
            {
                if (mCurrentBuddies[count] == null) // When an object gets deleted due to star collision or grouping
                {                                   // It would be overly complex to remove it from every list that references it
                    mCurrentBuddies.Remove(mCurrentBuddies[count]); // So instead whenever a group tag is checked, add a check first to see if it's valid
                    continue;   // If not, remove it and continue back to the next loop
                }
                Rigidbody body = mCurrentBuddies[count].GetComponent<Rigidbody>();
                align += body.velocity;
                cohesion += mCurrentBuddies[count].transform.position;
                if ( ( mCurrentBuddies[count].transform.position - transform.position ).magnitude < AvoidDistance)
                {
                    avoid += mCurrentBuddies[count].transform.position;
                }
            }

            align /= mCurrentBuddies.Count;
            cohesion /= mCurrentBuddies.Count;
            avoid /= mCurrentBuddies.Count;

            align.Normalize();
            cohesion = cohesion - transform.position;
            cohesion.Normalize();
            avoid = transform.position - avoid;
            avoid.Normalize();

            mBody.AddForce(( align * Speed ) + ( cohesion * flockSpeed ) + (avoid * resistSpeed) * Time.deltaTime);
            if (mBody.velocity.magnitude > maxSpeed)
            {
                mBody.velocity = mBody.velocity.normalized * maxSpeed;
            }
        }
    }
}
