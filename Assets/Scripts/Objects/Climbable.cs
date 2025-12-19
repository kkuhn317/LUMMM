using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Climbable : MonoBehaviour
{
    public enum ClimbMethod { Front, Side };
    public ClimbMethod climbMethod = ClimbMethod.Front;
    public bool isLadder = false;   // For front climbing only
    public float width = 0.5f;   // For side climbing only
    public float climbSpeed = 4f;
}
