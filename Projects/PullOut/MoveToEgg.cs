using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveToEgg : MonoBehaviour {

    public float mSpeed;
    public float rotateSpeed;
    private GameObject Target;


    void Start() {
        Target = GameObject.Find("Egg");
        transform.LookAt(Target.transform);
    }

    // Update is called once per frame
    void Update () {
        float step = mSpeed * Time.deltaTime;
        Vector3 moveTowardsVect = Vector3.MoveTowards(transform.position, Target.transform.position, step);
        transform.position = moveTowardsVect;
        transform.RotateAround(Target.transform.position, Vector3.forward, rotateSpeed * Mathf.Sin(Time.time) * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(moveTowardsVect.normalized, new Vector3(1, 0, 0));

        if (Vector3.Distance(transform.position, Target.transform.position) <= 2.0f)
        {
            Camera.main.GetComponent<GameManagerScript>().SendMessage("EndGame");
        }
    }
}
