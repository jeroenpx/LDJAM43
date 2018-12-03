using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour, ObstacleObject {

    public bool CanEnter(MoveableObject who, int orientation)
    {
        return false;
    }

    public GameObject GameObj { get { return gameObject; } }
}
