using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum MapDisplay
{
    Height,
    Moisture,
    Heat,
    Biome
}

[ExecuteInEditMode]
public class Map : MonoBehaviour
{
    public MapDisplay displayType;
    public RawImage debugImage;
    public BiomePreset[] biomes;
    public GameObject tilePrefab;

    [Header("Dimensions")]
    public int width;
    public int height;
    public float scale;
    public Vector2 offset;

    [Header("Height Map")]
    public Wave[] heightWaves;
    public Gradient heightDebugColors;
    public float[,] heightMap;

    [Header("Moisture Map")]
    public Wave[] moistureWaves;
    public Gradient moistureDebugColors;
    public float[,] moistureMap;

    [Header("Heat Map")]
    public Wave[] heatWaves;
    public Gradient heatDebugColors;
    public float[,] heatMap;

    private float lastGenerateTime;

    void Start ()
    {
        if(Application.isPlaying)
            GenerateMap();
    }
    
    void Update ()
    {
        if(Application.isPlaying)
            return;

        // true every 0.1 seconds
        if(Time.time - lastGenerateTime > 0.1f)
        {
            lastGenerateTime = Time.time;
            GenerateMap();
        }
    }

    void GenerateMap ()
    {
        // generate the height map
        heightMap = NoiseGenerator.Generate(width, height, scale, offset, heightWaves);

        // generate the moisture map
        moistureMap = NoiseGenerator.Generate(width, height, scale, offset, moistureWaves);

        // generate the heat map
        heatMap = NoiseGenerator.Generate(width, height, scale, offset, heatWaves);

        // create an array of colors for each pixel in the texture
        Color[] pixels = new Color[width * height];
        int i = 0;

        // loop through each pixel
        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                // how do we want to display the debug map?
                switch(displayType)
                {
                    case MapDisplay.Height:
                        pixels[i] = heightDebugColors.Evaluate(heightMap[x, y]);
                        break;
                    case MapDisplay.Moisture:
                        pixels[i] = moistureDebugColors.Evaluate(moistureMap[x, y]);
                        break;
                    case MapDisplay.Heat:
                        pixels[i] = heatDebugColors.Evaluate(heatMap[x, y]);
                        break;
                    case MapDisplay.Biome:
                    {
                        BiomePreset biome = GetBiome(heightMap[x, y], moistureMap[x, y], heatMap[x, y]);
                        pixels[i] = biome.debugColor;

                        if(Application.isPlaying)
                        {
                            GameObject tile = Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity);
                            tile.GetComponent<SpriteRenderer>().sprite = biome.GetTileSprite();
                        }

                        break;
                    }
                }

                i++;
            }
        }

        // create the texture
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        // apply the texture to the raw image
        debugImage.texture = tex;
    }

    // returns the closest biome that matches the given values
    BiomePreset GetBiome (float height, float moisture, float heat)
    {
        BiomePreset biomeToReturn = null;
        List<BiomePreset> tempBiomes = new List<BiomePreset>();

        // loop through each biome and if it meets the min requirements - add it to tempBiomes
        foreach(BiomePreset biome in biomes)
        {
            if(biome.MatchCondition(height, moisture, heat))
            {
                tempBiomes.Add(biome);
            }
        }

        float curValue = 0.0f;

        // loop through each of the biomes that meet the minimum requirements
        // find the one closes to the original height, moisture and heat values
        foreach(BiomePreset biome in tempBiomes)
        {
            float diffValue = (height - biome.minHeight) + (moisture - biome.minMoisture) + (heat - biome.minHeat);

            if(biomeToReturn == null)
            {
                biomeToReturn = biome;
                curValue = diffValue;
            }
            else if(diffValue < curValue)
            {
                biomeToReturn = biome;
                curValue = diffValue;
            }
        }

        // if no biome is found - return the first one in the biomes array
        if(biomeToReturn == null)
            return biomes[0];

        return biomeToReturn;
    }
}