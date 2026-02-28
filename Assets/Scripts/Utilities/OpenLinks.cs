using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenLinks : MonoBehaviour
{
    // this is just to call on buttons OnClick() events
    public void OpenLink(string url)
    {
        Application.OpenURL(url);
    }
}