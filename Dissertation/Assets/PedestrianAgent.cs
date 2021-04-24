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
        rBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
    }

    public Transform Platform;
    public Transform Floor;
    

    

    public override void OnEpisodeBegin()
    {
        period = DateTime.Now;
        TextBox.text = "Platforms reached: " + goalCount.ToString();
        if (Math.Abs(this.transform.localPosition.x) > 25f | Math.Abs(this.transform.localPosition.z) > 25f | resetAgent | this.StepCount >= 4990)
        {
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(0, 1f, 0);
            this.transform.rotation = new Quaternion(0f, 1f, 0f, 1f);
            resetAgent = false;
        }


        Platform.localPosition = new Vector3(UnityEngine.Random.value * 43f - 21.5f,
                                           0f,
                                           UnityEngine.Random.value * 43f - 21.5f);
        Floor.localPosition = new Vector3(Floor.localPosition.x, -0.05f, Floor.localPosition.z);

    }



    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        sensor.AddObservation(Platform.transform.position.x);
        sensor.AddObservation(Platform.transform.position.y);
        sensor.AddObservation(Platform.transform.position.z);
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

    public float pedestrianForwardSpeed = 20f;
    public float pedestrianBackSpeed = 10f;
    public float pedestrianSideSpeed = 2f;
    public float rotationSpeed = 20f;

    public override void OnActionReceived(float[] vectorAction)
        // todo ADD FRICTION, RESITANCE AND CAPS ON VELOCITIES AND ANGULAR VELOCITIES.
    {
        Vector3 controlSignal = Vector3.zero;
        controlSignal = vectorAction[0] * pedestrianForwardSpeed * 1f * this.transform.forward;
        controlSignal += vectorAction[1] * pedestrianBackSpeed * -1f * this.transform.forward;
        controlSignal += vectorAction[2] * pedestrianSideSpeed * 1f * this.transform.right;
        controlSignal += vectorAction[3] * pedestrianSideSpeed * -1f * this.transform.right;


        float rotationSignal = rotationSpeed * vectorAction[4];
        rotationSignal -= rotationSpeed * vectorAction[5];
        transform.RotateAround(this.transform.localPosition, Vector3.up, rotationSignal);
        rBody.AddForce(controlSignal);
        

        float distanceToPlatform = Vector3.Distance(this.transform.localPosition, Platform.localPosition);

        if (distanceToPlatform < 3.54f)
        { 
            goalCount++;
            AddReward(10.0f);
            resetAgent = true;
            EndEpisode();
        }

        if (Math.Abs(this.transform.localPosition.x) > 25f | Math.Abs(this.transform.localPosition.z) > 25f)
        {
            AddReward(-0.1f);
            resetAgent = true;
            EndEpisode();
        }

        AddReward(-0.001f);
    }

    public override void Heuristic(float[] actionsOut)
    {
        //actionsOut[0] = Input.GetAxis("Horizontal");
        //actionsOut[1] = Input.GetAxis("Vertical");

        if (Input.GetKey(KeyCode.W))
        {
            actionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            actionsOut[1] = 1;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            actionsOut[2] = 1;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            actionsOut[3] = 1;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            actionsOut[4] = 1;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            actionsOut[5] = 1;
        } else {}
    }

    public override void Initialize()
    {
        this.MaxStep = 5000;
    }

}
