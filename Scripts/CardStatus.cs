using System;
using System.Collections.Generic;
using UnityEngine;

public class CardStatus {
	public string name { get; set; }
	public int group { get; set; }
	public int number { get; set; }
	public List<CardCombine> combines { get; set; }
	public CardStatus() {
		name = "";
		group = number = 0;
		combines = new List<CardCombine>();
	}
	public void CombineAdd(CardCombine c, int point) {
		for(int i = 0; i < combines.Count; i++)
			if (combines[i] == c) {
				Debug.Log("same");
				combines[i].bias += point;
				return;
			}
		c.bias += point;
		combines.Add(c);
	}
	public override string ToString() {
		return group + "-" + number + ": " + name;
	}
}

public class CardCombine {
	public string style { get; set; }
	public bool position { get; set; }
	public int combinedWith { get; set; }
	public int bias { get; set; }
	public bool isEnd { get; set; }
	public string root { get; set; }

	public CardCombine() => style = "";
	public CardCombine(string style, bool position, int combinedWith, bool isEnd, string root) {
		this.style = style;
		this.position = position;
		this.combinedWith = combinedWith;
		this.isEnd = isEnd;
		bias = 1;
		this.root = root;
	}
	public override string ToString() {
		return style + ": " + (position ? "front" : "rear") + " " + combinedWith + " " + bias + " " + (isEnd ? "end" : "");
	}

	public static bool operator ==(CardCombine a, CardCombine b) =>
		a.style == b.style && a.position == b.position && a.combinedWith == b.combinedWith && a.isEnd == b.isEnd && a.root == b.root;
	public static bool operator !=(CardCombine a, CardCombine b) =>
		!(a == b);
	public override bool Equals(object obj) {
		return base.Equals(obj);
	}
	public override int GetHashCode() {
		return base.GetHashCode();
	}
}