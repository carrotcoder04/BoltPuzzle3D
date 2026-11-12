using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Snui;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum ToolMode { None, Drill, Hammer }

/// <summary>
/// Điều phối một màn chơi: chạm ốc → hộp cùng màu hoặc lỗ chờ; hộp đủ 3 ốc → hộp mới;
/// hết ốc → thắng; lỗ chờ đầy và không còn nước đi → thua (có hồi sinh).
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameController : MonoBehaviour
{
	public const int TopLayer = 8;
	public const string HomeSceneName = "HomeScene";
	public const string GameSceneName = "GameScene";

	public static GameController Instance { get; private set; }

	// Field giữ nguyên tên theo dữ liệu gốc của scene
	public UICanvas GameCanvas;
	public GameCreator GameCreator;
	public Transform GameRoot;
	public Transform DropRoot;
	public Transform BoardRoot;
	public Transform BoxRoot;
	public Transform HoleRoot;
	public List<Transform> HoleParents = new List<Transform>();
	public List<Board> Boards = new List<Board>();
	public List<BoxScript> Boxes = new List<BoxScript>();
	public int MovingScrewCount;
	public bool IsGameOver;
	public bool IsGameFail;
	public bool OnUsingHammer;
	public bool OnUsingBroom;
	public UnityEvent OnEventLoadingPuzzle = new UnityEvent();

	public const int MaxBoxSlots = 4;

	public int Level { get; private set; }
	public int TotalScrews { get; private set; }
	public int CollectedScrews { get; private set; }
	public ToolMode Mode { get; private set; }
	public int ReviveCount { get; private set; }
	public int UnlockedBoxSlots { get; private set; }

	public event System.Action OnProgressChanged;
	public event System.Action<ToolMode> OnModeChanged;

	GameConfig _cfg;
	Camera _mainCam;
	Camera _uiCam;
	RotateHelper _rotate;
	readonly List<Hole> _holes = new List<Hole>();
	readonly BoxScript[] _slots = new BoxScript[MaxBoxSlots];
	readonly Queue<int> _queue = new Queue<int>();
	readonly List<Screw> _reserve = new List<Screw>();
	readonly List<Screw> _boardScrews = new List<Screw>();
	GameObject _oneLeft;
	float _startTime;
	int _boostersUsed;
	int _taps;
	Transform _reserveRoot;
	bool _checkPending;

	// ---------------------------------------------------------------- setup
	void Awake()
	{
		Instance = this;
		Managers.Ensure();
		_cfg = GameConfig.Instance;
		ResolveReferences();
	}

	void OnDestroy()
	{
		if (Instance == this) Instance = null;
		DOTween.KillAll();
	}

	void ResolveReferences()
	{
		if (GameRoot == null) GameRoot = FindRoot("GameRoot");
		if (DropRoot == null) DropRoot = FindRoot("DropRoot");
		if (BoxRoot == null) BoxRoot = FindRoot("BoxRoot");
		if (HoleRoot == null) HoleRoot = FindRoot("HoleRoot");
		if (BoardRoot == null) BoardRoot = GameRoot;
		if (GameCreator == null) GameCreator = GetComponent<GameCreator>() ?? gameObject.AddComponent<GameCreator>();
		if (GameCanvas == null) GameCanvas = FindAnyObjectByType<UICanvas>(FindObjectsInactive.Include);

		foreach (var c in Camera.allCameras)
		{
			if (c.name == "MainCamera" || c.CompareTag("MainCamera")) _mainCam = _mainCam ?? c;
			if (c.name == "UICamera") _uiCam = c;
		}
		if (_mainCam == null) _mainCam = Camera.main;
		FitCamerasToAspect();

		if (GameRoot != null)
		{
			_rotate = GameRoot.GetComponent<RotateHelper>() ?? GameRoot.gameObject.AddComponent<RotateHelper>();
			if (GameRoot.GetComponent<ZoomGesture>() == null) GameRoot.gameObject.AddComponent<ZoomGesture>();
			if (GameRoot.GetComponent<TwoFingerRotateGesture>() == null) GameRoot.gameObject.AddComponent<TwoFingerRotateGesture>();
			var rb = GameRoot.GetComponent<Rigidbody>();
			if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
		}

		_holes.Clear();
		HoleParents.Clear();
		if (HoleRoot != null)
		{
			for (int i = 1; i <= 7; i++)
			{
				var t = HoleRoot.Find("Hole" + i);
				if (t == null) continue;
				var h = t.GetComponent<Hole>() ?? t.gameObject.AddComponent<Hole>();
				h.Init();
				_holes.Add(h);
				HoleParents.Add(t);
			}
			var one = HoleRoot.Find("OneLeft");
			_oneLeft = one != null ? one.gameObject : null;
			_reserveRoot = new GameObject("Reserve").transform;
			_reserveRoot.SetParent(HoleRoot, false);
		}
		if (_oneLeft != null) _oneLeft.SetActive(false);
	}

	/// <summary>
	/// Camera ortho giữ chiều cao cố định, nên trên máy dài và hẹp (19.5:9 trở lên) mô hình và dãy hộp bị cắt hai bên.
	/// Khi màn hình hẹp hơn thiết kế 750x1624 thì nới orthographicSize để giữ đủ bề ngang.
	/// </summary>
	void FitCamerasToAspect()
	{
		const float refAspect = 750f / 1624f;
		float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
		if (aspect >= refAspect) return;
		float k = refAspect / aspect;
		foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
		{
			if (!c.orthographic || c == _uiCam || c.name.Contains("UI")) continue;
			c.orthographicSize *= k;
		}
	}

	static Transform FindRoot(string name)
	{
		var go = GameObject.Find(name);
		return go != null ? go.transform : null;
	}

	void Start()
	{
		if (GameCanvas != null) DialogManager.DefaultRoot = GameCanvas.transform;
		if (_rotate != null) _rotate.OnTap += HandleTap;
		StartLevel();
	}

	void StartLevel()
	{
		var ud = Managers.UserData;
		Level = ud != null ? ud.CurrentLevel : 1;
		if (ud != null) ud.OnLevelStart();
		OnEventLoadingPuzzle?.Invoke();

		var puzzle = GameCreator.Create(Level, GameRoot);
		if (puzzle == null) return;
		Boards.Clear();
		Boards.AddRange(GameCreator.Boards);
		_boardScrews.Clear();
		_boardScrews.AddRange(GameCreator.Screws);
		TotalScrews = _boardScrews.Count;
		CollectedScrews = 0;
		foreach (var c in GameCreator.BoxQueue) _queue.Enqueue(c);

		UnlockedBoxSlots = Mathf.Clamp(_cfg != null ? _cfg.StartBoxSlots : 2, 1, MaxBoxSlots);
		for (int i = 0; i < UnlockedBoxSlots; i++) SpawnBox(i, i * 0.08f);

		if (GameCanvas != null) GameCanvas.Bind(this);
		Canvas.ForceUpdateCanvases();
		LayoutHoles(false);
		OnProgressChanged?.Invoke();
		if (Managers.Audio != null) Managers.Audio.PlaySfx(Sfx.Whoosh);
		_startTime = Time.time;
		AnalyticsManager.LevelStart(Level, puzzle.name, ud != null ? ud.Data.LevelAttempt : 1);
	}

	public bool InputLocked => IsGameOver || IsGameFail || DialogManager.Count > 0;

	// ---------------------------------------------------------------- boxes
	Vector3 SlotWorldPos(int slot)
	{
		var rt = GameCanvas != null ? GameCanvas.GetBoxPos(slot) : null;
		if (rt == null || _mainCam == null)
			return (BoxRoot != null ? BoxRoot.position : Vector3.zero) + Vector3.right * (slot - 1.5f) * 2.5f;
		var canvas = rt.GetComponentInParent<Canvas>();
		var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? (canvas.worldCamera != null ? canvas.worldCamera : _uiCam) : null;
		Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, rt.position);
		float depth = BoxRoot != null ? Mathf.Abs(BoxRoot.position.z - _mainCam.transform.position.z) : 15f;
		if (depth < 1f) depth = 15f;
		return _mainCam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, depth));
	}

	/// <summary>Đường kính đầu ốc (world) khi nằm trong lỗ hộp / lỗ chờ.</summary>
	public float ScrewHeadWorldSize = 0.62f;

	/// <summary>Chuyển một RectTransform UI sang toạ độ world của MainCamera ở độ sâu của BoxRoot.</summary>
	Vector3 UiToWorld(RectTransform rt)
	{
		var canvas = rt.GetComponentInParent<Canvas>();
		var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? (canvas.worldCamera != null ? canvas.worldCamera : _uiCam) : null;
		Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, rt.position);
		float depth = BoxRoot != null ? Mathf.Abs(BoxRoot.position.z - _mainCam.transform.position.z) : 15f;
		if (depth < 1f) depth = 15f;
		return _mainCam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, depth));
	}

	/// <summary>
	/// Xếp các lỗ chờ đang mở thành một hàng dưới dãy hộp (vị trí HoleContainer trên UI),
	/// khoảng cách theo bề rộng dãy hộp. Prefab gốc để mọi lỗ ở cùng một điểm.
	/// </summary>
	void LayoutHoles(bool animate)
	{
		if (HoleRoot == null || _mainCam == null) return;
		var hc = GameCanvas != null ? GameCanvas.transform.FindDeep("HoleContainer") as RectTransform : null;
		if (hc != null) HoleRoot.position = UiToWorld(hc);
		float width = 7.2f;
		if (GameCanvas != null && GameCanvas.GetBoxPos(0) != null && GameCanvas.GetBoxPos(3) != null)
			width = Mathf.Abs(SlotWorldPos(3).x - SlotWorldPos(0).x) * 1.15f;
		float step = width / 6f;
		var active = new List<Hole>();
		foreach (var h in _holes) if (h.IsActive) active.Add(h);
		for (int i = 0; i < active.Count; i++)
		{
			var lp = new Vector3((i - (active.Count - 1) * 0.5f) * step, 0f, 0f);
			var t = active[i].transform;
			if (animate) t.DOLocalMove(lp, 0.25f).SetEase(Ease.OutQuad);
			else t.localPosition = lp;
		}
		if (_oneLeft != null) _oneLeft.transform.localPosition = new Vector3(0f, -step * 0.9f, 0f);
	}

	void SpawnBox(int slot, float delay = 0f)
	{
		if (_slots[slot] != null || _queue.Count == 0 || _cfg == null || _cfg.BoxPrefab == null) return;
		int color = _queue.Dequeue();
		var go = Instantiate(_cfg.BoxPrefab, BoxRoot, false);
		go.transform.position = SlotWorldPos(slot);
		var box = go.GetComponent<BoxScript>();
		box.Setup(color, slot);
		box.Completed += OnBoxCompleted;
		_slots[slot] = box;
		Boxes.Add(box);
		var s = go.transform.localScale;
		go.transform.localScale = Vector3.zero;
		go.transform.DOScale(s, 0.3f).SetDelay(delay).SetEase(Ease.OutBack).OnComplete(() => PullMatching(box));
	}

	BoxScript FindBoxFor(int color)
	{
		foreach (var b in _slots)
			if (b != null && b.Color == color && !b.IsFull && !b.IsClosing) return b;
		return null;
	}

	void OnBoxCompleted(BoxScript box)
	{
		Boxes.Remove(box);
		if (box.Slot >= 0 && box.Slot < MaxBoxSlots && _slots[box.Slot] == box) _slots[box.Slot] = null;
		CollectedScrews += box.MaxScrewCount;
		OnProgressChanged?.Invoke();
		if (CollectedScrews >= TotalScrews) { Win(); return; }
		SpawnBox(box.Slot, 0.05f);
		RequestCheck();
	}

	/// <summary>Kéo ốc cùng màu từ lỗ chờ / kho dự trữ vào hộp vừa xuất hiện.</summary>
	void PullMatching(BoxScript box)
	{
		if (box == null || box.IsClosing) return;
		foreach (var h in _holes)
		{
			if (box.IsFull) return;
			if (h.Screw != null && h.Screw.ScrewColor == box.Color)
				SendToBox(h.Take(), box);
		}
		for (int i = _reserve.Count - 1; i >= 0 && !box.IsFull; i--)
		{
			var s = _reserve[i];
			if (s.ScrewColor != box.Color) continue;
			_reserve.RemoveAt(i);
			s.gameObject.SetActive(true);
			s.transform.position = HoleRoot != null ? HoleRoot.position : s.transform.position;
			SendToBox(s, box);
		}
		UpdateHoleNotice();
	}

	void SendToBox(Screw s, BoxScript box)
	{
		var t = box.ReserveSlot();
		if (t == null) return;
		MovingScrewCount++;
		s.FlyTo(t, 0.3f, () =>
		{
			MovingScrewCount--;
			if (box != null) box.OnScrewArrived(s);
			RequestCheck();
		});
	}

	// ---------------------------------------------------------------- input
	static readonly List<RaycastHit> _hitBuf = new List<RaycastHit>();

	/// <summary>
	/// Tìm ốc dưới điểm chạm. Collider tấm là convex hull nên thường trùm lên đầu ốc của chính nó,
	/// vì vậy bỏ qua tấm chứa ốc; chỉ coi là bị che khi có tấm KHÁC nằm trước ốc.
	/// Collider của ốc trong prefab gốc là trigger, nên phải quét cả trigger.
	/// </summary>
	Screw PickScrew(Ray ray, float radius, out Board firstBoard)
	{
		firstBoard = null;
		int mask = ~(1 << TopLayer);
		var hits = radius > 0f
			? Physics.SphereCastAll(ray, radius, 200f, mask, QueryTriggerInteraction.Collide)
			: Physics.RaycastAll(ray, 200f, mask, QueryTriggerInteraction.Collide);
		_hitBuf.Clear();
		_hitBuf.AddRange(hits);
		_hitBuf.Sort((a, b) => a.distance.CompareTo(b.distance));
		foreach (var h in _hitBuf)
		{
			var s = h.collider.GetComponentInParent<Screw>();
			if (s != null)
			{
				if (s.IsRemoved) continue;
				if (firstBoard == null || s.Board == firstBoard) return s;
				return null;
			}
			if (h.collider.isTrigger) continue;
			var b = h.collider.GetComponentInParent<Board>();
			if (b == null || b.IsFallen) continue;
			if (firstBoard == null) firstBoard = b;
			else if (b != firstBoard) return null;
		}
		return null;
	}

	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	static void LogTap(Screw s, string msg)
	{
		Debug.Log("[Tap] " + (s != null ? (s.Board != null ? s.Board.name : "-") + "/" + s.name + " color=" + s.ScrewColor : "") + " " + msg);
	}

	/// <summary>Báo cho người chơi biết ốc không đi được vì hết lỗ chờ và không có hộp cùng màu: các lỗ nhấp nháy đỏ, nảy lên.</summary>
	void FlashFullHoles()
	{
		foreach (var h in _holes)
		{
			if (!h.IsActive) continue;
			var hh = h;
			hh.ShowFailNotice(true);
			hh.transform.DOKill(true);
			hh.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 6, 0.6f).SetLink(hh.gameObject);
			DOVirtual.DelayedCall(0.6f, () => { if (hh != null && !IsGameFail) hh.ShowFailNotice(false); }).SetLink(hh.gameObject);
		}
	}

	void HandleTap(Vector2 screenPos)
	{
		if (InputLocked || _mainCam == null)
		{
			LogTap(null, "input locked: over=" + IsGameOver + " fail=" + IsGameFail + " dialogs=" + DialogManager.Count);
			return;
		}
		Physics.SyncTransforms();
		var ray = _mainCam.ScreenPointToRay(screenPos);
		Board board;
		var screw = PickScrew(ray, 0f, out board);
		if (screw == null && Mode != ToolMode.Hammer)
		{
			Board b2;
			screw = PickScrew(ray, 0.2f, out b2);
			if (board == null) board = b2;
		}
		if (screw != null) board = screw.Board;
		if (screw == null && board == null) { LogTap(null, "nothing hit"); return; }

		if (Mode == ToolMode.Hammer)
		{
			if (board != null && !board.IsFallen) UseHammerOn(board);
			return;
		}
		if (screw == null || screw.IsRemoved) return;
		if (Mode == ToolMode.Drill)
		{
			if (TryRoute(screw, true)) CompleteTool(BoosterType.Drill, Sfx.ToolDrill);
			return;
		}
		if (GameCreator.IsBlocked(screw, null, out var blocker))
		{
			LogTap(screw, "blocked by " + (blocker != null ? blocker.name : "?"));
			screw.ShakeBlocked();
			return;
		}
		if (!TryRoute(screw, false)) { LogTap(screw, "no box/hole free"); screw.ShakeBlocked(); FlashFullHoles(); return; }
		if (PlayableConfig.IsPlayable && PlayableConfig.MaxScrewTaps > 0 && ++_taps >= PlayableConfig.MaxScrewTaps)
			DOVirtual.DelayedCall(0.8f, () => { if (!IsGameOver) { IsGameOver = true; PlayableEndcard.Show("Great job!"); } }).SetLink(gameObject);
	}

	/// <summary>Đưa ốc trên tấm tới hộp cùng màu, nếu không thì vào lỗ chờ; allowReserve = cho vào kho ẩn khi hết chỗ.</summary>
	bool TryRoute(Screw s, bool allowReserve)
	{
		var box = FindBoxFor(s.ScrewColor);
		Hole hole = null;
		if (box == null)
		{
			hole = FreeHole();
			if (hole == null && !allowReserve) return false;
		}
		_boardScrews.Remove(s);
		MovingScrewCount++;
		if (box != null)
		{
			var t = box.ReserveSlot();
			s.Extract(() => s.FlyTo(t, 0.35f, () =>
			{
				MovingScrewCount--;
				if (box != null) box.OnScrewArrived(s);
				RequestCheck();
			}));
		}
		else if (hole != null)
		{
			hole.Reserve();
			s.Extract(() => s.FlyTo(hole.Pos, 0.35f, () =>
			{
				MovingScrewCount--;
				hole.Place(s);
				var b = FindBoxFor(s.ScrewColor);
				if (b != null) SendToBox(hole.Take(), b);
				UpdateHoleNotice();
				RequestCheck();
			}));
			UpdateHoleNotice();
		}
		else
		{
			s.Extract(() =>
			{
				MovingScrewCount--;
				ToReserve(s);
				RequestCheck();
			});
		}
		return true;
	}

	void ToReserve(Screw s)
	{
		s.transform.DOKill();
		s.transform.SetParent(_reserveRoot, true);
		s.gameObject.SetActive(false);
		_reserve.Add(s);
	}

	Hole FreeHole()
	{
		foreach (var h in _holes) if (h.IsActive && h.IsFree) return h;
		return null;
	}

	int FreeHoleCount()
	{
		int n = 0;
		foreach (var h in _holes) if (h.IsActive && h.IsFree) n++;
		return n;
	}

	void UpdateHoleNotice()
	{
		if (_oneLeft != null) _oneLeft.SetActive(!IsGameOver && FreeHoleCount() == 1);
	}

	// ---------------------------------------------------------------- tools
	public void SetMode(ToolMode m)
	{
		Mode = m;
		OnUsingHammer = m == ToolMode.Hammer;
		OnModeChanged?.Invoke(m);
	}

	/// <summary>Bấm nút công cụ. Trả false nếu không dùng được.</summary>
	public bool UseTool(BoosterType type)
	{
		if (InputLocked) return false;
		switch (type)
		{
			case BoosterType.Drill:
				SetMode(Mode == ToolMode.Drill ? ToolMode.None : ToolMode.Drill);
				return true;
			case BoosterType.Hammer:
				SetMode(Mode == ToolMode.Hammer ? ToolMode.None : ToolMode.Hammer);
				return true;
			case BoosterType.Broom:
				if (!UseBroom()) return false;
				CompleteTool(BoosterType.Broom, Sfx.ToolBroom);
				return true;
		}
		return false;
	}

	void CompleteTool(BoosterType type, string sfx)
	{
		var inv = Managers.Inventory;
		if (inv != null) inv.TryUseBooster(type);
		_boostersUsed++;
		AnalyticsManager.BoosterUse(type);
		if (Managers.Audio != null) Managers.Audio.PlaySfx(sfx);
		SetMode(ToolMode.None);
	}

	void UseHammerOn(Board board)
	{
		board.Smash();
		var list = new List<Screw>(board.Screws);
		foreach (var s in list) TryRoute(s, true);
		if (list.Count == 0) board.Fall();
		if (Managers.Vibration != null) Managers.Vibration.Heavy();
		CompleteTool(BoosterType.Hammer, Sfx.ToolHammer);
	}

	/// <summary>Chổi: dọn toàn bộ ốc trong lỗ chờ vào kho ẩn (sẽ tự bay vào hộp cùng màu sau).</summary>
	/// <summary>Thời gian tối đa chờ chổi quét qua; ốc nào chổi chưa chạm tới thì văng theo nhịp sau đó.</summary>
	const float BroomMaxWait = 2.0f;

	public bool UseBroom()
	{
		var list = new List<Screw>();
		foreach (var h in _holes)
			if (h.Screw != null) list.Add(h.Take());
		bool any = list.Count > 0;
		if (any)
		{
			OnUsingBroom = true;
			if (GameCanvas != null) GameCanvas.PlayBroom();
			StartCoroutine(BroomSweep(list));
		}
		UpdateHoleNotice();
		return any;
	}

	/// <summary>
	/// Đồng bộ với Spine cây chổi: mỗi khung hình lấy vị trí đầu chổi trên màn hình,
	/// ốc nào bị đầu chổi lướt qua (theo trục ngang) thì văng đi theo hướng chổi đang quét.
	/// </summary>
	IEnumerator BroomSweep(List<Screw> list)
	{
		// sắp trái → phải để phần dự phòng cũng quét theo thứ tự
		var cam = _mainCam != null ? _mainCam : Camera.main;
		if (cam != null) list.Sort((a, b) => cam.WorldToScreenPoint(a.transform.position).x.CompareTo(cam.WorldToScreenPoint(b.transform.position).x));
		var pending = new List<Screw>(list);
		float t = 0f;
		bool hasPrev = false;
		Vector2 prev = Vector2.zero;
		float hitRange = Screen.width * 0.035f;
		while (pending.Count > 0 && t < BroomMaxWait && cam != null)
		{
			if (GameCanvas != null && GameCanvas.TryGetBroomScreenPos(out var bp))
			{
				float step = hasPrev ? bp.x - prev.x : 0f;
				float dir = step != 0f ? Mathf.Sign(step) : 1f;
				// Spine có thể nhảy từ tư thế mặc định sang tư thế đầu ở vài khung đầu: bỏ qua các bước nhảy lớn
				bool jump = Mathf.Abs(step) > Screen.width * 0.08f;
				bool moving = hasPrev && !jump && Mathf.Abs(step) > 0.5f && t > 0.05f;
#if UNITY_EDITOR
				Debug.Log("[Broom] t=" + t.ToString("F2") + " x=" + bp.x.ToString("F0") + " y=" + bp.y.ToString("F0") + " step=" + step.ToString("F0") + (jump ? " JUMP" : ""));
#endif
				for (int i = pending.Count - 1; i >= 0; i--)
				{
					var s = pending[i];
					if (s == null) { pending.RemoveAt(i); continue; }
					float sx = cam.WorldToScreenPoint(s.transform.position).x;
					bool crossed = (prev.x - sx) * (bp.x - sx) <= 0f;
					if (moving && (crossed || Mathf.Abs(bp.x - sx) < hitRange))
					{
#if UNITY_EDITOR
						Debug.Log("[Broom] hit " + s.name + " sx=" + sx.ToString("F0") + " at t=" + t.ToString("F2"));
#endif
						SweepScrew(s, dir, 0f);
						pending.RemoveAt(i);
					}
				}
				prev = bp;
				hasPrev = true;
			}
			t += Time.deltaTime;
			yield return null;
		}
		for (int i = 0; i < pending.Count; i++)
			if (pending[i] != null) SweepScrew(pending[i], 1f, i * 0.1f);
		yield return new WaitForSeconds(0.7f);
		OnUsingBroom = false;
	}

	void SweepScrew(Screw s, float dir, float delay)
	{
		var cam = _mainCam != null ? _mainCam : Camera.main;
		var right = (cam != null ? cam.transform.right : Vector3.right) * (dir >= 0f ? 1f : -1f);
		var up = cam != null ? cam.transform.up : Vector3.up;
		var t = s.transform;
		t.DOKill();
		var p0 = t.position;
		var sc = t.localScale;
		var seq = DOTween.Sequence();
		if (delay > 0f) seq.AppendInterval(delay);
		seq.AppendCallback(() => FxManager.ScrewPop(t.position, s.DisplayColor));
		// bị chổi hất: bật lên + xoay, rồi văng ra theo hướng quét và nhỏ dần
		seq.Append(t.DOMove(p0 + up * 0.7f + right * 0.6f, 0.12f).SetEase(Ease.OutQuad));
		seq.Join(t.DOScale(sc * 1.2f, 0.12f).SetEase(Ease.OutQuad));
		seq.Append(t.DOMove(p0 + up * 1.8f + right * 9f, 0.4f).SetEase(Ease.InQuad));
		seq.Join(t.DOLocalRotate(new Vector3(0f, 0f, -540f * (dir >= 0f ? 1f : -1f)), 0.4f, RotateMode.LocalAxisAdd).SetEase(Ease.InQuad));
		seq.Join(t.DOScale(sc * 0.3f, 0.4f).SetEase(Ease.InQuad));
		seq.SetLink(s.gameObject);
		seq.OnComplete(() => { t.localScale = sc; ToReserve(s); });
	}

	/// <summary>Mở thêm ô hộp (AdBoxBtn1 = ô 3, AdBoxBtn2 = ô 4).</summary>
	public bool UnlockBoxSlot()
	{
		if (UnlockedBoxSlots >= MaxBoxSlots) return false;
		int slot = UnlockedBoxSlots++;
		AnalyticsManager.BoosterUse(BoosterType.AddBox);
		SpawnBox(slot);
		return true;
	}

	// ---------------------------------------------------------------- win / fail
	public void OnBoardFallen(Board b)
	{
		Boards.Remove(b);
		if (Managers.Vibration != null) Managers.Vibration.Light();
	}

	void RequestCheck()
	{
		if (_checkPending || IsGameOver) return;
		_checkPending = true;
		DOVirtual.DelayedCall(0.05f, () => { _checkPending = false; CheckFail(); }).SetLink(gameObject);
	}

	void RequestCheckLater(float delay)
	{
		DOVirtual.DelayedCall(delay, RequestCheck).SetLink(gameObject);
	}

	void CheckFail()
	{
		if (IsGameOver || IsGameFail || MovingScrewCount > 0) return;
		if (FreeHoleCount() > 0) return;
		foreach (var h in _holes)
			if (h.Reserved || (h.Screw != null && FindBoxFor(h.Screw.ScrewColor) != null)) return;
		// Hộp đang đóng nắp sẽ được thay bằng hộp mới, có thể hút ốc trong lỗ ra: chờ hộp mới xuất hiện rồi xét lại.
		foreach (var b in _slots)
			if (b != null && b.IsClosing) { RequestCheckLater(0.3f); return; }
		// Luật thua: mọi lỗ chờ đã đầy và không ốc nào trong lỗ vào được hộp hiện có => thua ngay,
		// không phụ thuộc vào việc trên mô hình còn ốc cùng màu hộp hay không (giống thể loại Screw Jam).
		Fail();
	}

	void Fail()
	{
		IsGameFail = true;
		SetMode(ToolMode.None);
		foreach (var h in _holes) if (h.IsActive) h.ShowFailNotice(true);
		if (Managers.Audio != null) Managers.Audio.PlaySfx(Sfx.Fail);
		if (Managers.Vibration != null) Managers.Vibration.Heavy();
		AnalyticsManager.LevelFail(Level, "holes_full", TotalScrews - CollectedScrews);
		DOVirtual.DelayedCall(0.8f, () =>
		{
			if (PlayableConfig.IsPlayable) { IsGameOver = true; PlayableEndcard.Show("Try the full game!"); return; }
			if (_cfg != null && _cfg.FailDialog != null) DialogManager.Show(_cfg.FailDialog);
		}).SetLink(gameObject);
	}

	public int NextRevivePrice
	{
		get
		{
			var p = _cfg != null ? _cfg.RevivePrices : null;
			if (p == null || p.Length == 0) return 0;
			return p[Mathf.Min(ReviveCount, p.Length - 1)];
		}
	}

	/// <summary>Hồi sinh: mở lỗ Hole6/7 nếu còn, nếu không thì dùng hiệu ứng chổi.</summary>
	/// <summary>Hồi sinh sẽ mở thêm lỗ (còn lỗ khoá) hay dọn sạch lỗ chờ.</summary>
	public bool ReviveAddsHole
	{
		get { foreach (var h in _holes) if (!h.IsActive) return true; return false; }
	}

	public void Revive(string method = "coin")
	{
		ReviveCount++;
		AnalyticsManager.LevelRevive(Level, method);
		IsGameFail = false;
		foreach (var h in _holes) h.ShowFailNotice(false);
		Hole locked = null;
		foreach (var h in _holes) if (!h.IsActive) { locked = h; break; }
		if (locked != null) { locked.Unlock(); LayoutHoles(true); }
		else UseBroom();
		if (Managers.Audio != null) Managers.Audio.PlaySfx(Sfx.Revive);
		UpdateHoleNotice();
	}

	void Win()
	{
		if (IsGameOver) return;
		IsGameOver = true;
		SetMode(ToolMode.None);
		UpdateHoleNotice();
		if (Managers.Audio != null) Managers.Audio.PlaySfx(Sfx.Win);
		FxManager.WinConfetti();
		AnalyticsManager.LevelWin(Level, Time.time - _startTime, TotalScrews, _boostersUsed);
		DOVirtual.DelayedCall(0.6f, () =>
		{
			if (PlayableConfig.IsPlayable) { PlayableEndcard.Show("Level Complete!"); return; }
			if (_cfg != null && _cfg.WinDialog != null) DialogManager.Show(_cfg.WinDialog);
			else ClaimWinAndGoHome(1);
		}).SetLink(gameObject);
	}

	public int WinReward => _cfg != null ? _cfg.WinCoinReward : 20;

	public void ClaimWinAndGoHome(int multiplier)
	{
		var ud = Managers.UserData;
		if (ud != null)
		{
			ud.AddCoins(WinReward * Mathf.Max(1, multiplier), "level_win");
			ud.CompleteLevel();
		}
		GoHome();
	}

	/// <summary>Bỏ màn (Pause → Home, Fail → Close): trừ 1 tim rồi về Home.</summary>
	public void QuitLevel()
	{
		if (!IsGameOver && Managers.Inventory != null) Managers.Inventory.UseHeart();
		IsGameOver = true;
		GoHome();
	}

	/// <summary>Chơi lại: trừ 1 tim, nạp lại GameScene.</summary>
	public void Retry()
	{
		var inv = Managers.Inventory;
		if (inv != null) inv.UseHeart();
		if (inv != null && !inv.HasHeart) { GoHome(); return; }
		IsGameOver = true;
		SceneManager.LoadScene(GameSceneName);
	}

	public void GoHome()
	{
		DialogManager.CloseAll();
		if (Application.CanStreamedLevelBeLoaded(HomeSceneName)) SceneManager.LoadScene(HomeSceneName);
		else SceneManager.LoadScene(GameSceneName);
	}

	public void ShowPause()
	{
		if (InputLocked || _cfg == null || _cfg.PauseDialog == null) return;
		DialogManager.Show(_cfg.PauseDialog);
	}

#if UNITY_EDITOR
	[ContextMenu("Debug/Win now")]
	void DebugWin() { CollectedScrews = TotalScrews; Win(); }
	[ContextMenu("Debug/Fail now")]
	void DebugFail() => Fail();
#endif
}
