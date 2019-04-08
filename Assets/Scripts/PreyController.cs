﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreyController : MonoBehaviour {

    enum PreyState
    {
        roam,
        flee,
        flock,
        alert
    }
    private PreyState pState;
    public Vector3 NextRoamPos;

    private GameObject flockTarget;

    private Vector3 enemyDir;
    readonly float fleeTime = 3f;
    float fleeTimer;
    readonly float alertTime = 3f;
    float alertTimer;

    float worldLowerX, worldUpperX,
          worldLowerZ, worldUpperZ;
    
    private int raycastNum = 8;
    RaycastHit hit;

    private float speed = 0.16f;
    private float rotSpeed = 10f;

    Rigidbody rb;

	// Use this for initialization
	void Start ()
    {
        rb = this.GetComponent<Rigidbody>();

        pState = PreyState.roam;

        worldLowerX = -50f;
        worldUpperX = 50f;
        worldLowerZ = -50f;
        worldUpperZ = 50f;
	}
	
	// Update is called once per frame
	void Update ()
    {
        // hack to force y position
        transform.position = new Vector3(transform.position.x, 1.2f, transform.position.z);
        // hack to force x, z rotation
        transform.rotation = new Quaternion(0f, transform.rotation.y, 0f, transform.rotation.w);

        UpdateCastVisionRays();

        // todo: hearing, smell

        switch (pState)
        {
            case PreyState.roam:
                UpdateStateRoam();
                break;
            case PreyState.flee:
                UpdateStateFlee();
                break;
            case PreyState.flock:
                UpdateStateFlock();
                break;
            case PreyState.alert:
                UpdateStateAlert();
                break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Sound")
        {
            enemyDir = other.transform.position - this.transform.position;

            if (pState != PreyState.flee)
            {
                pState = PreyState.alert;
                alertTimer = 0f;
                //print(this + " state set to alert");
            }
        }

        if (other.tag == "PredatorScent")
        {
            ScentController sCont = other.GetComponent<ScentController>();

            enemyDir = sCont.OrigPosition - this.transform.position;

            if (pState != PreyState.flee)
            {
                pState = PreyState.alert;
                alertTimer = 0f;
                //print(this + " state set to alert");
            }
        }
    }

    private void UpdateStateAlert()
    {
        alertTimer += Time.deltaTime;

        Quaternion targetRotation = Quaternion.LookRotation(enemyDir);
        rb.transform.rotation = Quaternion.Slerp(rb.transform.rotation, targetRotation, Time.deltaTime * rotSpeed);

        rb.transform.Translate(Vector3.forward * speed * 0.5f);

        if (alertTimer >= alertTime)
        {
            pState = PreyState.roam;
            NextRoamPos = this.transform.position;
            //print(this + "state set to roam");
        }
    }

    private void UpdateStateFlock()
    {
        if (transform.position.x - NextRoamPos.x < 10 &&
            transform.position.z - NextRoamPos.z < 10)
        {
            //NextRoamPos = new Vector3(transform.position.x + UnityEngine.Random.Range(-50f, 50f), transform.position.y, transform.position.z + UnityEngine.Random.Range(-50f, 50f));

            NextRoamPos = flockTarget.transform.position;

            /*if (NextRoamPos.x > worldUpperX || NextRoamPos.x < worldLowerX)
            {
                NextRoamPos.x = UnityEngine.Random.Range(worldLowerX, worldUpperX);
            }
            if (NextRoamPos.z > worldUpperZ || NextRoamPos.z < worldLowerZ)
            {
                NextRoamPos.z = UnityEngine.Random.Range(worldLowerZ, worldUpperZ);
            }*/

            pState = PreyState.roam;
            ////print(this + " state set to roam");
        }

        rb.transform.Translate(Vector3.forward * speed * 1.5f);

        Quaternion targetRotation = Quaternion.LookRotation(NextRoamPos - rb.transform.position);
        rb.transform.rotation = Quaternion.Slerp(rb.transform.rotation, targetRotation, Time.deltaTime * rotSpeed);
    }

    private void UpdateStateFlee()
    {
        fleeTimer += Time.deltaTime;

        if (fleeTimer < fleeTime) // Flee opposite direction
        {
            Quaternion targetRotation = Quaternion.LookRotation(-enemyDir);
            rb.transform.rotation = Quaternion.Slerp(rb.transform.rotation, targetRotation, Time.deltaTime * rotSpeed);

            rb.transform.Translate(Vector3.forward * speed * 2);
        }
        else // switch to alert state
        {
            pState = PreyState.alert;
            alertTimer = 0f;
            ////print(this + "State set to alert");
        }
    }

    private void UpdateStateRoam()
    {
        if (transform.position.x - NextRoamPos.x < 5 &&
            transform.position.z - NextRoamPos.z < 5)
        {
            NextRoamPos = new Vector3(transform.position.x + UnityEngine.Random.Range(-50f, 50f), transform.position.y, transform.position.z + UnityEngine.Random.Range(-50f, 50f));
            
            // Correct for values outside of intended worldspace. Currently this will result in
            // the assignment of completely random values within the range.
            if (NextRoamPos.x > worldUpperX || NextRoamPos.x < worldLowerX)
            {
                NextRoamPos.x = UnityEngine.Random.Range(worldLowerX, worldUpperX);
            }
            if (NextRoamPos.z > worldUpperZ || NextRoamPos.z < worldLowerZ)
            {
                NextRoamPos.z = UnityEngine.Random.Range(worldLowerZ, worldUpperZ);
            }
        }

        rb.transform.Translate(Vector3.forward * speed);

        Quaternion targetRotation = Quaternion.LookRotation(NextRoamPos - rb.transform.position);
        rb.transform.rotation = Quaternion.Slerp(rb.transform.rotation, targetRotation, Time.deltaTime * rotSpeed);
    }

    private void UpdateCastVisionRays()
    {
        Vector3 localForward = transform.rotation * Vector3.forward;

        for (float i = 0; i < raycastNum; i++)
        {
            Vector3 offset = new Vector3(0f, 0f, i/15);

            Debug.DrawRay(transform.position, (localForward + offset) * 30f, Color.yellow);
            Debug.DrawRay(transform.position, (localForward - offset) * 30f, Color.yellow);

            int layermask = ~(1 << 9);

            // Cast a ray forward and on a slightly larger angle for each iteration. This creates a cone of rays.
            if (Physics.Raycast(transform.position, localForward + offset, out hit, 30f, layermask) ||
                Physics.Raycast(transform.position, localForward - offset, out hit, 30f, layermask))
            {

                if (hit.collider.tag == "Prey" && pState != PreyState.flee && pState != PreyState.flock)
                {
                    NextRoamPos = hit.transform.position;
                    flockTarget = hit.collider.gameObject;

                    pState = PreyState.flock;
                    //print(this + "state set to flock");

                    // debug
                    Debug.DrawRay(transform.position, (localForward + offset) * 30f, Color.blue, 0.5f);
                    Debug.DrawRay(transform.position, (localForward - offset) * 30f, Color.blue, 0.5f);
                }

                if (hit.collider.tag == "Player" || hit.collider.tag == "Predator")
                {
                    pState = PreyState.flee;
                    enemyDir = hit.transform.position - transform.position;
                    fleeTimer = 0f;
                    //print(this + "state set to flee");

                    // debug
                    Debug.DrawRay(transform.position, (localForward + offset) * 30f, Color.red, 0.5f);
                    Debug.DrawRay(transform.position, (localForward - offset) * 30f, Color.red, 0.5f);
                }
            }
        }
    }
}