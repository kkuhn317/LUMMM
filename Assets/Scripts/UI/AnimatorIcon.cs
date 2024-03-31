using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorIcon : MonoBehaviour
{
    public LevelSelectionManager.MarioAnimator marioAnimator;

    private void Start()
    {
        LevelSelectionManager.Instance.AddAnimatorIcon(this);

        // Make only level up show up by default
        if (marioAnimator != LevelSelectionManager.MarioAnimator.LevelUp)
        {
            gameObject.SetActive(false);
        }
    }
}
