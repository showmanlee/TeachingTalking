﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class TestCombine {
	public CardCombine combine { get; }
	public int index { get; }

	public TestCombine(CardCombine c, int idx) {
		combine = c; index = idx;
	}
}

public class TestCanvas : MonoBehaviour {
	public Text message;
	public Camera cardCam;
	public Text cardText;
	public GameObject resultWindow, done, decisions, reorders;
	public GameObject cardCanvas;
	List<GameObject> selectedCards = new List<GameObject>();
	List<List<TestCombine>> orderedCombines = new List<List<TestCombine>>();            // 테스트 우선순위로 정렬된 리스트
	List<int> selectedCombines = new List<int>();                                       // 문장 조합에 실제로 사용된 orderedCombines 조합식 인덱스 리스트
	List<List<int>> reservedCombines = new List<List<int>>();                           // 평가 후 우선순위 감소를 위한 저장 리스트(앞전, 앞후, 감소 횟수)
	string result;

	void Start() {

	}

	void OnEnable() {
		cardCam.gameObject.SetActive(false);
	}


	void Update() {
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		Ray ray2 = cardCam.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (!resultWindow.activeSelf) {
			// 카드 선택
			if (Physics.Raycast(ray, out hit, 100, 1 << LayerMask.NameToLayer("Card")) && !cardCam.gameObject.activeSelf && Input.GetMouseButtonDown(0)) {
				cardCam.gameObject.SetActive(true);
				// 카드 이름 표시
				string name = hit.transform.gameObject.name;
				int n = System.Convert.ToInt32(name.Split('_')[1]), m = System.Convert.ToInt32(name.Split('_')[2]);
				if (GameManager.instance.cards[n - 1][m - 1].name == "")
					cardText.text = "이름이 없는 카드입니다!";
				else
					cardText.text = GameManager.instance.cards[n - 1][m - 1].name;
				StartCoroutine(ShowName());
				if (selectedCards.Count == 0) {
					selectedCards.Add(hit.transform.gameObject);
					hit.transform.GetComponent<Card>().Select(new Vector3(0f, -1f, -294f));
					message.text = "카드 선택이 끝났다면 버튼을 누르세요!";
					message.GetComponent<TextBlinking>().enable = false;
					done.gameObject.SetActive(true);
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
			if (Physics.Raycast(ray2, out hit, 30, 1 << LayerMask.NameToLayer("SelectedCard")) && cardCam.gameObject.activeSelf && Input.GetMouseButtonDown(1)) {
				if (selectedCards.Count == 1) {
					selectedCards.Remove(hit.transform.gameObject);
					hit.transform.GetComponent<Card>().Unselect();
					message.text = "낱말카드를 선택하세요";
					message.GetComponent<TextBlinking>().enable = true;
					done.gameObject.SetActive(false);
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
		}
		else {
			// 우선순위 변경을 위한 카드 선택
			if (Physics.Raycast(ray2, out hit, 30, 1 << LayerMask.NameToLayer("SelectedCard")) && cardCam.gameObject.activeSelf && !decisions.activeSelf && Input.GetMouseButtonDown(0)) {
				int i = selectedCards.FindIndex(r => r == hit.transform.gameObject);
				// 우선순위 변경 - selectedCombines
				if (i == 0) {
					for (int j = selectedCombines[i]; j < orderedCombines[i].Count; j++)
						if (orderedCombines[i][selectedCombines[i]].combine.style != orderedCombines[i][j].combine.style || j == orderedCombines[i].Count - 1) {
							selectedCombines[i] = j;
							break;
						}
					print(selectedCombines[i]);
				}
				else if (i == selectedCards.Count - 1) {
					for (int j = selectedCombines[i * 2 - 1]; j < orderedCombines[i * 2 - 1].Count; j++)
						if (orderedCombines[i * 2 - 1][selectedCombines[i * 2 - 1]].combine.style != orderedCombines[i * 2 - 1][j].combine.style || j == orderedCombines[i * 2 - 1].Count - 1) {
							selectedCombines[i * 2 - 1] = j;
							break;
						}
					print(selectedCombines[i * 2 - 1]);
				}
				else {
					// 순차적으로 우선순위 감소
					switch (reservedCombines[i][2] % 3) {
						case 0:
							for (int j = selectedCombines[i * 2 - 1]; j < orderedCombines[i * 2 - 1].Count; j++)
								if (orderedCombines[i * 2 - 1][selectedCombines[i * 2 - 1]].combine.style != orderedCombines[i * 2 - 1][j].combine.style || j == orderedCombines[i * 2 - 1].Count - 1) {
									selectedCombines[i * 2 - 1] = j;
									break;
								}
							reservedCombines[i][1] = selectedCombines[i * 2 - 1];
							print(selectedCombines[i * 2 - 1]);
							break;
						case 1:
							selectedCombines[i * 2 - 1] = reservedCombines[i][0];
							for (int j = selectedCombines[i * 2]; j < orderedCombines[i * 2].Count; j++)
								if (orderedCombines[i * 2][selectedCombines[i * 2]].combine.style != orderedCombines[i * 2][j].combine.style || j == orderedCombines[i * 2].Count - 1) {
									selectedCombines[i * 2] = j;
									break;
								}
							print(selectedCombines[i * 2]);
							break;
						case 2:
							selectedCombines[i * 2 - 1] = reservedCombines[i][1];
							reservedCombines[i][0] = selectedCombines[i * 2 - 1];
							break;
					}
					reservedCombines[i][2]++;
				}
				MakeSentence();
				decisions.SetActive(true);
				reorders.SetActive(false);
			}
		}

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
			if (done.activeSelf) {
				foreach (var c in selectedCards)
					c.GetComponent<Card>().Unselect();
				OnDisable();
			}
			else
				transform.Find("Exit").GetComponent<Button>().onClick.Invoke();
	}

	// 카드 선택 완료
	public void Done() {
		// 미학습 카드 에러
		foreach (var c in selectedCards) {
			int m = System.Convert.ToInt32(c.name.Split('_')[1]), n = System.Convert.ToInt32(c.name.Split('_')[2]);
			if (GameManager.instance.cards[m - 1][n - 1].name == "") {
				cardCanvas.GetComponent<CardCanvas>().TestError();
				foreach (var d in selectedCards)
					d.GetComponent<Card>().Unselect();
				OnDisable();
				return;
			}
		}
		done.SetActive(false);
		resultWindow.SetActive(true);
		// 우선순위 리스트 제작
		for (int i = 0; i < selectedCards.Count; i++) {
			// 리스트 생성 준비
			orderedCombines.Add(new List<TestCombine>());
			if (i > 0 && i < selectedCards.Count - 1)
				orderedCombines.Add(new List<TestCombine>());
			int m = System.Convert.ToInt32(selectedCards[i].name.Split('_')[1]), n = System.Convert.ToInt32(selectedCards[i].name.Split('_')[2]);
			string originalName = GameManager.instance.cards[m - 1][n - 1].name;
			// 리스트에 넣고 정렬		
			if (i * 2 - 1 != -1) {
				// 홀수 - 앞
				int mf = System.Convert.ToInt32(selectedCards[i - 1].name.Split('_')[1]);
				// 조합식들 투입
				for (int j = 0; j < GameManager.instance.cards[m - 1][n - 1].combines.Count; j++) {
					if (GameManager.instance.cards[m - 1][n - 1].combines[j].position) {
						orderedCombines[i * 2 - 1].Add(new TestCombine(GameManager.instance.cards[m - 1][n - 1].combines[j], j));
						// 만약 띄어쓰기가 있는 언어인 상황에서 현재 조합식에 띄어쓰기가 없는 경우, 띄어쓰기가 있는 버전도 투입
						if (GameManager.instance.spacing && GameManager.instance.cards[m - 1][n - 1].combines[j].style[0] != ' ') {
							CardCombine c = GameManager.instance.cards[m - 1][n - 1].combines[j];
							CardCombine cc = new CardCombine(" " + c.style, c.position, c.combinedWith, c.isEnd, c.root);
							orderedCombines[i * 2 - 1].Add(new TestCombine(cc, -1));
						}
					}
				}
				// 원래 이름(-2) 투입
				orderedCombines[i * 2 - 1].Add(new TestCombine(new CardCombine(
					GameManager.instance.GetSpacing() ? " " + originalName : originalName, true, -2, i == selectedCards.Count - 1 ? true : false, originalName), -1));
				orderedCombines[i * 2 - 1].Sort(delegate (TestCombine x, TestCombine y) {
					// 맞는 -> 다른 -> -2
					// end는 상황에 따라 최상위 or 최하위
					if (x.combine.isEnd && !y.combine.isEnd)
						return i == selectedCards.Count - 1 ? -1 : 1;
					else if (!x.combine.isEnd && y.combine.isEnd)
						return i == selectedCards.Count - 1 ? 1 : -1;
					if (x.combine.combinedWith == -2 && x.combine.combinedWith != -2)
						return 1;
					else if (x.combine.combinedWith != -2 && x.combine.combinedWith == -2)
						return -1;
					if (x.combine.combinedWith == mf && y.combine.combinedWith != mf)
						return -1;
					else if (x.combine.combinedWith != mf && y.combine.combinedWith == mf)
						return 1;
					else
						return x.combine.bias.CompareTo(y.combine.bias) * -1;
				});
				// 현재 조합 관계가 아닌 것들 중에서 중복된 게 있을 경우 제거
				List<string> exist = new List<string>();
				List<TestCombine> remove = new List<TestCombine>();
				foreach (var tc in orderedCombines[i * 2 - 1]) {
					if (!exist.Contains(tc.combine.style))
						exist.Add(tc.combine.style);
					else {
						if (tc.combine.combinedWith != mf)
							remove.Add(tc);
					}
				}
				foreach (var r in remove)
					orderedCombines[i * 2 - 1].Remove(r);
				foreach (var tc in orderedCombines[i * 2 - 1])
					print(tc.combine.ToString());
			}
			if (i * 2 < (selectedCards.Count - 1) * 2) {
				// 짝수 - 뒤
				int mr = System.Convert.ToInt32(selectedCards[i + 1].name.Split('_')[1]);
				// 조합식들 투입
				for (int j = 0; j < GameManager.instance.cards[m - 1][n - 1].combines.Count; j++)
					if (!GameManager.instance.cards[m - 1][n - 1].combines[j].position)
						orderedCombines[i * 2].Add(new TestCombine(GameManager.instance.cards[m - 1][n - 1].combines[j], j));
				// 원래 이름(-2) 투입
				orderedCombines[i * 2].Add(new TestCombine(new CardCombine(
					i != 0 && GameManager.instance.GetSpacing() ? " " + originalName : originalName, false, -2, false, originalName), -1));
				orderedCombines[i * 2].Sort(delegate (TestCombine x, TestCombine y) {
					// 맞는 -> 다른 -> -2
					if (x.combine.combinedWith == -2 && x.combine.combinedWith != -2)
						return 1;
					else if (x.combine.combinedWith != -2 && x.combine.combinedWith == -2)
						return -1;
					if (x.combine.combinedWith == mr && y.combine.combinedWith != mr)
						return -1;
					else if (x.combine.combinedWith != mr && y.combine.combinedWith == mr)
						return 1;
					else
						return x.combine.bias.CompareTo(y.combine.bias) * -1;
				});
				// 현재 조합 관계가 아닌 것들 중에서 중복된 게 있을 경우 제거
				List<string> exist = new List<string>();
				List<TestCombine> remove = new List<TestCombine>();
				foreach (var tc in orderedCombines[i * 2]) {
					if (!exist.Contains(tc.combine.style))
						exist.Add(tc.combine.style);
					else {
						if (tc.combine.combinedWith != mr)
							remove.Add(tc);
					}
				}
				foreach(var r in remove) 
					orderedCombines[i * 2].Remove(r);
				foreach (var tc in orderedCombines[i * 2])
					print(tc.combine.ToString());
			}
			// 지난 우선순위 표시기 초기화
			reservedCombines.Add(new List<int>());
			for (int j = 0; j < 3; j++)
				reservedCombines[i].Add(0);
		}
		// 선택된 우선순위 표시기 초기화
		foreach (var p in orderedCombines)
			selectedCombines.Add(0);

		// 문장 만드는 함수
		MakeSentence();
	}

	void MakeSentence() {
		result = "";

		// 선택된 카드가 하나라면
		if (selectedCards.Count == 1) {
			int m = System.Convert.ToInt32(selectedCards[0].name.Split('_')[1]), n = System.Convert.ToInt32(selectedCards[0].name.Split('_')[2]);
			resultWindow.transform.GetChild(1).GetComponent<Text>().text = GameManager.instance.cards[m - 1][n - 1].name;
			return;
		}

		for (int i = 0; i < selectedCards.Count; i++) {
			// 맨 끝인 경우
			if (i == 0)
				result += orderedCombines[i][selectedCombines[i]].combine.style;
			else if (i == selectedCards.Count - 1)
				result += orderedCombines[i * 2 - 1][selectedCombines[i * 2 - 1]].combine.style;
			else {
				string f = orderedCombines[i * 2 - 1][selectedCombines[i * 2 - 1]].combine.style;   // 앞
				string b = orderedCombines[i * 2][selectedCombines[i * 2]].combine.style;           // 뒤
																									// 모두 원형 = 둘이 같음
				if (orderedCombines[i * 2 - 1][selectedCombines[i * 2 - 1]].combine.combinedWith == -2 && orderedCombines[i * 2][selectedCombines[i * 2]].combine.combinedWith == -2)
					result += f;
				// 둘 중 하나가 원형 = 원형 아닌 쪽
				else if (orderedCombines[i * 2 - 1][selectedCombines[i * 2 - 1]].combine.combinedWith == -2 || orderedCombines[i * 2][selectedCombines[i * 2]].combine.combinedWith == -2)
					result += orderedCombines[i * 2 - 1][selectedCombines[i * 2 - 1]].combine.combinedWith == -2 ? b : f;
				else {
					// 앞과 뒤가 같음
					if (f == b)
						result += f;
					// 앞과 뒤가 다름 = 가중치가 큰 쪽
					else
						result += orderedCombines[i * 2 - 1][selectedCombines[i * 2 - 1]].combine.bias > orderedCombines[i * 2][selectedCombines[i * 2]].combine.bias ? f : b;
				}
			}
		}
		resultWindow.transform.GetChild(1).GetComponent<Text>().text = result;
	}

	// 문장 학습 결과
	public void Result(int a) {
		switch (a) {
			case 0:             // OK
				resultWindow.SetActive(false);
				for (int i = 0; i < selectedCards.Count; i++) {
					int m = System.Convert.ToInt32(selectedCards[i].name.Split('_')[1]), n = System.Convert.ToInt32(selectedCards[i].name.Split('_')[2]);
					int mf = -1, mr = -1;
					if (i != 0)
						mf = System.Convert.ToInt32(selectedCards[i - 1].name.Split('_')[1]);
					else if (i != selectedCards.Count - 1)
						mr = System.Convert.ToInt32(selectedCards[i + 1].name.Split('_')[1]);

					if (i * 2 - 1 != -1) {                                          // 앞쪽
						if (orderedCombines[i * 2 - 1][selectedCombines[i * 2 - 1]].index != -1) {  // 기존에 있던 조합식
							CardCombine cc;
							if (orderedCombines[i * 2 - 1][selectedCombines[i * 2 - 1]].combine.combinedWith != mf) {       // 결합 카드군이 다른 조합식이 선택됐다면?
								cc = orderedCombines[i * 2 - 1][selectedCombines[i * 2 - 1]].combine;
								cc.combinedWith = mf;
								GameManager.instance.cards[m - 1][n - 1].CombineAdd(cc, 5);
							}
							else {
								cc = GameManager.instance.cards[m - 1][n - 1].combines[orderedCombines[i * 2 - 1][selectedCombines[i * 2 - 1]].index];
								cc.bias += 5;
							}
							string front = cc.style.Split(new[] { cc.root }, StringSplitOptions.None)[0].TrimStart();
							// 형태소 추가
							bool morpfound = false;
							foreach (Morpheme morp in GameManager.instance.morphemes)
								if (morp.name == cc.root) {
									morpfound = true;
									if (front != "")
										morp.Connects(front, mf, true);
									break;
								}
							if (!morpfound) {
								GameManager.instance.morphemes.Add(new Morpheme(cc.root, m, n));
								if (front != "")
									GameManager.instance.morphemes[GameManager.instance.morphemes.Count - 1].Connects(front, mf, true);
							}
						}
						else {                                                                      // 임시용 원래 이름 조합식
							CardCombine cc = orderedCombines[i * 2 - 1][selectedCombines[i * 2 - 1]].combine;
							cc.combinedWith = mf;
							GameManager.instance.cards[m - 1][n - 1].CombineAdd(cc, 5);
							// 원래 이름밖에 없으니 어근만 저장
							bool morpfound = false;
							foreach (Morpheme morp in GameManager.instance.morphemes) {
								if (morp.name == cc.root) {
									morpfound = true;
									break;
								}
							}
							if (!morpfound)
								GameManager.instance.morphemes.Add(new Morpheme(cc.root, m, n));
						}
					}
					if (i * 2 < (selectedCards.Count - 1) * 2) {                // 뒷쪽
						if (orderedCombines[i * 2][selectedCombines[i * 2]].index != -1) {          // 기존에 있던 조합식
							CardCombine cc;
							if (orderedCombines[i * 2][selectedCombines[i * 2]].combine.combinedWith != mr) {       // 결합 카드군이 다른 조합식이 선택됐다면?
								cc = orderedCombines[i * 2][selectedCombines[i * 2]].combine;
								cc.combinedWith = mf;
								GameManager.instance.cards[m - 1][n - 1].CombineAdd(cc, 5);
							}
							else {
								cc = GameManager.instance.cards[m - 1][n - 1].combines[orderedCombines[i * 2][selectedCombines[i * 2]].index];
								cc.bias += 5;
							}
							string rear = cc.style.Split(new[] { cc.root }, StringSplitOptions.None)[1].TrimEnd();
							// 형태소 추가
							bool morpfound = false;
							foreach (Morpheme morp in GameManager.instance.morphemes)
								if (morp.name == cc.root) {
									morpfound = true;
									if (rear != "")
										morp.Connects(rear, mr, false);
									break;
								}
							if (!morpfound) {
								GameManager.instance.morphemes.Add(new Morpheme(cc.root, m, n));
								if (rear != "")
									GameManager.instance.morphemes[GameManager.instance.morphemes.Count - 1].Connects(rear, mr, false);
							}
						}
						else {                                                                      // 임시용 원래 이름 조합식
							CardCombine cc = orderedCombines[i * 2][selectedCombines[i * 2]].combine;
							cc.combinedWith = mr;
							GameManager.instance.cards[m - 1][n - 1].CombineAdd(cc, 5);
							// 원래 이름밖에 없으니 어근만 저장
							bool morpfound = false;
							foreach (Morpheme morp in GameManager.instance.morphemes) {
								if (morp.name == cc.root) {
									morpfound = true;
									break;
								}
							}
							if (!morpfound)
								GameManager.instance.morphemes.Add(new Morpheme(cc.root, m, n));
						}
					}
				}
				EndofTesting();
				break;
			case 1:             // SOSO
				for (int i = 0; i < selectedCards.Count; i++) {
					int m = System.Convert.ToInt32(selectedCards[i].name.Split('_')[1]), n = System.Convert.ToInt32(selectedCards[i].name.Split('_')[2]);
					int mf = -1, mr = -1;
					if (i != 0)
						mf = System.Convert.ToInt32(selectedCards[i - 1].name.Split('_')[1]);
					else if (i != selectedCards.Count - 1)
						mr = System.Convert.ToInt32(selectedCards[i + 1].name.Split('_')[1]);
					if (i * 2 - 1 != -1) {
						if (orderedCombines[i * 2 - 1][selectedCombines[i * 2 - 1]].index != -1) {  // 기존에 있던 조합식
							if (orderedCombines[i * 2 - 1][selectedCombines[i * 2 - 1]].combine.combinedWith != mf) {       // 결합 카드군이 다른 조합식이 선택됐다면?
								CardCombine cc = orderedCombines[i * 2 - 1][selectedCombines[i * 2 - 1]].combine;
								cc.combinedWith = mf;
								GameManager.instance.cards[m - 1][n - 1].CombineAdd(cc, 1);
							}
							else
								GameManager.instance.cards[m - 1][n - 1].combines[orderedCombines[i * 2 - 1][selectedCombines[i * 2 - 1]].index].bias++;
						}
						else {                                                                      // 임시용 원래 이름 조합식
							CardCombine cc = orderedCombines[i * 2 - 1][selectedCombines[i * 2 - 1]].combine;
							cc.combinedWith = mf;
							GameManager.instance.cards[m - 1][n - 1].CombineAdd(cc, 1);
						}
					}
					if (i * 2 < (selectedCards.Count - 1) * 2) {                // 뒷쪽
						if (orderedCombines[i * 2][selectedCombines[i * 2]].index != -1) {          // 기존에 있던 조합식
							if (orderedCombines[i * 2][selectedCombines[i * 2]].combine.combinedWith != mr) {           // 결합 카드군이 다른 조합식이 선택됐다면?
								CardCombine cc = orderedCombines[i * 2][selectedCombines[i * 2]].combine;
								cc.combinedWith = mf;
								GameManager.instance.cards[m - 1][n - 1].CombineAdd(cc, 1);
							}
							else
								GameManager.instance.cards[m - 1][n - 1].combines[orderedCombines[i * 2][selectedCombines[i * 2]].index].bias++;
						}
						else {                                                                      // 임시용 원래 이름 조합식
							CardCombine cc = orderedCombines[i * 2][selectedCombines[i * 2]].combine;
							cc.combinedWith = mr;
							GameManager.instance.cards[m - 1][n - 1].CombineAdd(cc, 1);
						}
					}
				}
				decisions.SetActive(false);
				reorders.SetActive(true);
				break;
			case 2:             // NO
				decisions.SetActive(false);
				reorders.SetActive(true);
				break;
			case 3:             // Force Exit
				resultWindow.SetActive(false);
				EndofTesting();
				break;
		}
	}

	void EndofTesting() {
		foreach (var c in selectedCards)
			c.GetComponent<Card>().Unselect();
		OnDisable();
		orderedCombines.Clear(); selectedCombines.Clear(); reservedCombines.Clear();
		GameManager.instance.SaveData();
	}

	// 초기화
	void OnDisable() {
		selectedCards.Clear();
		done.gameObject.SetActive(false);
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