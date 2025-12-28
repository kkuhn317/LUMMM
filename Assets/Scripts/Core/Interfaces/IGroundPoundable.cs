using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Interface for objects that can be ground pounded (that are solid) (e.g. Giant Thwomp after falling back)
public interface IGroundPoundable
{
    public void OnGroundPound(MarioMovement player);
}
