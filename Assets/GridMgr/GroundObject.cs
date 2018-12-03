using UnityEngine;
using System.Collections;

public delegate void ChangeEvent();

public interface GroundObject
{
    event ChangeEvent change;

    void PressureActivate();
    void PressureRelease();
    void PressureTick(MoveableObject standingOnIt);
    void NoPressureTick();

    GameObject GameObj { get; }
}
