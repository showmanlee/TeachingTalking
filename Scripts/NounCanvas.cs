using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class NounCanvas : MonoBehaviour {
	bool selected = false;
	public Text message;
	public InputField input;
	public Camera cardCam;
	public GameObject cardCanvas;
	GameObject selectedCard;

	void Start() {
	}

	void OnEnable() {
		cardCam.gameObject.SetActive(true);
	}

	void Update() {
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		Ray ray2 = cardCam.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		// 카드 선택
		if (Physics.Raycast(ray, out hit, 30, 1 << LayerMask.NameToLayer("Card")) && Input.GetMouseButtonDown(0)) {
			if (!selected) {
				cardCam.gameObject.SetActive(true);
				hit.transform.GetComponent<Card>().Select(new Vector3(1, 0, -292.5f));
				selectedCard = hit.transform.gameObject;
				selected = true;
				message.text = "카드 이름을 입력하세요!";
				message.GetComponent<TextBlinking>().enable = false;
				if (cardCanvas.GetComponent<CardCanvas>().IsActive())
					cardCanvas.GetComponent<CardCanvas>().Hide();

				// input 텍스트필드에 해당 카드 이름 넣기.
				int i = System.Convert.ToInt32(selectedCard.name.Split('_')[1]), j = System.Convert.ToInt32(selectedCard.name.Split('_')[2]);
				Debug.Log(GameManager.instance.cards[i - 1][j - 1].ToString());
				input.text = GameManager.instance.cards[i - 1][j - 1].name;
				input.gameObject.SetActive(true);
			}
		}
		// 카드 선택 해제
		if (Physics.Raycast(ray2, out hit, 30, 1 << LayerMask.NameToLayer("SelectedCard")) && Input.GetMouseButtonDown(1)) {
			if (selected) {
				hit.transform.GetComponent<Card>().Unselect();
				selected = false;
				input.gameObject.SetActive(false);
				message.text = "낱말카드를 선택하세요";
				message.GetComponent<TextBlinking>().enable = true;
			}
		}

		if (selectedCard == null)
			OnDisable();

		// 뒤로가기
		if (Input.GetKeyDown(KeyCode.Escape))
			if (input.IsActive()) {
				selectedCard.GetComponent<Card>().Unselect();
				OnDisable();
			}
			else
				transform.Find("Exit").GetComponent<Button>().onClick.Invoke();
	}

	// 카드 이름 입력 완료
	public void Typed() {
		input.gameObject.SetActive(false);
		message.text = "낱말카드를 선택하세요";
		message.GetComponent<TextBlinking>().enable = true;
		// 카드 이름 저장하는 함수
		int i = System.Convert.ToInt32(selectedCard.name.Split('_')[1]), j = System.Convert.ToInt32(selectedCard.name.Split('_')[2]);
		GameManager.instance.cards[i - 1][j - 1].name = input.text;
		input.text = "";
		GameManager.instance.SaveData();
		selectedCard.GetComponent<Card>().Unselect();
		selected = false;
	}

	// 초기화
	void OnDisable() {
		input.gameObject.SetActive(false);
		selected = false;
		message.text = "낱말카드를 선택하세요";
		message.GetComponent<TextBlinking>().enable = true;
	}
}
