using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int numberOfPredatorsOnStartup;
    public int predatorSpawnRadius;
    public GameObject predatorPrefab;
    public int numberOfPreyOnStartup;
    public int preySpawnRadius;
    public int spawnLocationCount;
    public GameObject preyPrefab;
    public int amountOfFoodOnStartup;
    public GameObject foodPrefab;
    public float[] arenaSize;
    public GameObject arenaEdgeCollider;

    private int[] predatorLayers = new int[4] { 3, 10, 5, 2 };
    private int[] preyLayers = new int[4] { 3, 10, 5, 2 };
    [Range(0.0001f, 1f)] public float mutationChance = 0.01f;
    [Range(0f, 1f)] public float mutationVariance = 0.5f;
    [Range(0.1f, 10f)] public float Gamespeed = 1f;

    public int totalPredatorCount = 0;
    public int totalPreyCount = 0;
    public int totalFoodCount = 0;
    private int executionCycle = 0;
    private List<float[]> foodSpawns = new List<float[]>();

    void Start()
    {
        InitialiseEdgeCollider();
        GenerateFood();
        GeneratePredatorsAndPrey();
    }

    void FixedUpdate()
    {
        if (totalFoodCount < amountOfFoodOnStartup)
        {
            float[] foodSpawn = foodSpawns[Random.Range(0, foodSpawns.Count)];
            Instantiate(foodPrefab, Random.insideUnitCircle * foodSpawn[2] + new Vector2(foodSpawn[0], foodSpawn[1]), this.transform.rotation);
            totalFoodCount++;
        }
    }

    void InitialiseEdgeCollider()
    {
        EdgeCollider2D arenaEdge = (Instantiate(arenaEdgeCollider, new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0))).GetComponent<EdgeCollider2D>();
        List<Vector2> edgePoints = new List<Vector2>();
        edgePoints.AddRange(new Vector2[5] { new Vector2(0, 0), new Vector2(0, arenaSize[1]), new Vector2(arenaSize[0], arenaSize[1]), new Vector2(arenaSize[0], 0), new Vector2(0, 0) });
        arenaEdge.SetPoints(edgePoints);
    }

    void GeneratePredatorsAndPrey()
    {
        bool isValidSpawnLocation;
        Vector2 preySpawnCenter;
        int counterForException;
        //find valid prey spawn location
        for (int i = 0; i < spawnLocationCount; i++)
        {
            isValidSpawnLocation = false;
            preySpawnCenter = new Vector2(0, 0);
            counterForException = 0;
            while (!isValidSpawnLocation)
            {
                preySpawnCenter = new Vector2(Random.Range(preySpawnRadius, arenaSize[0] - preySpawnRadius), Random.Range(preySpawnRadius, arenaSize[1] - preySpawnRadius));
                foreach (float[] foodSpawn in foodSpawns)
                {
                    isValidSpawnLocation = Mathf.Pow((preySpawnCenter.x - foodSpawn[0]), 2) + Mathf.Pow((preySpawnCenter.y - foodSpawn[1]), 2) > Mathf.Pow((foodSpawn[2] + preySpawnRadius), 2);
                    if (!isValidSpawnLocation)
                    {
                        break;
                    }
                }
                if (counterForException > 10000)
                {
                    throw new System.Exception("Couldn't find valid prey spawn location");
                }
                counterForException++;
            }
            //spawn prey
            for (int j = 0; j < numberOfPreyOnStartup / spawnLocationCount; j++)
            {
                Vector2 preyLoaction = Random.insideUnitCircle * preySpawnRadius + preySpawnCenter;
                CreatePrey(preyLoaction);
            }

            //find valid predator spawn location
            isValidSpawnLocation = false;
            Vector2 predatorSpawnCenter = new Vector2(0, 0);
            counterForException = 0;
            while (!isValidSpawnLocation)
            {
                predatorSpawnCenter = new Vector2(Random.Range(predatorSpawnRadius, arenaSize[0] - predatorSpawnRadius), Random.Range(predatorSpawnRadius, arenaSize[1] - predatorSpawnRadius));
                foreach (float[] foodSpawn in foodSpawns)
                {
                    isValidSpawnLocation = Mathf.Pow((predatorSpawnCenter.x - foodSpawn[0]), 2) + Mathf.Pow((predatorSpawnCenter.y - foodSpawn[1]), 2) > Mathf.Pow((foodSpawn[2] + predatorSpawnRadius), 2)
                                        && Mathf.Pow((predatorSpawnCenter.x - preySpawnCenter.x), 2) + Mathf.Pow((predatorSpawnCenter.y - preySpawnCenter.y), 2) > Mathf.Pow((preySpawnRadius + predatorSpawnRadius), 2);
                    if (!isValidSpawnLocation)
                    {
                        break;
                    }
                }
                if (counterForException > 10000)
                {
                    throw new System.Exception("Couldn't find valid predator spawn location");
                }
                counterForException++;
            }
            //spawn predators
            for (int j = 0; j < numberOfPredatorsOnStartup / spawnLocationCount; j++)
            {
                Vector2 predatorLoaction = Random.insideUnitCircle * predatorSpawnRadius + predatorSpawnCenter;
                CreatePredator(predatorLoaction, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            }
        }


    }

    public Predator CreatePredator(Vector3 position)
    {
        return CreatePredator(position, this.transform.rotation);
    }
    public Predator CreatePredator(Vector3 position, Quaternion rotation)
    {
        Predator newPredator = (Instantiate(predatorPrefab, position, rotation)).GetComponent<Predator>();
        newPredator.InitialisePredator(executionCycle, this, new float[8] { 100f, 301f, 0.1f, 0.25f, 15f, 150f/*gestation length*/, 1f, 100f });

        executionCycle = executionCycle == 4 ? 0 : executionCycle + 1;

        predatorLayers[0] = 3 + (int)(newPredator.fieldOfView * 360 / 10) * 4;
        newPredator.neuralNetwork = new NeuralNetwork(predatorLayers);

        totalPredatorCount++;

        return newPredator;
    }
    public Predator CreatePredator(Vector3 position, Quaternion rotation, NeuralNetwork neuralNetworkPreMutation)
    {
        Predator offspring = CreatePredator(position, rotation);
        offspring.neuralNetwork = neuralNetworkPreMutation.copy(new NeuralNetwork(predatorLayers));
        offspring.neuralNetwork.Mutate(mutationChance, mutationVariance);

        return offspring;
    }

    public Prey CreatePrey(Vector3 position)
    {
        return CreatePrey(position, this.transform.rotation);
    }
    public Prey CreatePrey(Vector3 position, Quaternion rotation)
    {
        Prey newPrey = (Instantiate(preyPrefab, position, rotation)).GetComponent<Prey>();
        newPrey.InitialisePrey(executionCycle, this, new float[7] { 100f, 301f, 0.1f, 0.8f, 10f, 100f/*gestation length*/, 2f });

        executionCycle = executionCycle == 4 ? 0 : executionCycle + 1;

        preyLayers[0] = 3 + (int)(newPrey.fieldOfView * 360 / 10) * 5;
        newPrey.neuralNetwork = new NeuralNetwork(preyLayers);

        totalPreyCount++;

        return newPrey;
    }
    public Prey CreatePrey(Vector3 position, Quaternion rotation, NeuralNetwork neuralNetworkPreMutation)
    {
        Prey offspring = CreatePrey(position, rotation);
        offspring.neuralNetwork = neuralNetworkPreMutation.copy(new NeuralNetwork(preyLayers));
        offspring.neuralNetwork.Mutate(mutationChance, mutationVariance);

        return offspring;
    }

    void GenerateFood()
    {
        int foodGenerated = 0;

        while (foodGenerated < amountOfFoodOnStartup)
        {
            int spawnSizeIdx = Random.Range(0, 17);
            float circleRadius = 1;
            int foodToGenerate = 0;
            Vector2 spawnLocation = new Vector2(0, 0);

            switch (spawnSizeIdx)
            {
                case 0:
                    if (foodGenerated <= amountOfFoodOnStartup - 1)
                    {
                        circleRadius = 1f;
                        foodToGenerate = 1;
                        spawnLocation = new Vector2(Random.Range(1, arenaSize[0] - circleRadius), Random.Range(1, arenaSize[1] - circleRadius));
                    }
                    break;
                case 1:
                    if (foodGenerated <= amountOfFoodOnStartup - 2)
                    {
                        circleRadius = 1;
                        foodToGenerate = 2;
                        spawnLocation = new Vector2(Random.Range(1, arenaSize[0] - circleRadius), Random.Range(1, arenaSize[1] - circleRadius));
                    }
                    break;
                case 2:
                    if (foodGenerated <= amountOfFoodOnStartup - 3)
                    {
                        circleRadius = 1;
                        foodToGenerate = 3;
                        spawnLocation = new Vector2(Random.Range(1, arenaSize[0] - circleRadius), Random.Range(1, arenaSize[1] - circleRadius));
                    }
                    break;
                case 3:
                    if (foodGenerated <= amountOfFoodOnStartup - 5)
                    {
                        circleRadius = 1;
                        foodToGenerate = 5;
                        spawnLocation = new Vector2(Random.Range(1, arenaSize[0] - circleRadius), Random.Range(1, arenaSize[1] - circleRadius));
                    }
                    break;
                case 4:
                    if (foodGenerated <= amountOfFoodOnStartup - 7)
                    {
                        circleRadius = 1f;
                        foodToGenerate = 7;
                        spawnLocation = new Vector2(Random.Range(1, arenaSize[0] - circleRadius), Random.Range(1, arenaSize[1] - circleRadius));
                    }
                    break;
                case 5:
                    if (foodGenerated <= amountOfFoodOnStartup - 11)
                    {
                        circleRadius = 1;
                        foodToGenerate = 11;
                        spawnLocation = new Vector2(Random.Range(1, arenaSize[0] - circleRadius), Random.Range(1, arenaSize[1] - circleRadius));
                    }
                    break;
                case 6:
                    if (foodGenerated <= amountOfFoodOnStartup - 13)
                    {
                        circleRadius = 1;
                        foodToGenerate = 13;
                        spawnLocation = new Vector2(Random.Range(1, arenaSize[0] - circleRadius), Random.Range(1, arenaSize[1] - circleRadius));
                    }
                    break;
                case 7:
                    if (foodGenerated <= amountOfFoodOnStartup - 17)
                    {
                        circleRadius = 1.5f;
                        foodToGenerate = 17;
                        spawnLocation = new Vector2(Random.Range(1, arenaSize[0] - circleRadius), Random.Range(1, arenaSize[1] - circleRadius));
                    }
                    break;
                case 8:
                    if (foodGenerated <= amountOfFoodOnStartup - 19)
                    {
                        circleRadius = 1.5f;
                        foodToGenerate = 19;
                        spawnLocation = new Vector2(Random.Range(1, arenaSize[0] - circleRadius), Random.Range(1, arenaSize[1] - circleRadius));
                    }
                    break;
                case 9:
                    if (foodGenerated <= amountOfFoodOnStartup - 23)
                    {
                        circleRadius = 1.9f;
                        foodToGenerate = 23;
                        spawnLocation = new Vector2(Random.Range(1, arenaSize[0] - circleRadius), Random.Range(1, arenaSize[1] - circleRadius));
                    }
                    break;
                case 10:
                    if (foodGenerated <= amountOfFoodOnStartup - 29)
                    {
                        circleRadius = 2.3f;
                        foodToGenerate = 29;
                        spawnLocation = new Vector2(Random.Range(1, arenaSize[0] - circleRadius), Random.Range(1, arenaSize[1] - circleRadius));
                    }
                    break;
                case 11:
                    if (foodGenerated <= amountOfFoodOnStartup - 31)
                    {
                        circleRadius = 2.3f;
                        foodToGenerate = 31;
                        spawnLocation = new Vector2(Random.Range(1, arenaSize[0] - circleRadius), Random.Range(1, arenaSize[1] - circleRadius));
                    }
                    break;
                case 12:
                    if (foodGenerated <= amountOfFoodOnStartup - 37)
                    {
                        circleRadius = 2.5f;
                        foodToGenerate = 37;
                        spawnLocation = new Vector2(Random.Range(1, arenaSize[0] - circleRadius), Random.Range(1, arenaSize[1] - circleRadius));
                    }
                    break;
                case 13:
                    if (foodGenerated <= amountOfFoodOnStartup - 41)
                    {
                        circleRadius = 2.75f;
                        foodToGenerate = 41;
                        spawnLocation = new Vector2(Random.Range(1, arenaSize[0] - circleRadius), Random.Range(1, arenaSize[1] - circleRadius));
                    }
                    break;
                case 14:
                    if (foodGenerated <= amountOfFoodOnStartup - 43)
                    {
                        circleRadius = 2.75f;
                        foodToGenerate = 43;
                        spawnLocation = new Vector2(Random.Range(1, arenaSize[0] - circleRadius), Random.Range(1, arenaSize[1] - circleRadius));
                    }
                    break;
                case 15:
                    if (foodGenerated <= amountOfFoodOnStartup - 47)
                    {
                        circleRadius = 3f;
                        foodToGenerate = 47;
                        spawnLocation = new Vector2(Random.Range(1, arenaSize[0] - circleRadius), Random.Range(1, arenaSize[1] - circleRadius));
                    }
                    break;
                case 16:
                    if (foodGenerated <= amountOfFoodOnStartup - 53)
                    {
                        circleRadius = 3.2f;
                        foodToGenerate = 53;
                        spawnLocation = new Vector2(Random.Range(1, arenaSize[0] - circleRadius), Random.Range(1, arenaSize[1] - circleRadius));
                    }
                    break;
                default:
                    circleRadius = 1f;
                    foodToGenerate = 0;
                    break;
            }

            if (foodToGenerate > 0)
            {
                for (int i = 0; i < foodToGenerate; i++)
                {
                    Instantiate(foodPrefab, Random.insideUnitCircle * circleRadius + spawnLocation, this.transform.rotation);
                    totalFoodCount++;
                }

                foodGenerated += foodToGenerate;
                foodSpawns.Add(new float[4] { spawnLocation.x, spawnLocation.y, circleRadius, foodToGenerate });
            }
        }
    }
}
