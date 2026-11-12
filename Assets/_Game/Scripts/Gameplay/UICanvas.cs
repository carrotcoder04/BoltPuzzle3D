using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Canvas trong GameScene: Level, thanh tiến độ, nút Pause, vị trí hộp, nút mở thêm ô hộp, công cụ.</summary>
public class UICanvas : MonoBehaviour
{
	GameController _gc;
	TMP_Text _level, _progressNum, _tip;
	Image _progress;
	readonly RectTransform[] _boxPos = new RectTransform[GameController.MaxBoxSlots];
	Button[] _adBoxBtns = new Button[2];
	GameObject _broomSpine;

	void Awake()
	{
		var root = transform;
		_level = root.FindComp<TMP_Text>("SafeArea/TopUI/Level");
		var prog = root.Find("SafeArea/TopUI/Progress");
		if (prog != null)
		{
			_progress = prog.FindComp<Image>("Image");
			_progressNum = prog.FindComp<TMP_Text>("Num");
		}
		if (_progress != null)
		{
			_progress.type = Image.Type.Filled;
			_progress.fillMethod = Image.FillMethod.Horizontal;
			_progress.fillOrigin = 0;
			_progress.fillAmount = 0f;
		}
		for (int i = 0; i < _boxPos.Length; i++)
			_boxPos[i] = root.FindDeep("BoxPos_" + (i + 1)) as RectTransform;
		_tip = root.FindComp<TMP_Text>("ToolTipText");
		if (_tip != null) _tip.gameObject.SetActive(false);
		var broom = root.FindDeep("BroomSpine");
		_broomSpine = broom != null ? broom.gameObject : null;
		if (_broomSpine != null) _broomSpine.SetActive(false);

		// Các node chưa dùng / chỉ để debug
		foreach (var n in new[] { "ViewRoot", "ViewScale", "Ad_Mask", "AdMask", "PuzzlePreview", "ToolsSpine", "GameProgressSpark", "SpecialNode" })
			root.SetActive(n, false);
	}

	public RectTransform GetBoxPos(int slot) => slot >= 0 && slot < _boxPos.Length ? _boxPos[slot] : null;

	public void Bind(GameController gc)
	{
		_gc = gc;
		UIUtil.SetText(_level, "Level " + gc.Level);
		transform.BindButton("PauseBtn", gc.ShowPause);
		_adBoxBtns[0] = transform.BindButton("AdBoxBtn1", () => OnAdBox(0));
		_adBoxBtns[1] = transform.BindButton("AdBoxBtn2", () => OnAdBox(1));
		RefreshAdBox();
		gc.OnProgressChanged += RefreshProgress;
		gc.OnModeChanged += OnMode;
		foreach (var t in GetComponentsInChildren<ToolBase>(true)) t.Bind(gc);
		RefreshProgress();
	}

	void OnDestroy()
	{
		if (_gc == null) return;
		_gc.OnProgressChanged -= RefreshProgress;
		_gc.OnModeChanged -= OnMode;
	}

	void RefreshProgress()
	{
		if (_gc == null) return;
		float p = _gc.TotalScrews > 0 ? (float)_gc.CollectedScrews / _gc.TotalScrews : 0f;
		if (_progress != null) { _progress.DOKill(); _progress.DOFillAmount(p, 0.3f); }
		UIUtil.SetText(_progressNum, Mathf.RoundToInt(p * 100f) + "%");
	}

	void RefreshAdBox()
	{
		if (_gc == null) return;
		int startSlots = _gc.UnlockedBoxSlots;
		for (int i = 0; i < _adBoxBtns.Length; i++)
		{
			if (_adBoxBtns[i] == null) continue;
			bool unlocked = startSlots > 2 + i;
			bool next = startSlots == 2 + i;
			_adBoxBtns[i].gameObject.SetActive(!unlocked);
			_adBoxBtns[i].interactable = next;
			var txt = _adBoxBtns[i].GetComponentInChildren<TMP_Text>(true);
			var inv = Managers.Inventory;
			if (txt != null && inv != null)
				txt.text = inv.GetBooster(BoosterType.AddBox) > 0 ? "Free" : (GameConfig.Instance != null ? GameConfig.Instance.BoxSlotPrice.ToString() : "");
		}
	}

	/// <summary>Mở ô hộp 3/4: dùng booster AddBox nếu có, nếu không trừ coin.</summary>
	void OnAdBox(int index)
	{
		if (_gc == null || _gc.InputLocked || _gc.UnlockedBoxSlots != 2 + index) return;
		var inv = Managers.Inventory;
		var ud = Managers.UserData;
		int price = GameConfig.Instance != null ? GameConfig.Instance.BoxSlotPrice : 200;
		bool paid = (inv != null && inv.TryUseBooster(BoosterType.AddBox)) || (ud != null && ud.TrySpendCoins(price, "box_slot"));
		if (!paid) { UIUtil.Deny(_adBoxBtns[index]); return; }
		_gc.UnlockBoxSlot();
		RefreshAdBox();
	}

	void OnMode(ToolMode m)
	{
		if (_tip == null) return;
		_tip.gameObject.SetActive(m != ToolMode.None);
		if (m == ToolMode.Drill) _tip.text = "Tap any screw to drill it out";
		else if (m == ToolMode.Hammer) _tip.text = "Tap a plate to smash it";
	}

	public void PlayBroom()
	{
		if (_broomSpine == null) return;
		_broomSpine.SetActive(true);
		// chạy chậm lại cho người chơi kịp thấy chổi quét qua, và luôn phát lại từ đầu
		var sg = _broomSpine.GetComponentInChildren<Spine.Unity.SkeletonGraphic>(true);
		if (sg != null)
		{
			sg.timeScale = 0.7f;
			if (sg.AnimationState != null && !string.IsNullOrEmpty(sg.startingAnimation))
				sg.AnimationState.SetAnimation(0, sg.startingAnimation, false);
		}
		DOVirtual.DelayedCall(2.2f, () => { if (_broomSpine != null) _broomSpine.SetActive(false); }).SetLink(gameObject);
	}
}
