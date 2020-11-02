using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SyntaxCanvas : MonoBehaviour {
	public Text message;
	public InputField input;
	public Text cardText;
	public Camera cardCam;
	public GameObject cardCanvas;
	List<GameObject> selectedCards = new List<GameObject>();

	void Start() {

	}

	void OnEnable() {
		cardCam.gameObject.SetActive(false);
	}

	void Update() {
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		Ray ray2 = cardCam.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		// 카드 선택
		if (Physics.Raycast(ray, out hit, 30, 1 << LayerMask.NameToLayer("Card")) && !cardCam.gameObject.activeSelf && Input.GetMouseButtonDown(0)) {
			cardCam.gameObject.SetActive(true);
			// 카드 이름 표시
			string name = hit.transform.gameObject.name;
			int n = System.Convert.ToInt32(name.Split('_')[1]), m = System.Convert.ToInt32(name.Split('_')[2]);
			if (GameManager.instance.cards[n - 1][m - 1].name == "")
				cardText.text = "이름이 없는 카드입니다!";
			else
				cardText.text = GameManager.instance.cards[n - 1][m - 1].name;
			StartCoroutine(ShowName());
			// 처음 선택된 카드인 경우
			if (selectedCards.Count == 0) {
				selectedCards.Add(hit.transform.gameObject);
				hit.transform.GetComponent<Card>().Select(new Vector3(0f, -1f, -294f));
				message.text = "이 카드들을 합쳐 뭐라 부르나요?";
				message.GetComponent<TextBlinking>().enable = false;
				input.gameObject.SetActive(true);
				if (cardCanvas.GetComponent<CardCanvas>().IsActive())
					cardCanvas.GetComponent<CardCanvas>().Hide();
			}
			else {
				selectedCards.Add(hit.transform.gameObject);
				hit.transform.GetComponent<Card>().Select(new Vector3(0f, -1f, -294f));
				for (int i = 0; i < selectedCards.Count; i++)
					selectedCards[i].transform.position =
						new Vector3(selectedCards.Count - 1 - (2 * i),
						-1 - (selectedCards.Count > 4 ? 0.5f * (selectedCards.Count - 4) : 0),
						-294 - (selectedCards.Count > 4 ? (selectedCards.Count - 4) : 0));
			}
		}
		// 카드 선택 해제
		if (Physics.Raycast(ray2, out hit, 100, 1 << LayerMask.NameToLayer("SelectedCard")) && cardCam.gameObject.activeSelf && Input.GetMouseButtonDown(1)) {
			if (selectedCards.Count == 1) {
				selectedCards.Remove(hit.transform.gameObject);
				hit.transform.GetComponent<Card>().Unselect();
				message.text = "낱말카드를 선택하세요";
				message.GetComponent<TextBlinking>().enable = true;
				input.gameObject.SetActive(false);
				cardCam.gameObject.SetActive(false);
			}
			else {
				selectedCards.Remove(hit.transform.gameObject);
				hit.transform.GetComponent<Card>().Unselect();
				for (int i = 0; i < selectedCards.Count; i++)
					selectedCards[i].transform.position =
						new Vector3(selectedCards.Count - 1 - (2 * i),
						-1 - (selectedCards.Count > 4 ? 0.5f * (selectedCards.Count - 4) : 0),
						-294 - (selectedCards.Count > 4 ? (selectedCards.Count - 4) : 0));
			}
		}
		// 카드 숨기기
		if (Input.GetKeyDown(KeyCode.Tab) && selectedCards.Count != 0)
			cardCam.GetComponent<Toggle>().toggle();

		// 카드 증발 시 핸들링	
		foreach (var c in selectedCards)
			if (c == null) {
				foreach (var cc in selectedCards)
					if (cc != null)
						cc.GetComponent<Card>().Unselect();
				OnDisable();
				break;
			}

		// 뒤로가기
		if (Input.GetKeyDown(KeyCode.Escape))
			if (input.IsActive()) {
				foreach (var c in selectedCards)
					c.GetComponent<Card>().Unselect();
				OnDisable();
			}
			else
				transform.Find("Exit").GetComponent<Button>().onClick.Invoke();
	}

	// 카드 이름 입력 완료
	public void Typed() {
		// 카드 이름 저장하는 함수
		if (input.text == "" || input.text.Trim() == "")
			Debug.Log("none of text");
		else if (selectedCards.Count == 1) {
			// 선택한 카드가 하나일 경우 -> 카드의 조합형만 저장
			int i = System.Convert.ToInt32(selectedCards[0].name.Split('_')[1]), j = System.Convert.ToInt32(selectedCards[0].name.Split('_')[2]);
			if (GameManager.instance.cards[i - 1][j - 1].name == "")
				GameManager.instance.cards[i - 1][j - 1].name = input.text;
			CardCombine cc = new CardCombine(input.text, false, -1, false, input.text.Trim());
			GameManager.instance.cards[i - 1][j - 1].CombineAdd(cc, 3);
		}
		else
			// 선택한 카드가 여러 개일 경우
			GameManager.instance.MultiCardLearning(selectedCards, input.text, false);
		// 후처리
		foreach (var c in selectedCards)
			c.GetComponent<Card>().Unselect();
		selectedCards.Clear();
		input.gameObject.SetActive(false);
		message.text = "낱말카드를 선택하세요";
		message.GetComponent<TextBlinking>().enable = true;
		cardCam.gameObject.SetActive(false);
		input.text = "";
		GameManager.instance.SaveData();
	}

	// 초기화
	void OnDisable() {
		selectedCards.Clear();
		input.gameObject.SetActive(false);
		message.text = "낱말카드를 선택하세요";
		message.GetComponent<TextBlinking>().enable = true;
		cardCam.gameObject.SetActive(false);
	}

	// 카드 이름 띄우는 애니메이션
	IEnumerator ShowName() {
		cardText.color = new Color(0, 0, 0, 1);
		yield return new WaitForSeconds(1);
		while (cardText.color.a > 0) {
			cardText.color -= new Color(0, 0, 0, Time.deltaTime);
			yield return new WaitForEndOfFrame();
		}
	}
}
