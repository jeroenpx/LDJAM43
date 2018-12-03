using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeepOrientation : MonoBehaviour {

    private void Start()
    {
        Update();
    }

    void Update () {
        transform.rotation = Quaternion.identity;
	}
}
