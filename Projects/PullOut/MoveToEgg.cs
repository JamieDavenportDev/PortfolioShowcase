using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveToEgg : MonoBehaviour {

    // The speed for moving towards the egg
    public float mSpeed;
    // Rotation speed to give swimming appearance
    public float rotateSpeed;
    // Points to the Egg
    private GameObject Target;

    void Start() {
        Target = GameObject.Find("Egg"); // Setup target to egg
        transform.LookAt(Target.transform); // Have sperm look towards egg
    }

    // Update is called once per frame
    void Update () {
        float step = mSpeed * Time.deltaTime;   // Speed scaled to frame time
        // Calculate and move towards the egg
        Vector3 moveTowardsVect = Vector3.MoveTowards(transform.position, Target.transform.position, step);
        transform.position = moveTowardsVect;
        // Handle rotation for swim effect
        transform.RotateAround(Target.transform.position, Vector3.forward, rotateSpeed * Mathf.Sin(Time.time) * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(moveTowardsVect.normalized, new Vector3(1, 0, 0));

        // Check if close enough to egg to end
        if (Vector3.Distance(transform.position, Target.transform.position) <= 2.0f)
        {
            // If so, call EndGame function in GameManager
            Camera.main.GetComponent<GameManagerScript>().SendMessage("EndGame");
        }
    }
}
