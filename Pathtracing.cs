    
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class Pathtracing : MonoBehaviour
{
    [Header("Path Tracing Settings")]
    [SerializeField] int time = 0;
    [SerializeField] int maxBounces = 3;
    [SerializeField] int samples = 2;
    [SerializeField] bool useShaderInSceneView = false;
    [SerializeField] bool useProgressiveRendering = false;
    [SerializeField] bool alignCameraToSceneView = false;
    [SerializeField] bool useBackground = true;
    [SerializeField] Shader rayTracingShader;
    Material rayTracingMaterial;

    Texture2D RenderTextureToTexture2D(RenderTexture rtex)
    {
        Texture2D tex = new Texture2D(rtex.width, rtex.height, TextureFormat.RGB24, false);
        RenderTexture.active = rtex;
        tex.ReadPixels(new Rect(0, 0, rtex.width, rtex.height), 0, 0);
        tex.Apply();
        return tex;
    }

    GameObject[] GetGameObjectsWithScript<T>() where T : MonoBehaviour
    {
        T[] ScriptArray = Object.FindObjectsOfType<T>();
        GameObject[] ObjectArray = new GameObject[ScriptArray.Length];
        for (int i = 0; i < ScriptArray.Length; i++)
            ObjectArray[i] = ScriptArray[i].gameObject;

        return ObjectArray;
    }

    Texture2D frameOld;
    GraphicsBuffer buffer;

    int NumRenderedFrames = 0;

    // called after a camera finishes rendering into the source texture
    void OnRenderImage(RenderTexture source, RenderTexture target)
    {
        if(!rayTracingMaterial)
            rayTracingMaterial = new Material(rayTracingShader);

        Camera cam = Camera.current;
        if (alignCameraToSceneView && cam.name == "SceneCamera")
        {
            Camera.main.transform.position = cam.transform.position;
            Camera.main.transform.rotation = cam.transform.rotation;
        }

        if(cam.name != "SceneCamera" || useShaderInSceneView)
        {
            UpdateCameraParams(cam);

            RenderTexture newFrameOld = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0 );
            if (frameOld)
                Graphics.Blit(frameOld, newFrameOld, rayTracingMaterial);
            else
                Graphics.Blit(null, newFrameOld, rayTracingMaterial);

            frameOld = RenderTextureToTexture2D(newFrameOld);
            Graphics.Blit(frameOld, target);
            
            NumRenderedFrames++;

            newFrameOld.Release();
            buffer.Release();
            buffer.Dispose();
        } 
        else
            Graphics.Blit(source, target);
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
        GameObject[] spheres = GetGameObjectsWithScript<Sphere>();

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

        rayTracingMaterial.SetInteger("Time", time);
        rayTracingMaterial.SetInteger("Frame", NumRenderedFrames);
        rayTracingMaterial.SetInteger("NumSpheres", spheres.Length);
        rayTracingMaterial.SetInteger("MaxBounces", maxBounces);
        rayTracingMaterial.SetInteger("SamplesPerPixel", samples);

        if(useProgressiveRendering && !wasProgressiveLastFrame)
            NumRenderedFrames = 0;

        rayTracingMaterial.SetInteger("UseProgressiveRendering", useProgressiveRendering ? 1 : 0);
        wasProgressiveLastFrame = useProgressiveRendering;
        rayTracingMaterial.SetInteger("UseBackground", useBackground ? 1 : 0);

        rayTracingMaterial.SetBuffer("SpheresBuffer", buffer);
    }
}
