using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarTwoCollider : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "TypeTwo")
        {
            FlockWithGroup script = other.GetComponent<FlockWithGroup>();
            GroupTag otherTag = other.GetComponent<GroupTag>();
            if (script.mCurrentBuddies.Contains(otherTag))
                script.mCurrentBuddies.Remove(otherTag);
            Destroy(other.gameObject);
            Destroy(gameObject);
        }
    }

}
