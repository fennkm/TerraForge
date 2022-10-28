using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using VectorSwizzling;

public class CameraController : MonoBehaviour
{
    public float panSpeed;
    public float xLim;
    public float yLim;
    public float rotateSpeed;
    [Range(0, 1)]
    public float zoomFactor;
    public float minDist;
    public float maxDist;

    private Camera cam;
    private Collider col;

    private float maxHeight;
    private float lerpVal;
    private Vector3 skyPoint;
    private Vector3 groundPoint;
    private float clipDist;

    private Vector2[] mouseDownPos = new Vector2[3];
    private Vector3 panStartPos;
    private Vector3 orbitStartPos;
    private Vector2 orbitStartRots;
    private Transform XGimble;
    private Transform YGimble;

    private int mode;

    public TerrainController terrian;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        col = GetComponent<Collider>();

        clipDist = (transform.position - cam.ScreenToWorldPoint(new Vector3(0, 0, cam.nearClipPlane))).magnitude;
        maxHeight = transform.position.y;

        skyPoint = transform.position;
        groundPoint = getGroundPoint();
        lerpVal = 1;
    }

    // Update is called once per frame
    void Update()
    {

        if (mode == 1 && !Input.GetMouseButton(1))
            mode = 0;
        else if (mode == 2 && !Input.GetMouseButton(2))
            mode = 0;

        if (mode == 0)
        {
            if (Input.GetMouseButton(1))
            {
                mouseDownPos[1] = Input.mousePosition;
                orbitStartPos = transform.position;
                orbitStartRots = new Vector2(transform.eulerAngles.x, transform.eulerAngles.y);
                mode = 1;
            }
            if (Input.GetMouseButton(2))
            {
                mouseDownPos[2] = Input.mousePosition;
                panStartPos = skyPoint;
                mode = 2;
            }
        }

        if (Input.GetMouseButton(1) && mode == 1)
        {
            Vector2 delta = Input.mousePosition.xy() - mouseDownPos[1];

            float angleY = delta.x * rotateSpeed;
            float angleX = Mathf.Clamp(-delta.y * rotateSpeed, 5 - orbitStartRots.x, 85 - orbitStartRots.x);
            Debug.Log(angleX);

            Vector3 groundToCam = (orbitStartPos - groundPoint);//.x0z().magnitude;

            Vector3 newPos =  
                groundPoint.x0z() +
                Quaternion.Euler(0f, angleY, 0f) * groundToCam.x0z() +
                transform.position._0y0();

            groundToCam = (orbitStartPos._0y0() + newPos.x0z() - groundPoint);
            
            float heading = Vector3.Angle(Vector3.back, groundToCam.x0z());
            if (groundToCam.x < 0)
                heading = 360 - heading;
            
            newPos = 
                groundPoint + 
                Quaternion.Euler(
                    angleX * Mathf.Cos(heading * Mathf.Deg2Rad), 
                    0f, 
                    angleX * Mathf.Sin(heading * Mathf.Deg2Rad)) * groundToCam;

            groundToCam = newPos - groundPoint;

            RaycastHit ray;

            if (Physics.Linecast(groundPoint, newPos, out ray))
                newPos = ray.point - groundToCam.normalized * clipDist * 2;

            transform.position = newPos;
                
            transform.LookAt(groundPoint, Vector3.up);

            skyPoint = getSkyPoint();

            lerpVal = (transform.position - groundPoint).magnitude / (skyPoint - groundPoint).magnitude;
        }  
        else if (Input.GetMouseButton(2) && mode == 2)
        {
            Vector3 skyToCam = skyPoint - transform.position;
            float viewAngle = 
                Vector3.Angle(Vector3.back, skyPoint.x0z() - groundPoint.x0z());

            if (skyPoint.x > groundPoint.x)
                viewAngle = 360f - viewAngle;

            Vector2 delta = Input.mousePosition.xy() - mouseDownPos[2];
            skyPoint = panStartPos - Quaternion.Euler(0f, viewAngle, 0f) * delta.x0y() * panSpeed;

            groundPoint = getGroundPoint();

            if (groundPoint.x > xLim )
                skyPoint += (xLim - groundPoint.x).x00();
            else if (groundPoint.x < -xLim)
                skyPoint += (-xLim - groundPoint.x).x00();

            if (groundPoint.z > yLim )
                skyPoint += (yLim - groundPoint.z)._00x();
            else if (groundPoint.z < -yLim)
                skyPoint += (-yLim - groundPoint.z)._00x();

            groundPoint = getGroundPoint();

            Vector3 newCamPos = skyPoint - skyToCam;

            transform.position = newCamPos;

            if (Physics.CheckSphere(transform.position, clipDist))
            {
                lerpVal = 0.1f;
                transform.position = Vector3.Lerp(groundPoint, skyPoint, lerpVal);
            }
            else
                lerpVal = (transform.position - groundPoint).magnitude / (skyPoint - groundPoint).magnitude;
        }

        if (mode == 0 && Input.mouseScrollDelta.y != 0)
        {
            lerpVal *= (Mathf.Sign(Input.mouseScrollDelta.y) == 1 ? 1 - zoomFactor : 1 / (1 - zoomFactor));

            lerpVal = Mathf.Max(0.1f, lerpVal);//Mathf.Clamp(lerpVal, 0.1f, 1f);

            transform.position = Vector3.LerpUnclamped(groundPoint, skyPoint, lerpVal);
        }
    }

    private Vector3 getGroundPoint()
    {
        RaycastHit hit;

        Physics.Raycast(skyPoint, transform.forward, out hit);

        return hit.point;
    }

    private Vector3 getSkyPoint()
    {
        float angle = Vector3.Angle(Vector3.up, -transform.forward);

        return groundPoint + 
            -transform.forward * (maxHeight - groundPoint.y) / Mathf.Cos(Mathf.Deg2Rad * angle);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawSphere(groundPoint, .5f);

        Gizmos.color = Color.green;

        Gizmos.DrawSphere(skyPoint, .5f);
    }
}
