using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

/// <summary>
/// ZombieAgent cho Demo V1.
/// - Action (Discrete, 4): 0=Forward, 1=Backward, 2=Left(strafe), 3=Right(strafe)
/// - Observations: relative position to player (x,z) normalized, agent forward (x,z) => 4 floats
/// - Movement: Rigidbody-based MovePosition (strafe + forward/back)
/// - Collision: OnTriggerEnter / OnCollisionEnter with Player để AddReward(1.0f) và EndEpisode()
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class ZombieAgent : Agent
{
    [Header("References")]
    public Transform playerTransform;    // assign Player GameObject's transform in Inspector
    public EnemyData enemyData;          // ScriptableObject (optional) assigned in Inspector

    [Header("Movement")]
    public float defaultMoveSpeed = 2f;
    public float successDistance = 0.8f;
    public int maxStepLimit = 500;

    [Header("Reward shaping")]
    [Tooltip("Scale for distance improvement reward (positive when getting closer).")]
    public float distanceRewardScale = 0.1f;
    [Tooltip("Clamp per-step shaped reward to avoid spikes.")]
    public float distanceRewardMin = -0.02f;
    public float distanceRewardMax = 0.05f;

    [Header("Debug")]
    public bool debugLogs = false;

    Rigidbody rb;
    int stepCount = 0;

    // cached values
    float moveSpeed => enemyData != null ? enemyData.moveSpeed : defaultMoveSpeed;
    float _roomSize = 5f; // used for normalization/randomization

    // distance shaping
    float lastDistance = 0f;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        // do not force-change collider.isTrigger here; let designer choose
    }

    public override void OnEpisodeBegin()
    {
        stepCount = 0;

        // reset velocities and rotation
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;         // fixed: use .velocity
            rb.angularVelocity = Vector3.zero;
        }

        // Randomize agent position within room bounds
        Vector3 agentPos = new Vector3(Random.Range(-_roomSize, _roomSize), transform.position.y, Random.Range(-_roomSize, _roomSize));
        transform.position = agentPos;
        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // Randomize player position, ensure not overlapping
        if (playerTransform != null)
        {
            Vector3 playerPos;
            do
            {
                playerPos = new Vector3(Random.Range(-_roomSize, _roomSize), playerTransform.position.y, Random.Range(-_roomSize, _roomSize));
            } while (Vector3.Distance(playerPos, transform.position) < 1.2f);

            playerTransform.position = playerPos;

            // init lastDistance for reward shaping
            lastDistance = Vector3.Distance(transform.position, playerTransform.position);
        }
        else
        {
            lastDistance = 0f;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (playerTransform == null)
        {
            // fallback zeros
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(transform.forward.x);
            sensor.AddObservation(transform.forward.z);
            return;
        }

        // relative position (player - agent) on X,Z, normalized by room size
        Vector3 rel = playerTransform.position - transform.position;
        sensor.AddObservation(rel.x / _roomSize);
        sensor.AddObservation(rel.z / _roomSize);

        // agent forward vector x,z
        Vector3 f = transform.forward;
        sensor.AddObservation(f.x);
        sensor.AddObservation(f.z);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Discrete action single branch
        int action = actionBuffers.DiscreteActions[0];

        float forward = 0f;
        float strafe = 0f;

        switch (action)
        {
            case 0: forward = 1f; break;   // forward
            case 1: forward = -1f; break;  // backward
            case 2: strafe = -1f; break;   // left (strafe)
            case 3: strafe = 1f; break;    // right (strafe)
            default: break;
        }

        // compute desired movement in local space (forward and right)
        Vector3 desiredLocal = transform.forward * forward + transform.right * strafe;
        Vector3 desiredDir = desiredLocal.sqrMagnitude > 0f ? desiredLocal.normalized : Vector3.zero;
        Vector3 desiredWorld = desiredDir * moveSpeed * Time.fixedDeltaTime; // use fixed delta

        if (rb != null)
        {
            // Rigidbody-based movement (safer for physics)
            rb.MovePosition(rb.position + desiredWorld);
        }
        else
        {
            transform.position += desiredWorld;
        }

        // small time-step penalty to encourage faster completion
        AddReward(-0.001f);

        // reward for reducing distance (shaping)
        if (playerTransform != null)
        {
            float curDist = Vector3.Distance(transform.position, playerTransform.position);
            float delta = lastDistance - curDist; // positive when getting closer
            float shaped = Mathf.Clamp(delta * distanceRewardScale, distanceRewardMin, distanceRewardMax);
            AddReward(shaped);

            if (debugLogs && Mathf.Abs(shaped) > 0f)
            {
                Debug.Log($"[ZombieAgent] distance delta={delta:F4}, shapedReward={shaped:F4}");
            }

            lastDistance = curDist;

            // success
            if (curDist <= successDistance)
            {
                AddReward(1.0f);
                if (debugLogs) Debug.Log("[ZombieAgent] SUCCESS: reached player");
                EndEpisode();
                return;
            }
        }

        stepCount++;
        if (stepCount >= maxStepLimit)
        {
            // timeout — end episode (no success reward)
            if (debugLogs) Debug.Log("[ZombieAgent] Timeout EndEpisode");
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // map keys to discrete actions for manual testing (W,S,A,D)
        var discrete = actionsOut.DiscreteActions;
        discrete[0] = 0; // default forward

        // Note: keep Heuristic only for testing — trainer/inference will override when running
        if (Input.GetKey(KeyCode.W)) discrete[0] = 0; // forward
        if (Input.GetKey(KeyCode.S)) discrete[0] = 1; // backward
        if (Input.GetKey(KeyCode.A)) discrete[0] = 2; // left (strafe)
        if (Input.GetKey(KeyCode.D)) discrete[0] = 3; // right (strafe)
    }

    private void OnTriggerEnter(Collider other)
    {
        // detect player collision (recommended: set Player collider or Zombie collider isTrigger = true)
        if (playerTransform != null && other.gameObject == playerTransform.gameObject)
        {
            AddReward(1.0f);
            if (debugLogs) Debug.Log("[ZombieAgent] OnTriggerEnter player - EndEpisode");
            EndEpisode();
        }
    }

    // fallback: optional OnCollisionEnter if you prefer non-trigger colliders
    private void OnCollisionEnter(Collision collision)
    {
        if (playerTransform != null && collision.gameObject == playerTransform.gameObject)
        {
            AddReward(1.0f);
            if (debugLogs) Debug.Log("[ZombieAgent] OnCollisionEnter player - EndEpisode");
            EndEpisode();
        }
    }
}
