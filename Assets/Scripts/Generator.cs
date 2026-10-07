using System.Numerics;
using UnityEngine;

public class Generator : MonoBehaviour
{
    //Random World Generation
    [SerializeField] private bool randomWorldGenEnabled = false;
    [SerializeField] private BigInteger seed;

    //Fixed World Generation
    //

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (randomWorldGenEnabled && seed == 0)
        {
            string seedConstruction = "";
            for (int character = 0; character < 24; character++)
            {
                seedConstruction += $"{(int)(Random.value * 10)}";
            }
            seed = BigInteger.Parse(seedConstruction);
            Debug.Log(seed);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
