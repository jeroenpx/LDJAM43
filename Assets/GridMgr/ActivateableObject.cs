using UnityEngine;
using System.Collections;


public interface ActivateableObject
{
    void ActivateAction();

    void DeactivateAction();

    GameObject GameObj { get; }
}
