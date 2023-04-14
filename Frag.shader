Shader "Hidden/Frag"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
    }
    SubShader
    {

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            float3 ViewParams;
            float4x4 CamLocalToWorldMatrix;
            int MaxBounces;
            int SamplesPerPixel;
            int Frame;
            bool UseProgressiveRendering;

            struct Ray {
                float3 o;
                float3 d;
            };

            struct Material {
                float4 color;
                float4 emission;
            };

            struct Hit {
                bool did;
                float d;
                float3 p;
                float3 n;
                Material material;
            };

            struct Sphere {
                float x, y, z;
                float radius;
                Material material;
            };
            
            float RandomValue(inout uint state) {
                state = state*747796405 + 2891336453;

                uint result = ((state>>((state>>28) + 4))^state)*277803737;
                result = (result>>22)^result;

                return result / 4294967295.0;
            }

            float RandomValueNormalDist(inout uint state) {
                float theta = 2*3.1415926*RandomValue(state);
                float rho = sqrt(-2*log(RandomValue(state)));
                
                return rho*cos(theta);
            }
            float3 RandomDirection(inout uint state) {
                float x = RandomValueNormalDist(state);
                float y = RandomValueNormalDist(state);
                float z = RandomValueNormalDist(state);

                return normalize(float3(x,y,z));
            }
            float3 RandomHemisphere(float3 n, inout uint state) {
                float3 dir = RandomDirection(state);

                return dir*sign(dot(n, dir));
            }

            Hit RaySphere(Ray ray, float3 sphereP, float sphereR) {
                Hit hit = (Hit)0; // Snazzy
                
                float3 fromSphere = ray.o - sphereP;

                float a = dot(ray.d, ray.d);
                float b = 2*dot(fromSphere, ray.d);
                float c = dot(fromSphere, fromSphere) - sphereR*sphereR;

                float dscrim = b*b - 4*a*c;

                if(dscrim >= 0) { // if there is at least 1 solution
                    float dst = -(b + sqrt(dscrim)) / (2*a);

                    if(dst >= 0) { // if there are solutions in front of us
                        hit.did = true;
                        hit.d = dst;
                        hit.p = ray.o + ray.d * dst;
                        hit.n = normalize(hit.p - sphereP);
                    }
                }

                return hit;
            }

            StructuredBuffer<Sphere> SpheresBuffer;
            
            int NumSpheres;

            Hit RayCollisions(Ray ray) {
                Hit closest = (Hit)0; // Snazzy
                closest.d = 1.#INF;
                
                for(int i = 0; i < NumSpheres; i++) {
                    Sphere sphr = SpheresBuffer[i];
                    Hit hit = RaySphere(ray, float3(sphr.x, sphr.y, sphr.z), sphr.radius);

                    if(hit.did && hit.d < closest.d) {
                        closest = hit;
                        closest.material = sphr.material;
                    }
                }

                return closest;
            }


            /* Taken from : https://blog.demofox.org/2020/05/25/casual-shadertoy-path-tracing-1-basic-camera-diffuse-emissive/
              - When a ray hits an object, emissive*throughput is added to the pixel’s color.
              - When a ray hits an object, the throughput is multiplied by the object’s albedo, which affects the color of future emissive lights.
              - When a ray hits an object, a ray will be reflected in a random direction and the ray will continue
                * I have chosen to use a normal distribution for calculating my random directions.
              - We will terminate when a ray misses all objects, or when N ray bounces have been reached. 
            */
            float3 Trace(Ray ray, inout uint state) {
                float3 incomingLight = 0;
                float3 rayCol = 1;

                for(int i = 0; i <= MaxBounces; i++) {
                    Hit hit = RayCollisions(ray);

                    if(hit.did) {
                        ray.o = hit.p;
                        ray.d = normalize(hit.n + RandomDirection(state)); // Cosine weighted normal distribution normal random direction

                        Material mat = hit.material;
                        float3 emittedLight = mat.emission.xyz * mat.emission.w;

                        incomingLight += emittedLight * rayCol;
                        rayCol *= mat.color;
                    } else {
                        break;
                    }
                }

                return incomingLight;
            }

            float4 frag (v2f i) : SV_Target
            {
                uint2 numPixels = _ScreenParams.xy;
                uint2 pixelCoord = i.uv*numPixels;
                uint pixIndex = pixelCoord.y*numPixels.x + pixelCoord.x;
                uint rngState = pixIndex + Frame*719393;

                float3 vpLocal = float3(i.uv - 0.5, 1)*ViewParams; // set origin at center of screen and make the viewport correct
                float3 vpWorld = mul(CamLocalToWorldMatrix, float4(vpLocal, 1));

                Ray ray;
                ray.o = _WorldSpaceCameraPos;
                ray.d = normalize(vpWorld - ray.o);

                float3 totalIncominglight = 0;
                for(int rayInd = 0; rayInd < SamplesPerPixel; rayInd++)
                    totalIncominglight += Trace(ray, rngState);

                float3 newPixelColor = totalIncominglight / SamplesPerPixel; 

                if(UseProgressiveRendering) {
                    float3 oldPixelColor = tex2D(_MainTex, i.uv).rgb;

                    float w = 1.0 / Frame;
                    // linearly interpolate between the last frame and the current.
                    // frame 1: old*0      + new*1
                    // frame 2: old*0.5    + new*0.5
                    // frame 3: old*0.6667 + new*0.333
                    // frame 4: old*0.75   + new*0.25
                    // frame 5: old*0.8    + new*0.2
                    // frame 6: old*0.833  + new*0.1667
                    // and so on. this means that over time new frames will contribute less and less.
                    // this is equivalent to lerp(oldPixelcolor, newPixelColor, 1.0 / Frame)
                    return float4(oldPixelColor*(1 - w) + newPixelColor*w, 1);
                }
                return float4(newPixelColor, 1);
            }
            ENDCG
        }
    }
}
