using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Utils
{
    private static int maxHeight = 150;
    private static float smooth = 0.01f;
    private static int octaves = 4;
    private static float persistence = 0.5f;

    public static int GenerateHeight(float x, float z)
    {
        //Parameters should come in from the chunk
        //noise.pnoise
        float height = Map(0, maxHeight, 0, 1, FBM(x * smooth, z * smooth, octaves, persistence));
        return (int)height;
    }
    
    public static int GenerateStoneHeight(float x, float z)
    {
        //Parameters should come in from the chunk
        float height = Map(0, maxHeight - 5, 0, 1, FBM(x * smooth * 2, z * smooth * 2, octaves + 1, persistence));
        return (int)height;
    }

    private static float Map(float newmin, float newmax, float originalMin, float originalMax, float value)
    {
        return Mathf.Lerp(newmin, newmax, Mathf.InverseLerp(originalMin, originalMax, value));
    }

    public static float FBM3D(float x, float y, float z, float sm, int oct)
    {
        float XY = FBM(x * sm, y * sm, oct, 0.5f);
        float YZ = FBM(y * sm, z * sm, oct, 0.5f);
        float XZ = FBM(x * sm, z * sm, oct, 0.5f);

        float YX = FBM(y * sm, x * sm, oct, 0.5f);
        float ZY = FBM(z * sm, y * sm, oct, 0.5f);
        float ZX = FBM(z * sm, x * sm, oct, 0.5f);

        return (XY + YZ + XZ + YX + ZY + ZX) / 6.0f;
    }

    private static float FBM(float x, float z, int oct, float pers)
    {
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float maxValue = 0;
        float offset = 32000f;

        for(int i = 0; i < oct; i++)
        {
            total += Mathf.PerlinNoise(x + offset * frequency, z + offset * frequency) * amplitude;

            maxValue += amplitude;

            amplitude *= pers;
            frequency *= 2;
        }

        return total / maxValue;
    }

}
