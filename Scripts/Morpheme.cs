using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

public class Morpheme {
	public string name { get; set; }
	public Dictionary<string, MorphemeConnect> fronts { get; set; }
	public Dictionary<string, MorphemeConnect> backs { get; set; }
	public int originGroup { get; set; }
	public int originCardNum { get; set; }

	public Morpheme(string name, int group, int number) {
		this.name = name;
		fronts = new Dictionary<string, MorphemeConnect>();
		backs = new Dictionary<string, MorphemeConnect>();
		originGroup = group;
		originCardNum = number;
	}

	public void Connects(string morp, int cardG, bool side) {
		if (morp == "")			// 형태소가 비었다면 저장 안함
			return;
		if (side) {
			if (!fronts.ContainsKey(morp))
				fronts.Add(morp, new MorphemeConnect());
			fronts[morp].ConnectAdd(cardG);
		}
		else {
			if (!backs.ContainsKey(morp))
				backs.Add(morp, new MorphemeConnect());
			backs[morp].ConnectAdd(cardG);
		} 
	}
}

public class MorphemeConnect {
	public int bias { get; set; }
	public List<int> connectWith { get; set; }

	public MorphemeConnect() {
		bias = 1;
		connectWith = new List<int>();
	}

	public void ConnectAdd(int n) {
		if (!connectWith.Contains(n))
			connectWith.Add(n);
		bias++;
	}
}