using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

// Simple Zombie Agent for Demo V1
[RequireComponent(typeof(Collider))]
public class ZombieAgent : Agent
{
    [Header("References")]
    public Transform playerTransform;    // set to Player cube in Inspector
    public EnemyData enemyData;          // set to an EnemyData ScriptableObject

    [Header("Runtime")]
    public int maxStepLimit = 500;

    int stepCount = 0;

    public override void Initialize()
    {
        // nothing special for now
    }

    public override void OnEpisodeBegin()
    {
        stepCount = 0;

        // Reset positions (simple randomization inside room bounds)
        float roomSize = 4.0f;
        transform.position = new Vector3(Random.Range(-roomSize, roomSize), transform.position.y, Random.Range(-roomSize, roomSize));
        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        if (playerTransform != null)
        {
            Vector3 playerPos;
            do
            {
                playerPos = new Vector3(Random.Range(-roomSize, roomSize), playerTransform.position.y, Random.Range(-roomSize, roomSize));
            } while (Vector3.Distance(playerPos, transform.position) < 1.0f);

            playerTransform.position = playerPos;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (playerTransform == null)
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(transform.forward.x);
            sensor.AddObservation(transform.forward.z);
            return;
        }

        // relative position to player (x,z), normalized by an assumed room size
        float roomSize = 5.0f;
        Vector3 rel = playerTransform.position - transform.position;
        sensor.AddObservation(rel.x / roomSize);
        sensor.AddObservation(rel.z / roomSize);

        // add agent forward vector (x,z)
        sensor.AddObservation(transform.forward.x);
        sensor.AddObservation(transform.forward.z);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        int action = actionBuffers.DiscreteActions[0];

        float move = 0f;
        float turn = 0f;
        if (enemyData != null)
        {
            // use moveSpeed/rotateSpeed from data if provided
        }

        switch (action)
        {
            case 0: move = 1f; break;  // forward
            case 1: move = -1f; break; // backward
            case 2: turn = -1f; break; // left
            case 3: turn = 1f; break;  // right
            default: break;
        }

        float moveSpeed = enemyData != null ? enemyData.moveSpeed : 2f;
        float rotateSpeed = enemyData != null ? enemyData.rotateSpeed : 120f;
        float successDistance = enemyData != null ? enemyData.successDistance : 0.8f;

        // Apply movement / rotation (transform-based for simplicity)
        transform.Rotate(0f, turn * rotateSpeed * Time.deltaTime, 0f);
        transform.position += transform.forward * move * moveSpeed * Time.deltaTime;

        // step penalty
        AddReward(-0.001f);

        // success check
        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= successDistance)
            {
                AddReward(1.0f);
                EndEpisode();
            }
        }

        stepCount++;
        if (stepCount >= maxStepLimit)
        {
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;
        d[0] = 0; // default no-op -> forward for convenience
        if (Input.GetKey(KeyCode.W)) d[0] = 0;
        if (Input.GetKey(KeyCode.S)) d[0] = 1;
        if (Input.GetKey(KeyCode.A)) d[0] = 2;
        if (Input.GetKey(KeyCode.D)) d[0] = 3;
    }

    private void OnTriggerEnter(Collider other)
    {
        // optional: if player has a collider and isTrigger, we can detect here
        if (other.transform == playerTransform)
        {
            AddReward(1.0f);
            EndEpisode();
        }
    }
}
