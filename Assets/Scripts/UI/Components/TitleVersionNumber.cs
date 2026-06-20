using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleVersionNumber : MonoBehaviour
{
    public string prefix = "Version ";

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<TMPro.TextMeshProUGUI>().text = prefix + Application.version;
    }
}