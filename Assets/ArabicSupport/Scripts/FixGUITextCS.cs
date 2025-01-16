using UnityEngine;
using System.Collections;
using ArabicSupport;
using TMPro;

public class FixGUITextCS : MonoBehaviour {
	
	public string text;
	public bool tashkeel = true;
	public bool hinduNumbers = true;
	
	// Use this for initialization
	void Start () {
		gameObject.GetComponent<TMP_Text>().text = ArabicFixer.Fix(text, tashkeel, hinduNumbers);
	}
	
	// Update is called once per frame
	void Update () {
		gameObject.GetComponent<TMP_Text>().text = ArabicFixer.Fix(text, tashkeel, hinduNumbers);
	}
}
