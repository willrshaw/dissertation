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
        TextBox.text = "Platforms reached: " + goalCount.ToString();
        if (Math.Abs(this.transform.localPosition.x) > 25f | Math.Abs(this.transform.localPosition.z) > 25f)
        {
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(0, 1f, 0);
            this.transform.rotation = new Quaternion(0f, 1f, 0f, 1f);
        }

        Platform.localPosition = new Vector3(UnityEngine.Random.value * 45f - 22.5f,
                                           0f,
                                           UnityEngine.Random.value * 45f - 22.5f);
        Floor.localPosition = new Vector3(Floor.localPosition.x, -0.05f, Floor.localPosition.z);

    }



    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        sensor.AddObservation(Platform.transform.position);
        sensor.AddObservation(this.transform.localPosition);
        //agent rotations
        sensor.AddObservation(this.transform.rotation);

        sensor.AddObservation(Vector3.Distance(this.transform.localPosition, Platform.localPosition));

        // Agent velocity
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);

    }

    public float pedestrianForwardSpeed = 5;
    public float pedestrianBackSpeed = 1f;
    public float pedestrianSideSpeed = 1;
    public float rotationSpeed = 5;

    public override void OnActionReceived(float[] vectorAction)
    {
        Vector3 controlSignal = Vector3.zero;
        controlSignal = vectorAction[0] * pedestrianForwardSpeed * 1f * transform.forward;
        controlSignal += vectorAction[1] * pedestrianBackSpeed * -1f * transform.forward;
        controlSignal += vectorAction[2] * pedestrianSideSpeed * 1f * transform.right;
        controlSignal += vectorAction[3] * pedestrianSideSpeed * -1f * transform.right;

        rBody.AddForce(controlSignal , ForceMode.VelocityChange);

        // rotate body
        float rotationSignal = rotationSpeed * vectorAction[4] - rotationSpeed * vectorAction[5];
        transform.RotateAround(this.transform.localPosition, Vector3.up, rotationSignal * Time.deltaTime);
        

        float distanceToPlatform = Vector3.Distance(this.transform.localPosition, Platform.localPosition);

        if (distanceToPlatform < 2.0f)
        { 
            goalCount++;
            SetReward(1.0f);
            EndEpisode();
        }

        if (Math.Abs(this.transform.localPosition.x) > 25f | Math.Abs(this.transform.localPosition.z) > 25f)
        {
            SetReward(-0.1f);
            EndEpisode();
        }
    }

    public override void Heuristic(float[] actionsOut)
    {
        actionsOut[0] = Input.GetAxis("Horizontal");
        actionsOut[1] = Input.GetAxis("Vertical");
    }


}
