using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Predator : MonoBehaviour
{
    private float maxHealthPoints = 100f;
    private float lifespan = 300f;
    private float speed = 0.1f;
    public float fieldOfView { get; private set; } = 0.3f;
    private int viewDistance = 10;
    private int maxViewDistance = 50;
    private int gestationLength = 60;
    private int offspringPerParturition = 1;
    private int attackDamage = 100;
    private int executionIdx = 0;

    [SerializeField] int gestationCost = 20;
    [SerializeField] int eatingHealthBonus = 50;
    [SerializeField] float speedCostPerSecond = 10f;
    [SerializeField] float healthCostPerSecond = 1f;
    [SerializeField] float rotationSpeed = 1f;

    public float currentHealthPoints { get; private set; }
    private float currentAge = 0f;
    private float currentSpeed = 0f;
    private float currentRotation = 0f;
    private int gestationCount = 0;
    private float[] raycastOutput;
    private float[] outputs;
    private int executionCycle = 0;

    private Rigidbody2D myRigidbody;
    private GameManager myGameManager;
    public LayerMask raycastMask;
    public NeuralNetwork neuralNetwork;

    void Start()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        GiveBirth();
        UpdateStatus();
    }
    void FixedUpdate()
    {
        Debug.Log("executionCycle = " + executionCycle + " || executionIdx = " + executionIdx);
        if (executionCycle == executionIdx)
        {
            raycastOutput = CastRays();
            outputs = GenerateOutputs();
            Debug.Log(outputs[0] + "||" + outputs[1]);
            currentRotation = outputs[0] * rotationSpeed;
            myRigidbody.MoveRotation(myRigidbody.rotation + currentRotation);
            currentSpeed = ((outputs[1] + 1) / 2) * speed;
            Vector2 velocity = transform.up * currentSpeed;
            myRigidbody.MovePosition(myRigidbody.position + velocity);
        }
        else
        {
            myRigidbody.MoveRotation(myRigidbody.rotation + currentRotation);
            Vector2 velocity = transform.up * currentSpeed;
            myRigidbody.MovePosition(myRigidbody.position + velocity);
        }
        executionCycle = executionCycle == 4 ? 0 : executionCycle + 1;
    }


    public void InitialisePredator(int executionIdx, GameManager gameManager, float[] initialisingValues)
    {
        this.executionIdx = executionIdx;
        this.myGameManager = gameManager;
        this.maxHealthPoints = initialisingValues[0];
        currentHealthPoints = maxHealthPoints;
        this.lifespan = initialisingValues[1];
        this.speed = initialisingValues[2];
        if (initialisingValues[3] >= 0.0001f && initialisingValues[3] <= 1f)
        {
            this.fieldOfView = initialisingValues[3];
        }
        else
        {
            throw (new System.Exception("field of view is outside of the range min=0.0001f, max=1f: " + fieldOfView));
        }
        if (initialisingValues[4] >= 0 && initialisingValues[4] <= maxViewDistance)
        {
            this.viewDistance = (int)initialisingValues[4];
        }
        else
        {
            throw (new System.Exception("view distance is outside of the range min=0, max=" + maxViewDistance + ": " + viewDistance));
        }
        if (initialisingValues[5] >= 10 /*60*/ && initialisingValues[5] <= lifespan)
        {
            this.gestationLength = (int)initialisingValues[5];
        }
        else
        {
            throw (new System.Exception("gestation length is outside of the range min=60, max=" + lifespan + ": " + gestationLength));
        }
        this.currentAge = Random.Range(-gestationLength / 5, 0);
        this.offspringPerParturition = (int)initialisingValues[6];
        this.attackDamage = (int)initialisingValues[7];
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Prey"))
        {
            Prey prey = other.gameObject.GetComponent<Prey>();
            prey.TakeDamage(attackDamage);

            if (prey.currentHealthPoints <= 0)
            {
                Destroy(other.gameObject);
                myGameManager.totalPreyCount--;

                if (currentHealthPoints + eatingHealthBonus > maxHealthPoints)
                {
                    currentHealthPoints = maxHealthPoints;
                }
                else
                {
                    currentHealthPoints += eatingHealthBonus;
                }
            }
        }
    }

    void UpdateStatus()
    {
        currentAge += Time.deltaTime;
        currentHealthPoints -= (healthCostPerSecond + currentSpeed * speedCostPerSecond) * Time.deltaTime;

        if (currentAge >= lifespan || currentHealthPoints <= 0f)
        {
            myGameManager.totalPredatorCount--;
            Destroy(gameObject);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealthPoints -= damageAmount;
    }

    float[] CastRays()
    {
        int numberOfRays = (int)(fieldOfView * 360 / 10);
        float[] output = new float[numberOfRays * 4];
        float angleOffset = -(float)numberOfRays * 10 / 2;

        for (int i = 0; i < numberOfRays * 4; i += 4)
        {
            Vector2 newVector = Quaternion.AngleAxis(i / 3 * 10 - angleOffset, new Vector3(0, 0, 1)) * transform.right;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, newVector, viewDistance, raycastMask);
            // Ray2D Ray = new Ray2D(transform.position, newVector);
            //Debug.DrawRay(transform.position, newVector * viewDistance, Color.red);

            if (hit)
            {
                output[i] = viewDistance - hit.distance / viewDistance;

                if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Prey"))
                {
                    output[i + 1] = 1f;
                }
                else
                {
                    output[i + 1] = 0f;
                }

                if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Predator"))
                {
                    output[i + 2] = 1f;
                }
                else
                {
                    output[i + 2] = 0f;
                }

                if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Obstacles"))
                {
                    output[i + 3] = 1f;
                }
                else
                {
                    output[i + 3] = 0f;
                }
            }
        }

        return output;
    }

    void GiveBirth()
    {
        Debug.Log("current age: " + currentAge + "|gestation length: " + gestationLength + "|gestation count: " + gestationCount);
        if ((currentAge >= gestationLength * (gestationCount + 1)) && (currentHealthPoints - gestationCost > 0))
        {
            for (int i = 0; i < offspringPerParturition; i++)
            {
                Vector3 spawnPosition = transform.position - 0.5f * transform.up;
                myGameManager.CreatePredator(spawnPosition, this.transform.rotation, this.neuralNetwork);
            }
            currentHealthPoints -= gestationCost * gestationCount;
            gestationCount++;
        }
    }

    float[] GenerateOutputs()
    {
        List<float> feedForwardList = new List<float>();

        feedForwardList.Add(currentHealthPoints / maxHealthPoints);
        feedForwardList.Add(currentAge / lifespan);
        feedForwardList.Add(currentSpeed / speed);
        feedForwardList.AddRange(raycastOutput);

        return neuralNetwork.FeedForward(feedForwardList.ToArray());
    }
}
