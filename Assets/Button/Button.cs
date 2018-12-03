using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button : MonoBehaviour, GroundObject {

    public List<GameObject> objects = new List<GameObject>();

    private float initPosition;

    void Start()
    {
        initPosition = transform.position.y;
        UpdatePress(false);
    }

    void UpdatePress(bool pressed) {
        transform.position = new Vector3(transform.position.x, initPosition + (!pressed?0.09f:0f), transform.position.z);
    }

    //
    // GroundObject implementation
    //

    public event ChangeEvent change;

    public void PressureActivate() {
        UpdatePress(true);
        foreach (GameObject obj in objects) {
            obj.GetComponent<ActivateableObject>().ActivateAction();
        }
    }
    public void PressureRelease() {
        UpdatePress(false);
        foreach (GameObject obj in objects)
        {
            obj.GetComponent<ActivateableObject>().DeactivateAction();
        }
    }
    public void PressureTick(MoveableObject standingOnIt)
    {
        
    }
    public void NoPressureTick() {

    }

    public GameObject GameObj { get { return gameObject; } }
}
