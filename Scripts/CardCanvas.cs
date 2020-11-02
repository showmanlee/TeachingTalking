using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardCanvas : MonoBehaviour {
	public GameObject cardDummy;
	public GameObject selectButton;
	List<int> cardCount = new List<int>();
	List<bool> cardActive = new List<bool>();
	List<GameObject> cardButtons = new List<GameObject>();

	// Start is called before the first frame update
	void Start() {

	}

	void Update() {
		// 버튼 만들기
		if (cardCount.Count == 0) {
			cardCount = GameManager.instance.GetCardCount();
			for (int i = 0; i < cardCount.Count; i++) {
				cardActive.Add(false);
				GameObject b = Instantiate(selectButton, transform.Find("Select"));
				b.name = i.ToString();
				b.transform.localPosition = new Vector3(-530 + (i / 10 * 130), 240 - (i % 10 * 30), 0);
				b.GetComponent<Button>().onClick.AddListener(OnCardButtonClicked);
				cardButtons.Add(b);
				b.transform.Find("Text").GetComponent<Text>().text = GameManager.instance.cardImages[i].GetName();
			}
		}
	}

	// 카드 생성 버튼 리스너
	void OnCardButtonClicked() {
		int i = System.Convert.ToInt32(EventSystem.current.currentSelectedGameObject.name);
		if (!cardActive[i]) {
			ShowCards(i + 1);
			EventSystem.current.currentSelectedGameObject.GetComponent<Image>().color = new Color(1, 0.5f, 0.5f);
		}
		else {
			DeleteCards(i + 1);
			EventSystem.current.currentSelectedGameObject.GetComponent<Image>().color = new Color(1, 1, 1);
		}
		cardActive[i] = !cardActive[i];
	}

	// 카드 생성
	public void ShowCards(int n) {
		int max = GameManager.instance.cardImages[n - 1].images.Count;
		for (int i = 0; i < max; i++) {
			string path = "Cards/" + n + "/";
			Texture2D image = GameManager.instance.cardImages[n - 1].images[i];
			GameObject c = Instantiate(cardDummy);
			// Card_카테고리명_i
			c.name = "Card_" + n + "_" + (i + 1);
			c.GetComponent<Card>().SetIlust(image);
		}
	}

	// 카드 삭제
	public void DeleteCards(int n) {
		for (int i = 1; ; i++) {
			GameObject c = GameObject.Find("Card_" + n + "_" + i);
			if (c == null)
				break;
			Destroy(c);
		}
	}

	// 모든 카드 삭제
	public void AllKill() {
		foreach(GameObject b in cardButtons) {
			int i = System.Convert.ToInt32(b.name);
			if (cardActive[i]) {
				cardActive[i] = false;
				DeleteCards(i + 1);
				b.GetComponent<Image>().color = new Color(1, 1, 1);
			}
		}
	}

	public void Hide() {
		if (transform.Find("Select").gameObject.activeSelf)
			transform.Find("Select").GetComponent<Toggle>().toggle();
	}

	public void Saving(bool b) {
		transform.Find("Saving").gameObject.SetActive(b);
	}

	public bool IsActive() => transform.Find("Select").gameObject.activeSelf;

	public void TestError() => StartCoroutine(ErrorAnim());

	IEnumerator ErrorAnim() {
		transform.Find("TestError").GetComponent<Text>().color = new Color(0, 0, 0, 1);
		yield return new WaitForSeconds(5);
		transform.Find("TestError").GetComponent<Text>().color = new Color(0, 0, 0, 0);
	}
}
