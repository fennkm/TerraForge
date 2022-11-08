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
    public float minZoom;
    public float maxZoom;

    private Camera cam;
    private Vector3 groundPoint;
    private float clipDist;

    private Vector2[] mouseDownPos = new Vector2[3];
    private Vector3 panStartPos;
    private Vector3 orbitStartPos;
    private float orbitStartPitch;

    private int mode;

    public TerrainUIController terrainUIController;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();

        clipDist = (transform.position - cam.ScreenToWorldPoint(new Vector3(0, 0, cam.nearClipPlane))).magnitude;

        groundPoint = GetGroundPoint();
    }

    // Update is called once per frame
    void Update()
    {

        if (mode == 1 && !Input.GetMouseButton(1))
            mode = 0;
        else if (mode == 2 && !Input.GetMouseButton(2))
            mode = 0;
        
        if (mode == 0)
            terrainUIController.ShowCursor();
        else
            terrainUIController.HideCursor();

        if (mode == 0)
        {
            if (Input.GetMouseButton(1))
            {
                groundPoint = GetGroundPoint();
                mouseDownPos[1] = Input.mousePosition;
                orbitStartPos = transform.position;
                orbitStartPitch = transform.eulerAngles.x;
                mode = 1;
            }
            if (Input.GetMouseButton(2))
            {
                mouseDownPos[2] = Input.mousePosition;
                panStartPos = transform.position;
                mode = 2;
            }
        }

        if (Input.GetMouseButton(1) && mode == 1)
        {
            Vector2 delta = Input.mousePosition.xy() - mouseDownPos[1];

            float angleY = delta.x * rotateSpeed;
            float angleX = Mathf.Clamp(-delta.y * rotateSpeed, 5 - orbitStartPitch, 85 - orbitStartPitch);

            Vector3 newPos =  
                groundPoint.x0z() +
                Quaternion.Euler(0f, angleY, 0f) * (orbitStartPos - groundPoint) +
                transform.position._0y0();

            Vector3 referenceVec = orbitStartPos._0y0() + newPos.x0z() - groundPoint;
            
            float heading = Vector3.Angle(Vector3.back, referenceVec.x0z());
            if (referenceVec.x < 0)
                heading = 360 - heading;
            
            newPos = 
                groundPoint + 
                Quaternion.Euler(
                    angleX * Mathf.Cos(heading * Mathf.Deg2Rad), 
                    0f, 
                    angleX * Mathf.Sin(heading * Mathf.Deg2Rad)) * referenceVec;

            transform.position = newPos;

            while (Physics.CheckSphere(transform.position, clipDist))
            {
                transform.position += transform.up * clipDist;
                orbitStartPos += transform.up * clipDist;
                orbitStartPitch = transform.eulerAngles.x - angleX;
            }

            transform.LookAt(groundPoint, Vector3.up);
        }  
        else if (Input.GetMouseButton(2) && mode == 2)
        {
            float viewAngle = 
                Vector3.Angle(Vector3.back, transform.position.x0z() - groundPoint.x0z());

            if (transform.position.x > groundPoint.x)
                viewAngle = 360f - viewAngle;

            Vector2 delta = Input.mousePosition.xy() - mouseDownPos[2];
            transform.position = panStartPos - Quaternion.Euler(0f, viewAngle, 0f) * delta.x0y() * panSpeed;

            groundPoint = GetGroundPoint();

            if (groundPoint.x > xLim )
                transform.position += (xLim - groundPoint.x).x00();
            else if (groundPoint.x < -xLim)
                transform.position += (-xLim - groundPoint.x).x00();

            if (groundPoint.z > yLim )
                transform.position += (yLim - groundPoint.z)._00x();
            else if (groundPoint.z < -yLim)
                transform.position += (-yLim - groundPoint.z)._00x();

            while (Physics.CheckSphere(transform.position, clipDist))
            {
                transform.position += transform.up * clipDist;
                panStartPos += transform.up * clipDist;
            }

            groundPoint = GetGroundPoint();
        }

        if (mode == 0 && !Input.GetKey(KeyCode.LeftShift) && Input.mouseScrollDelta.y != 0)
        {
            groundPoint = GetGroundPoint();

            Vector3 groundToCam = transform.position - groundPoint;

            Vector3 newPos = groundPoint + groundToCam * (1 + -Input.mouseScrollDelta.y * zoomFactor);

            groundToCam = newPos - groundPoint;

            if (!Physics.CheckSphere(newPos, clipDist * 2f) &&
                groundToCam.magnitude >= minZoom &&
                groundToCam.magnitude <= maxZoom)
                transform.position = newPos;
        }
    }

    private Vector3 GetGroundPoint()
    {
        RaycastHit hit;

        Physics.Raycast(transform.position, transform.forward, out hit);

        return hit.point;
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawSphere(groundPoint, .5f);

        Gizmos.color = Color.green;

        Gizmos.DrawLine(groundPoint, transform.position);
    }
}
