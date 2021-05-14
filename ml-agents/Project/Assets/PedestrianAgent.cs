using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;


public class PedestrianAgent : Agent
{
    //public TextMesh TextBox;
    public int goalCount = 0;
    Rigidbody rBody;
    public bool resetAgent = false;
    public Vector3 startingPos;
    public float tempDist = 999999999999f;
    public Vector3 locVel;
    public DateTime period;
    public float personalRadius = 1.5f;
    public float unwantedMovementTracker = 0f;
    public override void Initialize()
    {
        this.MaxStep = 5000;
        rBody = GetComponent<Rigidbody>();

        // Lock the rotation of the X and Z axis to keep capsule upright.
        rBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
        //| RigidbodyConstraints.FreezePositionY

        //rBody.drag = 20f;
        startingPos = this.transform.localPosition;

        //TextBox = GameObject.Find("TextThing").GetComponent<TextMesh>();
    }

    public Transform Platform;
    public Transform Floor;
    

    

    public override void OnEpisodeBegin()
    {
        period = DateTime.Now;
        //TextBox.text = "Platforms reached: " + goalCount.ToString();

        
        
        if (this.StepCount >= 4990 | resetAgent)
        {
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(UnityEngine.Random.value * 10f + 12f, 1f, UnityEngine.Random.value * -10f - 18f);
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

        //sensor.AddObservation(Vector3.Distance(this.transform.localPosition, Platform.localPosition));

        // Agent velocity
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);
        sensor.AddObservation(rBody.angularVelocity.x);
        sensor.AddObservation(rBody.angularVelocity.y);
        sensor.AddObservation(rBody.angularVelocity.z);
        sensor.AddObservation(locVel);

        //crowd observations
        sensor.AddObservation(CrowdDirection(personalRadius));
        sensor.AddObservation(CrowdDensity(personalRadius));


        //Debug.Log(sensor);
    }

    public Vector3 CrowdDirection(float radius)
    {
        Vector3 direction = Vector3.zero;
        Vector3 centre = this.transform.localPosition;
        Collider[] colliders = Physics.OverlapSphere(centre, radius);
        foreach (Collider col in colliders)
        {
            if (col.attachedRigidbody)
            {
                direction += col.attachedRigidbody.velocity;
            }
        }

        return Vector3.Normalize(direction);
    }

    public int CrowdDensity(float radius)
    {
        Vector3 centre = this.transform.localPosition;
        Collider[] colliders = Physics.OverlapSphere(centre, radius);
        return colliders.Length;
    }


    public override void OnActionReceived(ActionBuffers actionBuffers)
    {

        this.transform.rotation = new Quaternion (0f, this.transform.rotation.y, 0f, this.transform.rotation.w);

        float distanceToPlatform = Vector3.Distance(this.transform.localPosition, Platform.localPosition);

        if (distanceToPlatform <= 5.0f)
        { 
            goalCount++;
            AddReward(50.0f);
            resetAgent = true;
            EndEpisode();
        }

        // time punishment and distance reward
        //AddReward(1f / 100000000f * distanceToPlatform - -1f);

        AddReward(-0.01f);
        if (this.StepCount >= (this.MaxStep - 10))
        {
            AddReward(-50.0f);
            resetAgent = true;
            EndEpisode();
        }

        if ((tempDist - distanceToPlatform) / Time.deltaTime > 0)
        {
            AddReward(0.05f);
        }

        if ((tempDist - distanceToPlatform) / Time.deltaTime < 0)
        {
            AddReward(-0.05f);
        }


        tempDist = distanceToPlatform;
        if (Math.Abs(this.transform.localPosition.x) >= 40f | Math.Abs(this.transform.localPosition.z) >= 90f | Math.Abs(this.transform.localPosition.y) >= 2.5f)
        {
            resetAgent = true;
        }

        locVel = transform.InverseTransformDirection(rBody.velocity);

        float turbulenceRating = Vector3.Dot(CrowdDirection(personalRadius),
                                 Vector3.Normalize(this.rBody.velocity));
        AddReward(0.01f * turbulenceRating);

        //time penalty
        AddReward(-0.05f);

        float movementPunisherConstant = -0.1f;

        AddReward(unwantedMovementTracker * movementPunisherConstant);
        unwantedMovementTracker = 0f;
        MoveAgent(actionBuffers.DiscreteActions);
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var forwardAxis = act[0];
        var rightAxis = act[1];
        var rotateAxis = act[2];

        float pedestrianForwardSpeed = 0.8f;
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
                unwantedMovementTracker += 0.05f;
                break;
        }

        switch (rightAxis)
        {
            case 1:
                dirToGo = transform.right * pedestrianSideSpeed;
                unwantedMovementTracker += 0.01f;
                break;
            case 2:
                dirToGo = transform.right * -pedestrianSideSpeed;
                unwantedMovementTracker += 0.01f;
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

        //Debug.Log(dirToGo);
        transform.Rotate(rotateDir, Time.deltaTime * rotationSpeed);
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


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        //forward
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        //rotate
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[2] = 2;
        }
        //right
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[1] = 2;
        }
    }

   

}
