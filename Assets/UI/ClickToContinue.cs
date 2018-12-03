using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClickToContinue : MonoBehaviour {

    private int nextScene = 0;
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            SceneManager.LoadScene(nextScene);
        }
	}
}
