using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigureTile : MonoBehaviour, GroundObject {

    public BubbleSpawner spawner;

    public List<string> actionMap;
    public List<GameObject> arrowMap;

    public Transform arrowParent;

    private GameObject current;
    [SerializeField]
    private int setOrientation = -2;

    private bool occupied;
    private MoveableObject lastStandingOn;

    [SerializeField]
    private bool disabled;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void MsgDoAction() {
        if (!disabled)
        {
            spawner.Spawn();
        }
    }

    void MsgDoCancel() {
        if (!disabled)
        {
            spawner.Cancel();
        }
    }

    public void DisableConfig() {
        disabled = true;
        for (int i = transform.childCount-1; i >= 0; i--) {
            Transform t = transform.GetChild(i);
            if (t != arrowParent) {
                GameObject.DestroyImmediate(t.gameObject);
            }
        }
    }

    public void SetAvailableOptions(List<string> options) {
        spawner.setActions(options);
    }

    public void Select(string action) {
        if (current) {
            GameObject.Destroy(current);
            current = null;
        }
        setOrientation = actionMap.IndexOf(action);
        if (setOrientation == -1) {
            setOrientation = -2;
        }
        if (setOrientation == 4)
        {
            // wait action
            setOrientation = -1;
        }
        else if (setOrientation == 5) {
            // snow action
            setOrientation = -3;
        }
        if (setOrientation != -2)
        {
            GameObject newArrow = GameObject.Instantiate(arrowMap[actionMap.IndexOf(action)]);
            newArrow.transform.parent = arrowParent;
            newArrow.transform.localPosition = Vector3.zero;
            current = newArrow;
        }

        if (change!=null) {
            change();
        }
    }

    //
    // GroundObject implementation
    //

    public event ChangeEvent change;

    public void PressureActivate() {
        occupied = true;
        Debug.Log("Occupied!");
    }
    public void PressureRelease() {
        occupied = false;
        Debug.Log("Released!");
    }
    public void PressureTick(MoveableObject standingOnIt)
    {
        Debug.Log("Tick!");
        if (lastStandingOn == standingOnIt) {
            return;
        }
        if (setOrientation != -2)
        {
            Debug.Log("We have action!");
            // We have an action!
            if (standingOnIt.MoveDirection != -1)
            {
                Debug.Log("We have a player!");
                // We have a player!
                if (setOrientation == -3)
                {
                    // snow action
                    ((Golem)standingOnIt).Iced(false);
                    Select("nothing");
                } else { 
                    if (setOrientation != -1)
                    {
                        Debug.Log("Setting orientation! "+setOrientation);
                        standingOnIt.Orientation = setOrientation;
                    } else {
                        standingOnIt.Waiting = true;
                    }
                    Debug.Log("Setting move dir! " + setOrientation);
                    standingOnIt.MoveDirection = setOrientation;
                }
            }
        }
        lastStandingOn = standingOnIt;
    }
    public void NoPressureTick() {
        lastStandingOn = null;
    }

    public GameObject GameObj { get { return gameObject; } }
}
