using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ShaderMaterial
{
    [ColorUsage(true)] public Color color;
    [ColorUsage(true)] public Color emissionColor;
    public float emissionStrength;
    [Range(0, 1)] public float roughness;
}

public class Sphere : MonoBehaviour
{
    public ShaderMaterial Material; 

    public float[] GetMaterial()
    {
        float[] ret = new float[9];
        ret[0] = Material.color.r;
        ret[1] = Material.color.g;
        ret[2] = Material.color.b;
        ret[3] = Material.color.a;
        ret[4] = Material.emissionColor.r;
        ret[5] = Material.emissionColor.g;
        ret[6] = Material.emissionColor.b;
        ret[7] = Material.emissionStrength;
        ret[8] = Material.roughness;
        return ret;
    }
}
