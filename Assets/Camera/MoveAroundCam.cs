using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAroundCam : MonoBehaviour {

    public Transform rotationPivot;
    public float moveSpeedBoundaryMode;
    public float moveSpeedDragMode;
    public float smoothTime;
    public bool boundaryMode = false;
    public float cancelMagnitude;

    private Vector3 lastMousePos = Vector3.zero;
    private Vector3 curSpeed;
    private Vector3 curAcceleration;

    private GameObject lastClicked;
    private GameObject active;

    // Update is called once per frame
    void Update ()
    {
        Vector3 mouse = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(mouse);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                lastClicked = hit.collider.gameObject;
            }
            else {
                lastClicked = gameObject;// Just pass a non null object... (for the cancel to work correctly)
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (lastClicked != null)
            {
                lastClicked.SendMessage("MsgDoAction", SendMessageOptions.DontRequireReceiver);
                if (active != null)
                {
                    active.SendMessage("MsgDoCancel", SendMessageOptions.DontRequireReceiver);
                }
                active = lastClicked;
                lastClicked = null;
            }
        }


        //
        // Mouse Movement
        //
        Vector3 right = rotationPivot.right;
        Vector3 forward = Vector3.Cross(right, Vector3.up);
        Vector3 destSpeed = Vector3.zero;
        float pixelsSide = Screen.height / 8f;// magic number: 1/8 of the screen height

        if (boundaryMode)
        {
            float xBoundary = pixelsSide / (float)Screen.width;
            float yBoundary = pixelsSide / (float)Screen.height;
            float xPercent = mouse.x / (float)Screen.width;
            float yPercent = mouse.y / (float)Screen.height;
            int goRight = 0;
            int goUp = 0;
            if (xPercent < xBoundary)
            {
                goRight = -1;
            }
            if (1 - xPercent < xBoundary)
            {
                goRight = 1;
            }
            if (yPercent < yBoundary)
            {
                goUp = -1;
            }
            if (1 - yPercent < yBoundary)
            {
                goUp = 1;
            }
            destSpeed = forward * goUp * moveSpeedBoundaryMode + right * goRight * moveSpeedBoundaryMode;
        } else {
            if (Input.GetKeyDown(KeyCode.Mouse0)) {
                lastMousePos = mouse;
            }
            if (Input.GetKey(KeyCode.Mouse0))
            {
                Vector3 delta = mouse - lastMousePos;
                float xPercent = delta.x / pixelsSide;
                float yPercent = delta.y / pixelsSide;
                destSpeed = -forward* yPercent * moveSpeedDragMode + -right * xPercent * moveSpeedDragMode;
                if(destSpeed.magnitude>cancelMagnitude) {
                        if (lastClicked != null)
                        {
                            lastClicked = null;
                        }
                }
            }
            lastMousePos = mouse;
        }

        curSpeed = Vector3.SmoothDamp(curSpeed, destSpeed, ref curAcceleration, smoothTime);

        transform.position = transform.position + curSpeed * Time.deltaTime;

        /*Debug.DrawRay(transform.position, forward, Color.red);
        Debug.DrawRay(transform.position, right, Color.green);

        Debug.Log("right:"+ goRight + ", up:"+goUp);*/
    }
}
