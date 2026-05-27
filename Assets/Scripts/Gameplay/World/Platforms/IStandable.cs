using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IStandable
{
    void OnStandEnter(MarioCore mario);
    void OnStandExit(MarioCore mario);
}