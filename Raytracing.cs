using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class Raytracing : MonoBehaviour
{
    [SerializeField] int maxBounces = 1;
    [SerializeField] int samples = 1;
    [SerializeField] bool useShaderInSceneView;
    [SerializeField] Shader rayTracingShader;
    [SerializeField] Shader framestackingShader;
    [SerializeField] GameObject sphereHolder;
    Material rayTracingMaterial;
    Material framestackingMaterial;

    GameObject[] GetChildren(GameObject obj)
    {
        List<GameObject> children = new List<GameObject>();

        for (int i = 0; i < obj.transform.childCount; i++)
        {
            children.Add(obj.transform.GetChild(i).gameObject);
        }

        return children.ToArray();
    }

    RenderTexture frameOld;
    GraphicsBuffer buffer;

    int NumRenderedFrames = 0;

    // called after a camera finishes rendering into the source texture
    bool clr = false;
    void OnRenderImage(RenderTexture source, RenderTexture target)
    {
        Camera cam = Camera.current;
        if(cam.name != "SceneCamera" || useShaderInSceneView)
        {
            clr = true;
            if (!frameOld || !framestackingMaterial || !rayTracingMaterial)
            {
                frameOld = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0);
                rayTracingMaterial = new Material(rayTracingShader);
                framestackingMaterial = new Material(framestackingShader);
            }
            else
                framestackingMaterial.SetTexture("TextureOld", frameOld);

            UpdateCameraParams(cam);

            RenderTexture frameNew = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0);
            // Render a new frame into frameNew

            rayTracingMaterial.SetInteger("Frame", NumRenderedFrames+1);
            Graphics.Blit(null, frameNew, rayTracingMaterial);
            // Do framestacking and render into frameOld    
            framestackingMaterial.SetInteger("NumRenderedFrames", NumRenderedFrames);
            Graphics.Blit(frameNew, frameOld, framestackingMaterial);
            // Blit frameOld into the target texture
            Graphics.Blit(frameOld, target);
            NumRenderedFrames++;
        } else
        {
            Graphics.Blit(source, target);
        }

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

        rayTracingMaterial.SetInteger("NumSpheres", spheres.Length);
        rayTracingMaterial.SetInteger("MaxBounces", maxBounces);
        rayTracingMaterial.SetInteger("SamplesPerPixel", samples);

        rayTracingMaterial.SetBuffer("SpheresBuffer", buffer);
    }
}
