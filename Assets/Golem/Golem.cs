using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Golem : MonoBehaviour, MoveableObject
{

    public int x;
    public int y;
    public int level;
    public bool waiting;
    public int orientation;
    public int movedir;
    public Animator anim;

    public GameObject portalEffect;
    public GameObject blockOfIce;

    public event ChangeEvent exit;

    private bool exiting = false;
    [SerializeField]
    private bool iced = false;

    // Use this for initialization
    void Start () {
        Update();
        if (iced) {
            anim.SetBool("Iced", true);
        }
    }
	
	// Update is called once per frame
	void Update () {
        int r = Random.Range(0, 2);
        anim.SetInteger("NextIdle", r);
    }

    //
    // MoveableObject implementation
    //
    public int X {
        get { return this.x; }
        set { this.x = value; }
    }
    public int Y
    {
        get { return this.y; }
        set { this.y = value; }
    }
    public int Level
    {
        get { return this.level; }
        set { this.level = value; }
    }
    public bool Waiting
    {
        get { return this.waiting; }
        set { this.waiting = value; }
    }
    public int Orientation
    {
        get { return this.orientation; }
        set { this.orientation = value; }
    }
    public int MoveDirection
    {
        get { return this.movedir; }
        set { this.movedir = value; }
    }

    public void BeforeStep()
    {
        // -1 for box (don't move)
        MoveDirection = Orientation;
        Waiting = false;
    }

    public int GetMaxStepUp() {
        // 0 for box
        if (iced)
        {
            return 0;
        }
        else
        {
            return 1;
        }
    }

    public int GetMaxStepDown() {
        // 20 for box
        if (iced)
        {
            return 20;
        }
        else
        {
            return 1;
        }
    }

    public void InitAnim() {
        anim.SetBool("Running", Orientation == MoveDirection);
    }

    public void AnimMove(Vector3 from, Vector3 to, float percent) {
        transform.localPosition = Vector3.Lerp(from, to, percent);
    }

    public void AnimSetRotation(Quaternion rot)
    {
        transform.localRotation = rot;
    }

    public void Exit() {
        if (!exiting)
        {
            MoveDirection = -1;
            Waiting = true;
            exiting = true;
            GameObject.Instantiate(portalEffect, transform.position, transform.rotation);
            StartCoroutine(TeleportAway());
        }
    }

    public void Iced(bool loadedLikeThis) {
        if (!iced)
        {
            iced = true;
            GameObject ice = GameObject.Instantiate(blockOfIce, transform);
            anim.SetBool("Iced", true);
            Orientation = -1;
            if (!loadedLikeThis) {
                Waiting = true;// wait a frame
                // Play animation!
            }
        }
    }

    IEnumerator TeleportAway()
    {
        yield return new WaitForSeconds(0.5f);
        if (exit != null)
        {
            exit();// Remove myself from the simulation for the next frame...
        }
        yield return new WaitForSeconds(0.5f);
        GameObject.Destroy(gameObject);
    }

    public GameObject GameObj { get { return gameObject; } }
}
