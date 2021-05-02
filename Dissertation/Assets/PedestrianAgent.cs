using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;


public class PedestrianAgent : Agent
{
    public Text TextBox;
    public int goalCount = 0;
    Rigidbody rBody;
    public bool resetAgent = false;

    public DateTime period;
    void Start()
    {
        rBody = GetComponent<Rigidbody>();

        // Lock the rotation of the X and Z axis to keep capsule upright.
        rBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        //| RigidbodyConstraints.FreezePositionY

        //rBody.drag = 20f;
    }

    public Transform Platform;
    public Transform Floor;
    

    

    public override void OnEpisodeBegin()
    {
        period = DateTime.Now;
        TextBox.text = "Platforms reached: " + goalCount.ToString();
        if (Math.Abs(this.transform.localPosition.x) > 25f | Math.Abs(this.transform.localPosition.z) > 25f | resetAgent | this.StepCount >= 4990 | resetAgent)
        {
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(UnityEngine.Random.value * 10f + 12f, 1f, UnityEngine.Random.value * -10f - 18f);
            this.transform.rotation = new Quaternion(0f, 1f, 0f, 1f);
            resetAgent = false;
        }


        //Platform.localPosition = new Vector3(UnityEngine.Random.value * 43f - 21.5f,
        //                                 0f,
        //                               UnityEngine.Random.value * 43f - 21.5f);

        Platform.localPosition = new Vector3(-18f,
                                           0f,
                                           18f);

        Floor.localPosition = new Vector3(Floor.localPosition.x, -0.05f, Floor.localPosition.z);

    }



    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        sensor.AddObservation(Platform.transform.localPosition.x);
        sensor.AddObservation(Platform.transform.localPosition.y);
        sensor.AddObservation(Platform.transform.localPosition.z);
        sensor.AddObservation(this.transform.localPosition.x);
        sensor.AddObservation(this.transform.localPosition.y);
        sensor.AddObservation(this.transform.localPosition.z);
        //agent rotations
        sensor.AddObservation(this.transform.rotation.x);
        sensor.AddObservation(this.transform.rotation.y);
        sensor.AddObservation(this.transform.rotation.z);
        sensor.AddObservation(this.transform.rotation.w);

        sensor.AddObservation(Vector3.Distance(this.transform.localPosition, Platform.localPosition));

        // Agent velocity
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);
        sensor.AddObservation(rBody.angularVelocity.x);
        sensor.AddObservation(rBody.angularVelocity.y);
        sensor.AddObservation(rBody.angularVelocity.z);


    }

    
    public override void OnActionReceived(float[] vectorAction)
    { 
        

        float distanceToPlatform = Vector3.Distance(this.transform.localPosition, Platform.localPosition);

        if (distanceToPlatform < 3.54f)
        { 
            goalCount++;
            SetReward(1.0f);
            resetAgent = true;
            EndEpisode();
        }

        if (Math.Abs(this.transform.localPosition.x) > 25f | Math.Abs(this.transform.localPosition.z) > 25f)
        {
            AddReward(-0.1f);
            resetAgent = true;
            EndEpisode();
        }

        // time punishment
        AddReward(-0.01f);

        AddReward(1f / 1000f * distanceToPlatform);

        if (this.StepCount == (this.MaxStep - 1))
        {
            AddReward(-1.0f);
            resetAgent = true;
            EndEpisode();
        }
        MoveAgent(vectorAction);
    }

    public void MoveAgent(float[] act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var forwardAxis = (int)act[0];
        var rightAxis = (int)act[1];
        var rotateAxis = (int)act[2];

        float pedestrianForwardSpeed = 1f;
        float pedestrianBackSpeed = 0.5f;
        float pedestrianSideSpeed = 0.1f;
        float rotationSpeed = 100f;

        switch (forwardAxis)
        {
            case 1:
                dirToGo = transform.forward * pedestrianForwardSpeed;
                break;
            case 2:
                dirToGo = transform.forward * -pedestrianBackSpeed;
                break;
        }

        switch (rightAxis)
        {
            case 1:
                dirToGo = transform.right * pedestrianSideSpeed;
                break;
            case 2:
                dirToGo = transform.right * -pedestrianSideSpeed;
                break;
        }

        switch (rotateAxis)
        {
            case 1:
                rotateDir = transform.up * -1f;
                break;
            case 2:
                rotateDir = transform.up * 1f;
                break;
        }

        transform.Rotate(rotateDir, Time.deltaTime * rotationSpeed);
        rBody.AddForce(dirToGo, ForceMode.VelocityChange);
    }

    public override void Heuristic(float[] actionsOut)
    {
        //forward
        if (Input.GetKey(KeyCode.W))
        {
            actionsOut[0] = 1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            actionsOut[0] = 2f;
        }
        //rotate
        if (Input.GetKey(KeyCode.A))
        {
            actionsOut[2] = 1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            actionsOut[2] = 2f;
        }
        //right
        if (Input.GetKey(KeyCode.E))
        {
            actionsOut[1] = 1f;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            actionsOut[1] = 2f;
        }
    }

    public override void Initialize()
    {
        this.MaxStep = 2000;
    }

}
