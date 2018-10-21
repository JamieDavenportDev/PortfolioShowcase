using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MagnetizedByPlayer : MonoBehaviour
{
    public enum Type { Attract, Repel }

    [SerializeField]
    private float RepelForce = 1000.0f;

    [SerializeField]
    private float MinimumDistance = 1.0f;

    [SerializeField]
    private Type MagnetizeType = Type.Repel;

    [SerializeField]
    private float maxSpeed = 25.0f;

    private Player mPlayer;
    private Rigidbody mBody;
    private bool locked = false;

    void Awake()
    {
        mPlayer = FindObjectOfType<Player>();
        mBody = GetComponent<Rigidbody>();
        if(MagnetizeType == Type.Repel)
        {
            mBody.AddForce(maxSpeed * 5.0f * RepelForce * Time.deltaTime, maxSpeed * 5.0f * RepelForce * Time.deltaTime, maxSpeed * 5.0f * RepelForce * Time.deltaTime);
        }
    }

	void Update()
    {
        if (MagnetizeType == Type.Repel && mBody.velocity == Vector3.zero)
        {
            mBody.AddForce(maxSpeed * Time.deltaTime, maxSpeed * Time.deltaTime, maxSpeed * Time.deltaTime);
        }
        if ( mPlayer != null && !locked)
        {
            Vector3 difference = MagnetizeType == Type.Repel ? transform.position - mPlayer.transform.position : mPlayer.transform.position - transform.position;
            if( difference.magnitude <= MinimumDistance )
            {
                mBody.AddForce(difference * RepelForce * Time.deltaTime);
                if (mBody.velocity.magnitude > maxSpeed)
                {
                    mBody.velocity = mBody.velocity.normalized * maxSpeed;
                }
            }
        }		
	}

    void LockLocation()
    {
        locked = true;
        mBody.velocity = Vector3.zero;
    }
}
