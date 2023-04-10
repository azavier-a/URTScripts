Shader "Hidden/Frag"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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
            int Frame   ;

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

            float3 Trace(Ray ray, inout uint state) {
                float3 incomingLight = 0;
                float3 rayCol = 1;

                for(int i = 0; i <= MaxBounces; i++) {
                    Hit hit = RayCollisions(ray);

                    if(hit.did) {
                        ray.o = hit.p;
                        ray.d = RandomHemisphere(hit.n, state);

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

                for(int rayInd = 0; rayInd < SamplesPerPixel; rayInd++) {
                    totalIncominglight += Trace(ray, rngState);
                }

                float3 pixelColor = totalIncominglight / SamplesPerPixel;
                return float4(pixelColor, 1);
            }
            ENDCG
        }
    }
}
