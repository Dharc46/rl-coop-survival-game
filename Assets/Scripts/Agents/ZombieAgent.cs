using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

/// <summary>
/// ZombieAgent - Continuous(2) actions version.
/// Observations (6):
///  - rel.x / roomSize
///  - rel.z / roomSize
///  - forward.x
///  - forward.z
///  - distance / roomSize
///  - signedAngle/PI (in [-1,1])
/// Actions (continuous 2):
///  - actions[0] = forward speed factor (-1..1)
///  - actions[1] = strafe speed factor (-1..1)
/// Movement: Rigidbody.MovePosition
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
    [Tooltip("Global multiplier to slow/speed agent for testing")]
    public float speedMultiplier = 0.5f;

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
            rb.linearVelocity = Vector3.zero;
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
            // fallback zeros (6 floats)
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(transform.forward.x);
            sensor.AddObservation(transform.forward.z);
            sensor.AddObservation(0f); // distance
            sensor.AddObservation(0f); // angle
            return;
        }

        Vector3 rel3 = playerTransform.position - transform.position;
        Vector3 rel = new Vector3(rel3.x, 0f, rel3.z);
        float dist = rel.magnitude;

        // normalized rel x,z
        sensor.AddObservation(rel.x / _roomSize);
        sensor.AddObservation(rel.z / _roomSize);

        // forward x,z
        Vector3 f = transform.forward;
        sensor.AddObservation(f.x);
        sensor.AddObservation(f.z);

        // distance normalized
        sensor.AddObservation(Mathf.Clamp(dist / _roomSize, 0f, 1f));

        // signed angle between forward and rel (range -PI..PI) normalized by PI => [-1,1]
        Vector3 dirNorm = rel.magnitude > 0f ? rel.normalized : Vector3.forward;
        float forwardDot = Vector3.Dot(transform.forward.normalized, dirNorm);
        float rightDot = Vector3.Dot(transform.right.normalized, dirNorm);
        float angleRad = Mathf.Atan2(rightDot, forwardDot); // signed
        sensor.AddObservation(angleRad / Mathf.PI);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Continuous actions: two floats in [-1,1]
        float forwardInput = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
        float strafeInput  = Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);

        // scale and apply movement (use fixed delta for physics)
        Vector3 desiredLocal = transform.forward * forwardInput + transform.right * strafeInput;
        Vector3 desiredDir = desiredLocal.sqrMagnitude > 0f ? desiredLocal.normalized : Vector3.zero;
        Vector3 desiredWorld = desiredDir * moveSpeed * speedMultiplier * Time.fixedDeltaTime;

        if (rb != null)
        {
            rb.MovePosition(rb.position + desiredWorld);
        }
        else
        {
            transform.position += desiredWorld;
        }

        // time-step penalty
        AddReward(-0.001f);

        // distance shaping
        if (playerTransform != null)
        {
            float curDist = Vector3.Distance(transform.position, playerTransform.position);
            float delta = lastDistance - curDist;
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
            if (debugLogs) Debug.Log("[ZombieAgent] Timeout EndEpisode");
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // provide continuous heuristic: W/S => forward (-1..1), A/D => strafe (-1..1)
        var cont = actionsOut.ContinuousActions;
        float f = 0f;
        float s = 0f;
        if (Input.GetKey(KeyCode.W)) f += 1f;
        if (Input.GetKey(KeyCode.S)) f -= 1f;
        if (Input.GetKey(KeyCode.D)) s += 1f;
        if (Input.GetKey(KeyCode.A)) s -= 1f;

        cont[0] = Mathf.Clamp(f, -1f, 1f);
        cont[1] = Mathf.Clamp(s, -1f, 1f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playerTransform != null && other.gameObject == playerTransform.gameObject)
        {
            AddReward(1.0f);
            if (debugLogs) Debug.Log("[ZombieAgent] OnTriggerEnter player - EndEpisode");
            EndEpisode();
        }
    }

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
