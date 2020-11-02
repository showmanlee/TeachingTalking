using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardGroup {
	string name;
	public List<Texture2D> images { get; }
	public CardGroup(string name) {
		this.name = name;
		images = new List<Texture2D>();
	}

	public string GetName() => name;
}

