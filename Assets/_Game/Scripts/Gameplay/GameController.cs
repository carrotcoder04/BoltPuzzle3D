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
		OnProgressChanged?.Invoke();
		if (Managers.Audio != null) Managers.Audio.PlaySfx(Sfx.Whoosh);
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
	void HandleTap(Vector2 screenPos)
	{
		if (InputLocked || _mainCam == null) return;
		Physics.SyncTransforms();
		var ray = _mainCam.ScreenPointToRay(screenPos);
		if (!Physics.Raycast(ray, out var hit, 200f, ~(1 << TopLayer), QueryTriggerInteraction.Ignore)) return;
		var screw = hit.collider.GetComponentInParent<Screw>();
		var board = screw != null ? screw.Board : hit.collider.GetComponentInParent<Board>();

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
		if (GameCreator.IsBlocked(screw)) { screw.ShakeBlocked(); return; }
		if (!TryRoute(screw, false)) screw.ShakeBlocked();
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
	public bool UseBroom()
	{
		bool any = false;
		foreach (var h in _holes)
		{
			if (h.Screw == null) continue;
			ToReserve(h.Take());
			any = true;
		}
		if (any)
		{
			OnUsingBroom = true;
			if (GameCanvas != null) GameCanvas.PlayBroom();
			DOVirtual.DelayedCall(0.5f, () => OnUsingBroom = false).SetLink(gameObject);
		}
		UpdateHoleNotice();
		return any;
	}

	/// <summary>Mở thêm ô hộp (AdBoxBtn1 = ô 3, AdBoxBtn2 = ô 4).</summary>
	public bool UnlockBoxSlot()
	{
		if (UnlockedBoxSlots >= MaxBoxSlots) return false;
		int slot = UnlockedBoxSlots++;
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

	void CheckFail()
	{
		if (IsGameOver || IsGameFail || MovingScrewCount > 0) return;
		if (FreeHoleCount() > 0) return;
		foreach (var h in _holes)
			if (h.Reserved || (h.Screw != null && FindBoxFor(h.Screw.ScrewColor) != null)) return;
		Physics.SyncTransforms();
		foreach (var s in _boardScrews)
			if (s != null && !s.IsRemoved && FindBoxFor(s.ScrewColor) != null && !GameCreator.IsBlocked(s)) return;
		Fail();
	}

	void Fail()
	{
		IsGameFail = true;
		SetMode(ToolMode.None);
		foreach (var h in _holes) if (h.IsActive) h.ShowFailNotice(true);
		if (Managers.Audio != null) Managers.Audio.PlaySfx(Sfx.Fail);
		if (Managers.Vibration != null) Managers.Vibration.Heavy();
		DOVirtual.DelayedCall(0.8f, () =>
		{
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
	public void Revive()
	{
		ReviveCount++;
		IsGameFail = false;
		foreach (var h in _holes) h.ShowFailNotice(false);
		Hole locked = null;
		foreach (var h in _holes) if (!h.IsActive) { locked = h; break; }
		if (locked != null) locked.Unlock();
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
		DOVirtual.DelayedCall(0.6f, () =>
		{
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
