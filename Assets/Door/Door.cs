using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour, ObstacleObject, ActivateableObject
{

    public bool startOpen;
    public float openSize;
    public float openSpeed;

    private bool open;
    private float openPercent;

    private float initialY;

    void Start() {
        initialY = transform.position.y;
    }

    void Update()
    {
        bool moving = false;
        if (open && openPercent < 1f) {
            openPercent += Time.deltaTime * openSpeed;
            moving = true;
        }
        if (!open && openPercent > 0f)
        {
            openPercent -= Time.deltaTime * openSpeed;
            moving = true;
        }
        if (openPercent > 1f) {
            openPercent = 1f;
        }
        if (openPercent <0f)
        {
            openPercent = 0f;
        }
        if (moving) {
            // TODO: play sound + camera shake
        }
        transform.position = new Vector3(transform.position.x, initialY + openPercent*openSize, transform.position.z);
    }

    public void ActivateAction() {
        open = !startOpen;
    }

    public void DeactivateAction()
    {
        open = startOpen;
    }

    public bool CanEnter(MoveableObject who, int orientation)
    {
        return openPercent > .95f;
    }

    public GameObject GameObj { get { return gameObject; } }
}
