using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bubble : MonoBehaviour {

    private BubbleSpawner spawner;
    private string action;
    private bool enabled = true;

    public float timeToGoRound = 2f;
    public float upwardsHeight = 1f;

    public List<string> actionMap;
    public List<GameObject> arrowMap;

    private Vector3 pos;
    private float indexPercent;

    void Start() {
        pos = transform.localPosition;
        Update();
    }

	// Update is called once per frame
	void Update () {
        transform.localPosition = pos;
        transform.position = transform.position + Vector3.up * upwardsHeight * Mathf.Sin((Time.time / timeToGoRound + indexPercent) * Mathf.PI * 2);
    }

    // Called from BubbleSpawner
    public void SetOrigin(BubbleSpawner spawner, float indexPercent) {
        this.spawner = spawner;
        this.indexPercent = indexPercent;
    }

    // Called from BubbleSpawner
    public void SetAction(string action) {
        this.action = action;
        GameObject arrow = GameObject.Instantiate(arrowMap[actionMap.IndexOf(action)]);
        arrow.GetComponent<KeepOrientation>().enabled = true;
        arrow.transform.parent = transform;
        arrow.transform.localPosition = Vector3.zero;
    }


    // Called from Camera...
    void MsgDoAction() {
        if (enabled)
        {
            spawner.Select(action);
        }
    }

    void MsgDoCancel() {
        if (enabled)
        {
            spawner.Cancel();
        }
    }

    // Called from BubbleSpawner...
    public void KillBubble() {
        enabled = false;
        GameObject.Destroy(gameObject);
    }
}
