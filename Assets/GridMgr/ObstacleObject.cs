using UnityEngine;
using System.Collections;

public interface ObstacleObject
{
    bool CanEnter(MoveableObject who, int orientation);

    GameObject GameObj { get; }
}
