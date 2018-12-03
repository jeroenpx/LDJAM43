using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleSpawner: MonoBehaviour {

    public GameObject bubblePrefab;
    public List<string> availableActions;
    public float radius;
    public ConfigureTile configureTile;

    private List<GameObject> bubbles = new List<GameObject>();

    private bool doSpawn = false;

    /**
     * Set the actions
     */
    public void setActions(List<string> actions) {
        this.availableActions = actions;
    }

    /**
     * Spawn the bubbles
     */
    public void Spawn() {
        if (bubbles.Count == 0)
        {
            doSpawn = true;
        } else
        {
            Cancel();
        }
    }

    /**
     * Called from Bubble!!
     */
    public void Select(string action) {
        configureTile.Select(action);
        Cancel();
    }

    /**
     * Cancel the bubbles
     */
    public void Cancel() {
        foreach (GameObject bub in bubbles)
        {
            bub.GetComponent<Bubble>().KillBubble();
        }
        bubbles = new List<GameObject>();
    }

    // Update is called once per frame
    void Update () {
        if (doSpawn) {
            doSpawn = false;
            if (bubbles.Count == 0)
            {
                int i = 0;
                foreach (string availableAction in availableActions)
                {
                    GameObject newBubble = GameObject.Instantiate(bubblePrefab);
                    newBubble.transform.parent = this.transform;
                    newBubble.transform.localRotation = Quaternion.identity;
                    bubbles.Add(newBubble);
                    Bubble bubble = newBubble.GetComponent<Bubble>();
                    bubble.SetOrigin(this, ((float)i)/(availableActions.Count+1));
                    bubble.SetAction(availableAction);
                    float angle = (float)i / availableActions.Count * Mathf.PI * 2f + Mathf.PI / 2f;
                    float posX = Mathf.Cos(angle);
                    float posY = Mathf.Sin(angle);
                    bubble.transform.localPosition = new Vector3(posX, 0, posY) * radius;
                    i++;
                }
            }
        }


        Vector3 lookAt = -Camera.main.ViewportPointToRay(Vector3.zero).direction;
        transform.rotation = Quaternion.LookRotation(lookAt, Vector3.up)* Quaternion.LookRotation(Vector3.right, Vector3.forward);
	}
}
