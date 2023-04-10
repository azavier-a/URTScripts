using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialHolder : MonoBehaviour
{
    [SerializeField, ColorUsage(true)] Color color;
    ShaderMaterial material;

    public ShaderMaterial GetMaterial()
    {
        material.color = new Vector4(color.r, color.g, color.b, color.a);

        return material;
    }
}
