
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class Raytracing : MonoBehaviour
{
    [SerializeField] int maxBounces = 3;
    [SerializeField] int samples = 2;
    [SerializeField] bool useShaderInSceneView = false;
    [SerializeField] bool useProgressiveRendering = false;
    [SerializeField] Shader rayTracingShader;
    Material rayTracingMaterial;

    GameObject[] GetChildren(GameObject obj)
    {
        List<GameObject> children = new List<GameObject>();

        for (int i = 0; i < obj.transform.childCount; i++)
        {
            children.Add(obj.transform.GetChild(i).gameObject);
        }

        return children.ToArray();
    }

    Texture2D RenderTextureToTexture2D(RenderTexture rtex)
    {
        Texture2D tex = new Texture2D(rtex.width, rtex.height, TextureFormat.RGB24, false);
        RenderTexture.active = rtex;
        tex.ReadPixels(new Rect(0, 0, rtex.width, rtex.height), 0, 0);
        tex.Apply();
        return tex;
    }

    Texture2D frameOld;
    GraphicsBuffer buffer;

    int NumRenderedFrames = 0;

    // called after a camera finishes rendering into the source texture
    bool clr = false;
    void OnRenderImage(RenderTexture source, RenderTexture target)
    {
        if(!rayTracingMaterial)
            rayTracingMaterial = new Material(rayTracingShader);

        Camera cam = Camera.current;
        if(cam.name != "SceneCamera" || useShaderInSceneView)
        {
            clr = true;

            NumRenderedFrames++;
            UpdateCameraParams(cam);

            RenderTexture newFrameOld = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0 );
            if (frameOld)
                Graphics.Blit(frameOld, newFrameOld, rayTracingMaterial);
            else
                Graphics.Blit(null, newFrameOld, rayTracingMaterial);

            frameOld = RenderTextureToTexture2D(newFrameOld);
            Graphics.Blit(frameOld, target);
            newFrameOld.Release();
        } 
        else
            Graphics.Blit(source, target);
        if (clr)
        {
            buffer.Release();
            buffer.Dispose();
        }
    }

    struct MaterialData
    {
        public float r, g, b, a;
        public float er, eg, eb, es;
    }

    unsafe struct SphereData
    {
        public float x, y, z;
        public float radius;
        public MaterialData material;
    }

    bool wasProgressiveLastFrame = false;
    void UpdateCameraParams(Camera cam)
    {

        GameObject[] spheres = GetChildren(GameObject.Find("Spheres"));
        

        buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, spheres.Length, (3*4 + 4) + (4*4 + 4*4));
        SphereData[] bufferData = new SphereData[spheres.Length];
        unsafe
        {
            
            for (int i = 0; i < spheres.Length; i++)
            {
                GameObject sp = spheres[i];

                bufferData[i].x = sp.transform.position.x;
                bufferData[i].y = sp.transform.position.y;
                bufferData[i].z = sp.transform.position.z;

                bufferData[i].radius = sp.transform.localScale.x / 2f;

                float[] mat = sp.GetComponent<Sphere>().GetMaterial();
                bufferData[i].material.r = mat[0];
                bufferData[i].material.g = mat[1];
                bufferData[i].material.b = mat[2];
                bufferData[i].material.a = mat[3];
                bufferData[i].material.er = mat[4];
                bufferData[i].material.eg = mat[5];
                bufferData[i].material.eb = mat[6];
                bufferData[i].material.es = mat[7];
            }
            buffer.SetData(bufferData);
        }

        float planeH = cam.nearClipPlane * Mathf.Tan(cam.fieldOfView * .5f * Utils.Deg2Rad) * 2;
        float planeW = planeH * cam.aspect;

        rayTracingMaterial.SetMatrix("CamLocalToWorldMatrix", cam.transform.localToWorldMatrix);
        rayTracingMaterial.SetVector("ViewParams", new Vector3(planeW, planeH, cam.nearClipPlane));

        rayTracingMaterial.SetInteger("Frame", NumRenderedFrames);
        rayTracingMaterial.SetInteger("NumSpheres", spheres.Length);
        rayTracingMaterial.SetInteger("MaxBounces", maxBounces);
        rayTracingMaterial.SetInteger("SamplesPerPixel", samples);

        if(useProgressiveRendering && !wasProgressiveLastFrame)
            NumRenderedFrames = 0;

        rayTracingMaterial.SetInteger("UseProgressiveRendering", useProgressiveRendering ? 1 : 0);
        wasProgressiveLastFrame = useProgressiveRendering;

        rayTracingMaterial.SetBuffer("SpheresBuffer", buffer);
    }
}
