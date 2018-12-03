using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hill : MonoBehaviour, ObstacleObject
{
    public int direction;

    public bool CanEnter(MoveableObject who, int orientation)
    {
        return who.Orientation!=-1 && who.Orientation == orientation && (direction + orientation)%2 == 0;
    }

    public GameObject GameObj { get { return gameObject; } }
}
