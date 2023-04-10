using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTest : MonoBehaviour
{
    [SerializeField] Vector2 pointCount = new Vector2(16, 9);
    [SerializeField] Color pointColor = new Color(255, 255, 255);
    [SerializeField] float pointRad = 0.1f;
    [SerializeField] Color arrowColor = new Color(155, 0, 0);
    [SerializeField] float arrowMag = 1f;

    void OnDrawGizmosSelected()
    {
        CameraRayTest();
    }

    void CameraRayTest()
    {
        Camera cam = Camera.main;
        Transform camT = cam.transform;

        float planeH = cam.nearClipPlane * Mathf.Tan(cam.fieldOfView * .5f * Utils.Deg2Rad) * 2;
        float planeW = planeH * cam.aspect;

        Vector3 bottomLeftLocal = new Vector3(-planeW / 2, -planeH / 2, cam.nearClipPlane);

        for(int x = 0; x < pointCount.x; x++)
        {
            for (int y = 0; y < pointCount.y; y++)
            {
                float tx = x / (pointCount.x - 1f);
                float ty = y / (pointCount.y - 1f);

                Vector3 pointLocal = bottomLeftLocal + new Vector3(planeW * tx, planeH * ty);
                Vector3 point = camT.position + camT.right * pointLocal.x + camT.up * pointLocal.y + camT.forward * pointLocal.z;

                DrawPoint(point);
                DrawArrow(camT.position, point);
            }
        }
    }

    void DrawPoint(Vector3 point)
    {
        Gizmos.color = pointColor;
        Gizmos.DrawSphere(point, pointRad);
    }

    void DrawArrow(Vector3 origin, Vector3 pointAt)
    {
        Vector3 to = pointAt - origin;

        Gizmos.color = arrowColor;
        Gizmos.DrawRay(origin + to.normalized * 0.05f, to.normalized * arrowMag);
    }
}
