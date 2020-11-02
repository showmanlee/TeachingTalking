using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextBlinking : MonoBehaviour {
	float t = 0;
	public bool enable = true;
	
	void Start() {

	}

	
	void Update() {
		if (enable) {
			t += Time.deltaTime;
			float d = Mathf.Cos(t) * 0.5f + 0.5f;
			GetComponent<Text>().color = new Color(0, 0, 0, d);
		}
		else
			GetComponent<Text>().color = new Color(0, 0, 0, 1);
	}
}
