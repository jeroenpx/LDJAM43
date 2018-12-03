using UnityEngine;
using System.Collections;

public interface MoveableObject
{
    event ChangeEvent exit;

    int X
    {
        get; set;
    }
    int Y
    {
        get; set;
    }
    int Orientation
    {
        get; set;
    }
    int MoveDirection {
        get; set;
    }
    int Level {
        get; set;
    }
    bool Waiting
    {
        get; set;
    }

    void BeforeStep();

    int GetMaxStepUp();

    int GetMaxStepDown();


    // Animation
    void InitAnim();
    void AnimSetRotation(Quaternion rot);
    void AnimMove(Vector3 from, Vector3 to, float percent);

    GameObject GameObj { get; }
}
