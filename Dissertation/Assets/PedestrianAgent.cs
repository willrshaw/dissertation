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
    public Vector3 startingPos;

    public DateTime period;
    void Start()
    {
        rBody = GetComponent<Rigidbody>();

        // Lock the rotation of the X and Z axis to keep capsule upright.
        rBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        //| RigidbodyConstraints.FreezePositionY

        //rBody.drag = 20f;
        startingPos = this.transform.localPosition;
    }

    public Transform Platform;
    public Transform Floor;
    

    

    public override void OnEpisodeBegin()
    {
        period = DateTime.Now;
        TextBox.text = "Platforms reached: " + goalCount.ToString();
        
        if (this.StepCount >= 4990 | resetAgent)
        {
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            //this.transform.localPosition = new Vector3(UnityEngine.Random.value * 10f + 12f, 1f, UnityEngine.Random.value * -10f - 18f);
            //this.transform.localPosition = GetRandomSpawnPos();
            this.transform.localPosition = startingPos;

            this.transform.rotation = new Quaternion(0f, 0f, 0f, 1f);
            resetAgent = false;
        }
        


        //Platform.localPosition = new Vector3(UnityEngine.Random.value * 43f - 21.5f,
        //                                 0f,
        //                               UnityEngine.Random.value * 43f - 21.5f);

        //Platform.localPosition = new Vector3(-18f, 0f, 18f);

        //Floor.localPosition = new Vector3(Floor.localPosition.x, -0.05f, Floor.localPosition.z);

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

        if (distanceToPlatform <= 5.0f)
        { 
            goalCount++;
            AddReward(10.0f);
            resetAgent = true;
            EndEpisode();
        }

        // time punishment and distance reward
        AddReward(1f / 1000f * distanceToPlatform - -0.01f);

        if (this.StepCount == (this.MaxStep - 1))
        {
            AddReward(-5.0f);
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
                dirToGo = this.transform.forward * pedestrianForwardSpeed;
                break;
            case 2:
                dirToGo = this.transform.forward * -pedestrianBackSpeed;
                break;
        }

        switch (rightAxis)
        {
            case 1:
                dirToGo = this.transform.right * pedestrianSideSpeed;
                break;
            case 2:
                dirToGo = this.transform.right * -pedestrianSideSpeed;
                break;
        }

        switch (rotateAxis)
        {
            case 1:
                rotateDir = this.transform.up * -1f;
                break;
            case 2:
                rotateDir = this.transform.up * 1f;
                break;
        }

        this.transform.Rotate(rotateDir, Time.deltaTime * rotationSpeed);
        rBody.AddForce(dirToGo, ForceMode.VelocityChange);
    }


    // https://gamedevacademy.org/unity-machine-learning-training-tutorial/
    // retrieved from source above
    public Vector3 GetRandomSpawnPos()
    {
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        int maxTries = 10;

        while (foundNewSpawnLocation == false)
        {
            var randomPosX = UnityEngine.Random.value * 10f + 12f;
            var randomPosZ = UnityEngine.Random.value * -10f - 18f;

            randomSpawnPos = new Vector3(randomPosX, 1f, randomPosZ);
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2f, 2f, 1f)) == false)
            {
                foundNewSpawnLocation = true;
            }

            // catch case to stop infinite runtime
            maxTries -= 1;
            if (maxTries <= 0)
            {
                foundNewSpawnLocation = true;
                randomSpawnPos = startingPos;
            }
        }
        return randomSpawnPos;
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
        this.MaxStep = 5000;
    }

}
