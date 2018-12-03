using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Exit : MonoBehaviour, GroundObject {
    
    //
    // GroundObject implementation
    //

    public event ChangeEvent change;

    public void PressureActivate() {
    }
    public void PressureRelease() {
    }
    public void PressureTick(MoveableObject standingOnIt)
    {
        if (standingOnIt is Golem) {
            ((Golem)standingOnIt).Exit();
        }
    }
    public void NoPressureTick() {

    }

    public GameObject GameObj { get { return gameObject; } }
}
