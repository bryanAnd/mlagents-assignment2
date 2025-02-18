using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class CustomAgent : Agent
{
    private Rigidbody2D rb;
    private Transform childSpriteTransform;
    private int episodeCount;

    [Header("Standard Attributes")]
    public Transform targetTransform;
    public float velocityMultiplier;

    [Header("Distance observation")]
    public float k;

    [Header("Target Spawning Attributes")]
    public int numChecks;
    /*
     * Starting range is normally set to 3, so a max spawn area of +3 x and -3 x, and +3 y and -3 y.
     * Refer to Tristan if that doesnt make sense. 
     */
    public float startingRange;

    /*
     * Range increment is like 0.05, it just increases the range when a particular number of episodes has elapsed.
     */
    public float rangeIncrement;

    /*
     * This is a really important attribute, since there the only way for an episode to reset is for the agent to get to 
     * the goal it is important to tinker with this attribute, I forgot what I set it to but I think it was like maybe 50 or even 100. IDK, soz 
     */
    public int episodesTillRangeIncrement;

    /*
     * This is set to 10, unless you change the size of the play area, dont change this value.
     */
    public float maxRange;

    private float range;
    

    private void Start()
    {
        range = startingRange;
        childSpriteTransform = transform.GetChild(0);
        episodeCount = 0;
        rb = GetComponent<Rigidbody2D>();
        rb.angularVelocity = 0f;
    }

    public override void OnEpisodeBegin()
    {
        /*
         * So essentially, each time the agent reaches a goal this will increase the episode count,
         * if then agent reaches the goal enough, the goal will be able to spawn into a larger radius around the map,
         * this basically tricks the agent at the start into collecting / realising that going to the goal is good and thus
         * by the time the goal is starts to spawn far away the agent realises that it needs to go to the goal.
         */
        episodeCount += 1;
        if (episodeCount >= episodesTillRangeIncrement)
        {
            if (range < maxRange)
            {
                range += rangeIncrement;
            }
        }

        ResetTargetPosition();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        //sensor.AddObservation(targetTransform.localPosition);
        //sensor.AddObservation(transform.localPosition);

        //2
        sensor.AddObservation((Vector2)(targetTransform.position - transform.position));

        //2
        sensor.AddObservation(GetObservableDistance());

        //1
        sensor.AddObservation(StepCount / MaxStep);

        //5 observations
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        /*
         * it would be cool to experiment with continous actions.
         */
        Vector2 movementDirection = Vector2.zero;

        int movement = actionBuffers.DiscreteActions[0];

        if (movement == 0)
        {
            movementDirection.x = 1;
        }
        if (movement == 1)
        {
            movementDirection.x = -1;
        }
        if (movement == 2)
        {
            movementDirection.y = 1;
        }
        if (movement == 3)
        {
            movementDirection.y = -1;
        }

        rb.velocity = movementDirection.normalized * velocityMultiplier * Time.fixedDeltaTime;

        if (movementDirection != Vector2.zero)
        {
            childSpriteTransform.up = rb.velocity;
        }

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Target"))
        {
            AddReward(1.0f);
            EndEpisode();
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Target"))
        {
            AddReward(1.0f);
            EndEpisode();
        }
    }

    public Vector2 GetObservableDistance()
    {
        Vector2 offset = targetTransform.position - transform.position;
        float d = Mathf.Sqrt(offset.x * offset.x + offset.y * offset.y);
        Vector2 unitVector = (1 / d) * offset;
        return Mathf.Exp(-k * d) * unitVector;
    }

    public void ResetTargetPosition()
    {
        for (int i = 0; i < numChecks; i++)
        {
            Vector2 possiblePosition = new Vector2(transform.position.x + Random.Range(-range, range), transform.position.y + Random.Range(-range, range));

            Vector2 transformedPosition = new Vector2(transform.parent.position.x - possiblePosition.x, transform.parent.position.y - possiblePosition.y);

            if (transformedPosition.x < range &&
                transformedPosition.y < range &&
                transformedPosition.x > -range &&
                transformedPosition.y > -range)
            {
                if (!Physics2D.OverlapBox(possiblePosition, new Vector2(1, 1), 0f))
                {
                    targetTransform.position = possiblePosition;
                    break;
                }
            }
        }
    }
}
