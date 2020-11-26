using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour {
	public Texture ilust;
	bool selected = false;
	public int m = 0, n = 0;

	void Start() {
		// 카드 생성 시 초기 위치 잡기
		transform.position = new Vector3(Random.Range(-7, 7), Random.Range(1, 5), Random.Range(1, 10));
		GetComponent<Rigidbody>().AddForce(new Vector3(Random.Range(-100, 100), 200, Random.Range(-100, 100)));
		GetComponent<Rigidbody>().AddTorque(new Vector3(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180)));
		m = System.Convert.ToInt32(gameObject.name.Split('_')[1]);
		n = System.Convert.ToInt32(gameObject.name.Split('_')[2]);
		if (GameManager.instance.cards[m - 1][n - 1].name == "")
			GetComponent<Renderer>().material.color = new Color(1f, 1f, 0f);
	}

	void Update() {
		// 카드 날리기
		if (!selected) {
			if (Input.GetKeyDown(KeyCode.F1)) {
				GetComponent<Rigidbody>().AddForce(new Vector3(Random.Range(-400, 400), 400, Random.Range(-300, 400)));
				GetComponent<Rigidbody>().AddTorque(new Vector3(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180)));
			}
			if (transform.position.y > 7.5f || transform.position.y < 0f)
				transform.position = new Vector3(Random.Range(-7, 7), Random.Range(1, 5), Random.Range(1, 10));
		}
		if (GameManager.instance.GetModes() == Modes.Main && selected)
			Unselect();
	}

	// 일러스트 할당
	public void SetIlust(Texture t) {
		ilust = t;
		for (int i = 0; i < 2; i++)
			transform.GetChild(i).GetComponent<MeshRenderer>().material.SetTexture("_MainTex", t);
	}

	// 카드 선택/해제
	public void Select(Vector3 pos) {
		selected = true;
		GetComponent<Rigidbody>().useGravity = false;
		GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
		if (GameManager.instance.cards[m - 1][n - 1].name == "")
			GetComponent<Renderer>().material.color = new Color(1f, 1f, 0f);
		else
			GetComponent<Renderer>().material.color = new Color(0.5f, 0.5f, 1f);
		gameObject.layer = LayerMask.NameToLayer("SelectedCard");
		transform.position = pos;
		transform.eulerAngles = new Vector3(90, 0, 0);
	}
	public void Unselect() {
		selected = false;
		GetComponent<Rigidbody>().useGravity = true;
		GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
		if (GameManager.instance.cards[m - 1][n - 1].name == "")
			GetComponent<Renderer>().material.color = new Color(1f, 1f, 0f);
		else
			GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0.5f);
		gameObject.layer = LayerMask.NameToLayer("Card");
		transform.position = new Vector3(Random.Range(-7, 7), Random.Range(1, 5), Random.Range(1, 10));
		transform.eulerAngles = new Vector3(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180));
	}
}
