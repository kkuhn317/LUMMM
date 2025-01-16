using UnityEngine;
using System.Collections;
using ArabicSupport;
using TMPro;

public class SetArabicTextExample : MonoBehaviour {
	
	public string text;
	
	// Use this for initialization
	void Start () {	
		gameObject.GetComponent<TMP_Text>().text = "This sentence (wrong display):\n" + text +
			"\n\nWill appear correctly as:\n" + ArabicFixer.Fix(text, false, false);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
