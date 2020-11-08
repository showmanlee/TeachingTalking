using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public enum Modes { Main, Noun, Syntax, Sentence, Test }
public class GameManager : MonoBehaviour {
	public static GameManager instance;
	public Text slotText;
	public GameObject robot, cardcam, cardCanvas;
	public GameObject[] Canvases;
	List<int> cardCount = new List<int>();
	public List<CardGroup> cardImages { get; set; }
	public List<List<CardStatus>> cards { get; set; }       // 카드와 조합식
	public List<Morpheme> morphemes { get; set; }           // 어근과 형태소
	public bool spacing { get; set; }
	Modes mode;
	int slot = 0;

	void Awake() {
		instance = this;
	}
	void Start() {
		cards = new List<List<CardStatus>>();
		cardImages = new List<CardGroup>();
		morphemes = new List<Morpheme>();
		// 에셋 번들 불러오기 및 이미지 로드
		AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Application.dataPath + "/AssetBundles", "cardbundle"));
		if (bundle == null) {
			Debug.LogError("AssetBundle을 생성해야 합니다.");
			Application.Quit();
		}
		string path = "assets/resources/cards/";
		for (int i = 1; ; i++) {
			TextAsset name = bundle.LoadAsset<TextAsset>(path + i + "/0.txt");
			if (name == null)
				break;
			cardImages.Add(new CardGroup(name.text));
			for (int j = 1; ; j++) {
				string ipath = path + i + "/" + j;
				if (bundle.LoadAsset<Texture2D>(ipath + ".jpg") != null)
					cardImages[i - 1].images.Add(bundle.LoadAsset<Texture2D>(ipath + ".jpg"));
				else if (bundle.LoadAsset<Texture2D>(ipath + ".png") != null)
					cardImages[i - 1].images.Add(bundle.LoadAsset<Texture2D>(ipath + ".png"));
				else
					break;
			}
		}
		bundle.Unload(false);

		Debug.Log("Hello");
		// 카드 수 확인
		CardLoad();
	}
	void Update() {
		// 모든 카드 삭제
		if (Input.GetKeyDown(KeyCode.F2))
			cardCanvas.GetComponent<CardCanvas>().AllKill();
	}

	// 캔버스 바꾸기
	public void CanvasChange(int n) {
		Canvases[(int)mode].SetActive(false);
		Canvases[n].SetActive(true);
		mode = (Modes)n;
		if (mode != Modes.Main)
			StartCoroutine(WalkAnim());
		else
			StartCoroutine(RollAnim());
		cardCanvas.GetComponent<CardCanvas>().Hide();
	}

	// 카드 저장하기
	public void SaveData() {
		StreamWriter sw = new StreamWriter("cardData" + slot + ".data", false);
		cardCanvas.GetComponent<CardCanvas>().Saving(true);
		// 카드/조합식
		for (int i = 0; i < cards.Count; i++) {
			for (int j = 0; j < cards[i].Count; j++) {
				if (cards[i][j].name.Length == 0)
					continue;
				cards[i][j].combines.Sort(delegate (CardCombine x, CardCombine y) {
					if (x.style.Length == y.style.Length)
						return x.style.CompareTo(y.style);
					else
						return x.style.Length.CompareTo(y.style.Length) * -1;
				});
				sw.WriteLine("{0} {1}", i + 1, j + 1);
				sw.WriteLine(cards[i][j].name);
				foreach (var c in cards[i][j].combines)
					sw.WriteLine("\"{0}\"{1} {2} {3} {4} #{5}", c.style, c.position ? 'f' : 'r', c.combinedWith, c.bias, c.isEnd ? 'e' : 'n', c.root);
				sw.WriteLine("§");
			}
		}
		// 형태소
		sw.WriteLine("--------");
		foreach (var m in morphemes) {
			sw.WriteLine(m.name);
			sw.WriteLine("{0} {1}", m.originGroup, m.originCardNum);
			foreach (string k in m.fronts.Keys) {
				sw.WriteLine("\"{0}\"f {1}", k, m.fronts[k].bias);
				foreach (int n in m.fronts[k].connectWith)
					sw.Write("{0} ", n);
				sw.WriteLine();
			}
			foreach (string k in m.backs.Keys) {
				sw.WriteLine("\"{0}\"b {1}", k, m.backs[k].bias);
				foreach (int n in m.backs[k].connectWith)
					sw.Write("{0} ", n);
				sw.WriteLine();
			}
			sw.WriteLine("§");
		}
		sw.Close();
		cardCanvas.GetComponent<CardCanvas>().Saving(false);
	}

	// 구문/문장 학습
	public void MultiCardLearning(List<GameObject> selectedCards, string input, bool mode) {
		int[,] cardRange = new int[selectedCards.Count, 2];     // 조합식 영역
		int[,] originalRange = new int[selectedCards.Count, 2]; // 원래 단어 영역
		bool[] isOriginal = new bool[selectedCards.Count];      // 조합된 형태가 원본인가?(카드군 자동 학습에 필요)

		// 2-1. 단어 원본 찾기 - V
		for (int i = 0; i < selectedCards.Count; i++) {
			cardRange[i, 0] = -1; cardRange[i, 1] = -1; isOriginal[i] = false;        // 초기 상태
			int m = System.Convert.ToInt32(selectedCards[i].name.Split('_')[1]), n = System.Convert.ToInt32(selectedCards[i].name.Split('_')[2]);       // 카드군-번호 따기
			string currentName = cards[m - 1][n - 1].name;      // 현재 카드 단어 원본
			if (currentName == "")                              // 이름이 없으면 패스
				continue;
			for (int j = cardRange[i == 0 ? i : i - 1, 1] + 1; j <= input.Length - currentName.Length; j++)
				if (input.Substring(j, currentName.Length) == currentName) {    // 원본 발견?
					cardRange[i, 0] = j; cardRange[i, 1] = j + currentName.Length - 1;
					isOriginal[i] = true;
					break;
				}
		}
		// 2-3. 조합식으로 탐색 - V
		for (int i = 0; i < selectedCards.Count; i++)
			if (cardRange[i, 0] == -1) {
				int m = System.Convert.ToInt32(selectedCards[i].name.Split('_')[1]), n = System.Convert.ToInt32(selectedCards[i].name.Split('_')[2]);
				foreach (var c in cards[m - 1][n - 1].combines) {       // 조합식 순회
					string currentName = c.style;                       // 현재 조합식
					if (currentName == "")
						continue;
					for (int j = cardRange[i == 0 ? i : i - 1, 1] + 1; j <= input.Length - currentName.Length; j++) {
						if (input.Substring(j, currentName.Length) == currentName) {    // 조합식 발견?
							cardRange[i, 0] = j; cardRange[i, 1] = j + currentName.Length - 1;
							break;
						}
					}
					if (cardRange[i, 0] != -1)
						break;
				}
			}
		// 2-4. 한글 종성 체크(맨앞글자) - V
		for (int i = 0; i < selectedCards.Count; i++)
			if (cardRange[i, 0] == -1) {
				int m = System.Convert.ToInt32(selectedCards[i].name.Split('_')[1]), n = System.Convert.ToInt32(selectedCards[i].name.Split('_')[2]);
				if (cards[m - 1][n - 1].name.Length > 1 && (cards[m - 1][n - 1].name[0] - 44032) % 28 == 0) {   // 카드 이름이 한 글자 이상이고 맨 앞글자가 받침 없는 한글인가?
					string currentName = cards[m - 1][n - 1].name;
					for (int j = cardRange[i == 0 ? i : i - 1, 1] + 1; j <= input.Length - currentName.Length; j++) {
						string p = input.Substring(j, currentName.Length);
						p = ((char)(p[0] - ((p[0] - 44032) % 28))).ToString() + p.Substring(1);
						if (p == currentName) {
							cardRange[i, 0] = j; cardRange[i, 1] = j + currentName.Length - 1;
							break;
						}
					}
				}
			}
		// 2-5. 앞 글자들만 검색 - V
		for (int i = 0; i < selectedCards.Count; i++)
			if (cardRange[i, 0] == -1) {
				int m = System.Convert.ToInt32(selectedCards[i].name.Split('_')[1]), n = System.Convert.ToInt32(selectedCards[i].name.Split('_')[2]);
				string origin = cards[m - 1][n - 1].name;   // 원래 단어
				if (origin.Length > 1) {
					for (int k = 1; k < origin.Length; k++) {               // 단어 길이 줄여가기
						string cut = origin.Substring(0, origin.Length - k);
						// 알파벳 1글자면 패스
						if (cut.Length == 1 && cut[0] < 256)
							break;
						for (int j = cardRange[i == 0 ? i : i - 1, 1] + 1; j <= input.Length - cut.Length; j++) {
							if (input.Substring(j, cut.Length) == cut) {
								cardRange[i, 0] = j; cardRange[i, 1] = j + cut.Length - 1;
								break;
							}
						}
					}
				}
			}
		// 2-6. 전 단어 다음 글자 - V
		for (int i = 0; i < selectedCards.Count; i++)
			if (cardRange[i, 0] == -1) {
				if (i == 0)
					cardRange[i, 0] = cardRange[i, 1] = 0;
				else
					cardRange[i, 0] = cardRange[i, 1] = cardRange[i - 1, 1] + 1;
			}
		// 겹친 구역 최종 보정(앞쪽 우선)
		for (int i = 1; i < selectedCards.Count; i++) {
			if (cardRange[i, 0] < cardRange[i - 1, 1]) {
				cardRange[i, 0] = cardRange[i - 1, 1] + 1;
				if (cardRange[i, 0] > cardRange[i, 1])
					cardRange[i, 1] = cardRange[i, 0];
			}
		}
		cardRange[0, 0] = 0;
		cardRange[selectedCards.Count - 1, 1] = input.Length - 1;
		// 3-1. 조합형 우측 확장
		for (int i = 0; i < selectedCards.Count - 1; i++) {
			while (input[cardRange[i, 1] + 1] != ' ' && cardRange[i, 1] + 1 != cardRange[i + 1, 0])
				cardRange[i, 1]++;
			//if (cardRange[i, 1] > cardRange[i, 0])
			//	cardRange[i, 1] = cardRange[i, 0];
		}
		// 3-3. 조합형 좌측 확장
		for (int i = 1; i < selectedCards.Count; i++) {
			cardRange[i, 0] = cardRange[i - 1, 1] + 1;
		}
		// 저장
		for (int i = 0; i < selectedCards.Count; i++) {
			Debug.LogFormat("{0}:{1}-{2}", i, cardRange[i, 0], cardRange[i, 1]);
			int s = cardRange[i, 0], l = cardRange[i, 1] - cardRange[i, 0] + 1;
			if (l <= 0)
				continue;
			int m = System.Convert.ToInt32(selectedCards[i].name.Split('_')[1]), n = System.Convert.ToInt32(selectedCards[i].name.Split('_')[2]);

			// 어근 확정
			originalRange[i, 0] = originalRange[i, 1] = -1;
			string origin = cards[m - 1][n - 1].name;
			bool get = false;
			if (origin.Length > 1) {
				for (int k = 0; k < origin.Length; k++) {               // 단어 길이 줄여가기
					string cut = origin.Substring(0, origin.Length - k);
					for (int j = originalRange[i == 0 ? i : i - 1, 1] + 1; j <= input.Length - cut.Length; j++) {
						if (input.Substring(j, cut.Length) == cut) {
							originalRange[i, 0] = j; originalRange[i, 1] = j + cut.Length - 1;
							get = true;
							break;
						}
					}
					if (get) break;
				}
			}
			if (originalRange[i, 0] == -1) {        // 단어로 어근을 찾지 못했다면 전 조합식 뒤의 공백 이후 첫 글자로 찾기
				int ss = s;
				while (input[ss] == ' ')
					ss++;
				originalRange[i, 0] = originalRange[i, 1] = ss;
			}
			string combine = input.Substring(s, l);                                                                     // 조합
			string root = input.Substring(originalRange[i, 0], originalRange[i, 1] - originalRange[i, 0] + 1);          // 어근
			string front = combine.Split(new[] { root }, StringSplitOptions.None)[0].TrimStart();                       // 앞쪽 형태소
			string rear = combine.Split(new[] { root }, StringSplitOptions.None)[1].TrimEnd();                          // 뒤쪽 형태소

			// 조합식 및 형태소 저장
			// 없는 이름 등록
			if (cards[m - 1][n - 1].name == "")
				cards[m - 1][n - 1].name = (combine[0] == ' ' ? combine.Substring(1) : combine);
			// 띄어쓰기 탐지
			if (!spacing && combine[0] == ' ')
				spacing = true;
			// 앞쪽으로 저장
			if (i != 0) {
				int mf = System.Convert.ToInt32(selectedCards[i - 1].name.Split('_')[1]);
				// 조합식
				CardCombine c = new CardCombine(combine, true, mf, mode && i == selectedCards.Count - 1 ? true : false, root);
				cards[m - 1][n - 1].CombineAdd(c, 2);
				if (isOriginal[i]) {            // 유사 형태 형태소 자동학습 - 지금 조합하려는 카드가 원본으로 사용된다면
					for (int j = 0; j < cards[m - 1].Count; j++) {      // 카드군의 카드 순회하기
						if (j == n - 1 || cards[m - 1][j].name == "")
							continue;

						string am = AutoMorpheme(m, mf, cards[m - 1][j].name, true);                    // 붙는 어근에 자동학습 알고리즘 적용
						string newstring = (combine[0] == ' ' ? " " : "") + am + cards[m - 1][j].name;
						if (am == "§")
							newstring = combine.Replace(cards[m - 1][n - 1].name, cards[m - 1][j].name);  // 초기 학습 상태라면 입력된 조합식을 이용해 초기 자동학습

						CardCombine cn = new CardCombine(newstring, true, mf, mode && i == selectedCards.Count - 1 ? true : false, cards[m - 1][j].name); // 새로운 조합식
						cards[m - 1][j].CombineAdd(cn, 1);              // 저장
					}
				}
				else {                          // 변형 어근 자동학습 - 원본으로 사용되지 않는다면
					for (int j = 0; j < cards[m - 1].Count; j++) {
						if (j != n - 1)
							AutoRoot(m, j, mf, true);
					}
				}
				// 형태소
				bool morpfound = false;
				foreach (Morpheme morp in morphemes)
					if (morp.name == root) {
						morpfound = true;
						if (front != "")
							morp.Connects(front, mf, true);
					}
				if (!morpfound) {
					morphemes.Add(new Morpheme(root, m, n));
					if (front != "")
						morphemes[morphemes.Count - 1].Connects(front, mf, true);
				}
			}
			// 뒷쪽으로 저장
			if (i != selectedCards.Count - 1) {
				int mr = System.Convert.ToInt32(selectedCards[i + 1].name.Split('_')[1]);
				// 조합식
				CardCombine c = new CardCombine(combine, false, mr, false, root);
				cards[m - 1][n - 1].CombineAdd(c, 2);
				if (isOriginal[i]) {            // 유사 형태 형태소 자동학습
					for (int j = 0; j < cards[m - 1].Count; j++) {
						if (j == n - 1 || cards[m - 1][j].name == "")
							continue;

						string am = AutoMorpheme(m, mr, cards[m - 1][j].name, false);                    // 붙는 어근에 자동학습 알고리즘 적용
						string newstring = (combine[0] == ' ' ? " " : "") + cards[m - 1][j].name + am;
						if (am == "§")
							newstring = combine.Replace(cards[m - 1][n - 1].name, cards[m - 1][j].name);  // 초기 학습 상태라면 입력된 조합식을 이용해 초기 자동학습

						CardCombine cn = new CardCombine(newstring, false, mr, false, cards[m - 1][j].name);
						cards[m - 1][j].CombineAdd(cn, 1);
					}
				}
				else {                          // 변형 어근 자동학습
					for (int j = 0; j < cards[m - 1].Count; j++) {
						if (j != n - 1)
							AutoRoot(m, j, mr, false);
					}
				}
				// 형태소
				bool morpfound = false;
				foreach (Morpheme morp in morphemes)
					if (morp.name == root) {
						morpfound = true;
						if (rear != "")
							morp.Connects(rear, mr, false);
					}
				if (!morpfound) {
					morphemes.Add(new Morpheme(root, m, n));
					if (rear != "")
						morphemes[morphemes.Count - 1].Connects(rear, mr, false);
				}
			}
		}
	}

	// 유사 형태 형태소 자동학습
	string AutoMorpheme(int main, int sub, string root, bool direction) {
		// 조건에 맞는 형태소 모으기
		Dictionary<string, int> roots = new Dictionary<string, int>();      // 어근과 그에 대응하는 형태소
		List<string> connects = new List<string>();                         // 붙을 수 있는 형태소
		foreach (var m in morphemes) {
			if (m.originGroup == main) {
				if (direction) {
					foreach (var c in m.fronts.Keys)
						if (m.fronts[c].connectWith.Contains(sub)) {
							if (!roots.Keys.Contains(m.name))
								roots.Add(m.name, -1);
							if (!connects.Contains(c))
								connects.Add(c);
							roots[m.name] = connects.IndexOf(c);
						}
				}
				else {
					foreach (var c in m.backs.Keys)
						if (m.backs[c].connectWith.Contains(sub)) {
							if (!roots.Keys.Contains(m.name))
								roots.Add(m.name, -1);
							if (!connects.Contains(c))
								connects.Add(c);
							roots[m.name] = connects.IndexOf(c);
						}
				}
			}
		}

		// 조건에 맞는 어근이 없으면(첫 학습이면) 원래 코드로 넘겨 기존 방식으로 자동학습
		if (roots.Count == 0)
			return "§";
		// 사용된 형태소가 하나뿐이라면 그걸 반환
		if (connects.Count == 1)
			return connects[0];
		// 이미 어근 리스트 안에 학습 데이터가 있으면 그걸 반환
		if (roots.ContainsKey(root))
			return connects[roots[root]];

		// 자동학습: 현재 어근이 가지고 있는 모든 형태소 중 지금 조합에 사용된 형태소가 있는지 확인
		string result = "";
		foreach (Morpheme m in morphemes) {
			if (m.name == root) {
				if (direction) {
					foreach (string s in m.fronts.Keys)
						if (connects.Contains(s)) {
							result = s;
							break;
						}
				}
				else {
					foreach (string s in m.backs.Keys)
						if (connects.Contains(s)) {
							result = s;
							break;
						}
				}
				break;
			}
		}
		if (result != "")
			return result;

		// 없는 경우 등장하는 형태소의 빈도에 따라 결정
		List<int> counts = new List<int>();
		for (int i = 0; i < connects.Count; i++)
			counts.Add(0);
		foreach (int i in roots.Values)
			if (i >= 0)
				counts[i]++;
		return connects[counts.IndexOf(counts.Max())];
	}

	// 어근 변형 자동학습
	void AutoRoot(int main, int num, int sub, bool dir) {
		// 현재 카드에서 생성된 어근이 다른 모든 조합식 추출
		List<CardCombine> currentCombines = new List<CardCombine>();
		// 같은 카드군의 다른 카드에서 현재 카드군 조합의 어근이 다른 조합식 추출
		List<CardCombine> otherSameCombines = new List<CardCombine>();
		// 같은 카드군의 다른 카드에서 현재 카드군 조합이 아닌 어근이 다른 조합식 추출
		List<CardCombine> otherDiffCombines = new List<CardCombine>();

		// 조건에 맞는 조합식 추출
		// 그 과정에서 현재 카드의 붙는 카드군, 방향이 같은 조합식을 발견하면 모두 가중치를 올려주고 학습 중단
		for (int i = 0; i < cards[main - 1].Count; i++) {
			bool find = false;
			foreach (CardCombine c in cards[main - 1][i].combines) {
				if (c.root != cards[main - 1][i].name && c.position == dir) {
					if (i == num - 1) {
						if (c.combinedWith == sub) {
							c.bias++;
							find = true;
						}
						else
							currentCombines.Add(c);
					}
					else {
						if (c.combinedWith == sub)
							otherSameCombines.Add(c);
						else
							otherDiffCombines.Add(c);
					}

				}
			}
			if (find)
				return;
		}
		// 만약 현재 카드의 어근 변화 사례 또는 다른 카드의 어근 변화 사례가 없으면 학습 미진행
		if (otherDiffCombines.Count == 0 || currentCombines.Count == 0)
			return;

		// 자동학습
		// 같은 카드군의 다른 카드에서 생성된 어근 변화 사례 중 현재 카드군과의 조합식 형태가 다른 카드군과의 조합에도 등장하는 경우를 뽑아 리스트를 만들고,
		// 현재 카드의 어근 변화 사례 중 리스트의 조합식과 조합되는 카드군이 같은 것이 발견될 경우 그 조합식을 현재 카드군 조합으로 학습
		List<CardCombine> matchedCombines = new List<CardCombine>();
		foreach (CardCombine os in otherSameCombines) {
			foreach (CardCombine od in otherDiffCombines) {
				if (os.style == od.style)
					matchedCombines.Add(od);
			}
		}
		foreach (CardCombine mc in matchedCombines) {
			foreach (CardCombine cc in currentCombines) {
				if (mc.combinedWith == cc.combinedWith) {
					CardCombine cn = new CardCombine(cc.style, dir, sub, cc.isEnd, cc.root);
					cards[main - 1][num - 1].CombineAdd(cn, 1);
				}
			}
		}
	}

	// 카드 개수 체크 및 cards 초기화
	void CardCheck() {
		for (int i = 0; i < cardImages.Count; i++) {
			cardCount.Add(0);
			cards.Add(new List<CardStatus>());
			for (int j = 0; j < cardImages[i].images.Count; j++) {
				cardCount[i]++;
				cards[i].Add(new CardStatus());
				cards[i][j].group = i + 1;
				cards[i][j].number = j + 1;
			}
		}
	}

	// 카드 데이터 불러오기
	public void CardLoad() {
		cards.Clear();
		morphemes.Clear();
		spacing = false;
		CardCheck();
		FileInfo fi = new FileInfo("cardData" + slot + ".data");
		if (!fi.Exists) {
			FileStream fs = File.Create("cardData" + slot + ".data");
			fs.Close();
			StreamWriter sw = new StreamWriter("cardData" + slot + ".data");
			sw.WriteLine("--------");
			sw.Close();
		}
		else {
			StreamReader sr = new StreamReader("cardData" + slot + ".data");
			string s;
			int g = 0, n = 0;
			// 카드와 조합식 저장
			while ((s = sr.ReadLine()) != "--------") {
				// 숫자면 해당 번호에 (줄바꿈) 이름 저장
				if (char.IsDigit(s[0])) {
					g = Convert.ToInt32(s.Split(' ')[0]);
					n = Convert.ToInt32(s.Split(' ')[1]);
					s = sr.ReadLine();
					cards[g - 1][n - 1].name = s;
				}
				// "면 지금 번호에 조합식 저장
				else if (s[0] == '"') {
					CardCombine c = new CardCombine();
					c.style = s.Split('"')[1];
					if (!spacing && c.style[0] == ' ')
						spacing = true;
					string p = s.Split('"')[2];
					c.position = p.Split(' ')[0] == "f" ? true : false;
					c.combinedWith = Convert.ToInt32(p.Split(' ')[1]);
					c.bias = Convert.ToInt32(p.Split(' ')[2]);
					c.position = p.Split(' ')[3] == "e" ? true : false;
					c.root = p.Split('#')[1];
					cards[g - 1][n - 1].combines.Add(c);
				}
				// §면 다음 번호로
				else if (s[0] == '§')
					continue;
			}
			// 형태소 저장
			while ((s = sr.ReadLine()) != null) {
				// §면 다음 번호로
				if (s[0] == '§')
					continue;
				// "면 지금 형태소에 조합 저장
				else if (s[0] == '"') {
					string p = s.Split('"')[1];
					string q = s.Split('"')[2];
					int b = Convert.ToInt32(q.Split(' ')[1]);
					MorphemeConnect mc = new MorphemeConnect();
					mc.bias = b;
					string[] k = sr.ReadLine().Split(' ');
					foreach (string kk in k)
						if (kk != "")
							mc.connectWith.Add(Convert.ToInt32(kk));
					if (q.Split(' ')[0] == "f")
						morphemes[morphemes.Count - 1].fronts.Add(p, mc);
					else
						morphemes[morphemes.Count - 1].backs.Add(p, mc);
				}
				// 아니면 새로운 형태소로 저장
				else {
					string name = s;
					string c = sr.ReadLine();
					int group = Convert.ToInt32(c.Split(' ')[0]);
					int num = Convert.ToInt32(c.Split(' ')[1]);
					Morpheme mm = new Morpheme(name, group, num);
					morphemes.Add(mm);
				}
			}
			sr.Close();
		}
	}

	// 저장 슬롯 변경
	public void DataChange() {
		slot = (slot + 1) % 10;
		slotText.GetComponent<Text>().text = "저장 슬롯: " + slot;
		CardLoad();
	}

	// 저장 데이터 초기화
	public void Reset() {
		File.Delete("cardData" + slot + ".data");
		FileStream fs = File.Create("cardData" + slot + ".data");
		fs.Close();
		CardCheck();
	}

	// 가중치 초기화
	public void BiasReset() {
		foreach (List<CardStatus> l in cards)
			foreach (CardStatus c in l)
				foreach (CardCombine cc in c.combines)
					cc.bias = 0;
		SaveData();
	}

	public Modes GetModes() => mode;
	public GameObject GetActiveCanvas() => Canvases[(int)mode];
	public List<int> GetCardCount() => cardCount;
	public bool GetSpacing() => spacing;

	public void Exit() {
		Application.Quit();
	}

	// 애니메이션
	IEnumerator RollAnim() {
		robot.GetComponent<Animator>().SetBool("Roll_Anim", true);
		yield return new WaitForSeconds(1);
		robot.GetComponent<Animator>().SetBool("Roll_Anim", false);
	}
	IEnumerator WalkAnim() {
		robot.GetComponent<Animator>().SetBool("Walk_Anim", true);
		yield return new WaitForSeconds(2);
		robot.GetComponent<Animator>().SetBool("Walk_Anim", false);
	}
}