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

	// Animation "animation" của clean.skel (đo bằng spine-csharp): chổi bay vào từ góc dưới phải (0–0.75s),
	// sự kiện "hit" ở 1.2s, rồi quét TRÁI → PHẢI theo xương "clean" từ x≈-204 tới x≈267, y≈40–52 (1.17–1.57s).
	// Ảnh khung khay "clean_2" (slot clean_frame) phủ x -381..374, y -34..36: đây là phần phải trùng hàng lỗ.
	const float BroomSweepFromX = -204f, BroomSweepToX = 267f, BroomSweepY = 45f;
	const float FrameMinX = -381f, FrameMaxX = 374f, FrameY = 1f;
	const float BroomTimeScale = 0.85f;

	Spine.Bone _broomBone;
	Spine.Unity.SkeletonGraphic _broomSg;
	RectTransform _broomRt;
	Vector2 _broomBasePos;
	Vector3 _broomBaseScale;
	bool _broomBaseSaved;
	Tween _broomHide;

	/// <summary>Chổi đã tới pha quét (sau sự kiện "hit").</summary>
	public bool BroomSweeping { get; private set; }
	/// <summary>Chổi đã quét xong (hết pha quét hoặc animation kết thúc).</summary>
	public bool BroomDone { get; private set; }

	Camera UiCam(Spine.Unity.SkeletonGraphic sg)
	{
		var canvas = sg != null ? sg.canvas : null;
		return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
	}

	Vector2 SpineToScreen(Spine.Unity.SkeletonGraphic sg, float x, float y)
	{
		var world = sg.transform.TransformPoint(new Vector3(x * sg.MeshScale, y * sg.MeshScale, 0f));
		return RectTransformUtility.WorldToScreenPoint(UiCam(sg), world);
	}

	/// <summary>Vị trí đầu chổi (xương "clean") trên màn hình.</summary>
	public bool TryGetBroomScreenPos(out Vector2 screen)
	{
		screen = Vector2.zero;
		var sg = _broomSg;
		if (_broomSpine == null || !_broomSpine.activeInHierarchy || sg == null || sg.Skeleton == null) return false;
		if (_broomBone == null || _broomBone.Skeleton != sg.Skeleton) _broomBone = sg.Skeleton.FindBone("clean");
		if (_broomBone == null) return false;
		screen = SpineToScreen(sg, _broomBone.WorldX, _broomBone.WorldY);
		return true;
	}

	/// <summary>
	/// Phát chổi quét qua hàng lỗ: đặt và co giãn BroomSpine để đường quét của chổi nằm đúng trên hàng lỗ
	/// (holeMin/holeMax là toạ độ màn hình của lỗ ngoài cùng trái/phải).
	/// </summary>
	public void PlayBroom(Vector2 holeMin, Vector2 holeMax, int holeCount)
	{
		if (_broomSpine == null) return;
		BroomSweeping = false;
		BroomDone = false;
		_broomRt = _broomSpine.transform as RectTransform;
		if (_broomRt != null && !_broomBaseSaved) { _broomBasePos = _broomRt.anchoredPosition; _broomBaseScale = _broomRt.localScale; _broomBaseSaved = true; }
		if (_broomRt != null) { _broomRt.anchoredPosition = _broomBasePos; _broomRt.localScale = _broomBaseScale; }
		_broomSpine.SetActive(true);
		_broomSg = _broomSpine.GetComponentInChildren<Spine.Unity.SkeletonGraphic>(true);
		var sg = _broomSg;
		float duration = 2.5f;
		if (sg != null)
		{
			sg.timeScale = BroomTimeScale;
			if (_broomRt != null && sg.MeshScale > 0f) AlignBroom(sg, holeMin, holeMax, holeCount);
			if (sg.AnimationState != null && !string.IsNullOrEmpty(sg.startingAnimation))
			{
				sg.AnimationState.ClearListenerNotifications();
				var entry = sg.AnimationState.SetAnimation(0, sg.startingAnimation, false);
				if (entry != null && entry.Animation != null) duration = entry.Animation.Duration;
				entry.Event += (te, ev) => { if (ev.Data.Name == "hit") BroomSweeping = true; };
				entry.Complete += te => { BroomDone = true; };
				sg.Update(0f);
				sg.LateUpdate();
			}
		}
		float real = duration / Mathf.Max(0.1f, BroomTimeScale);
		_broomHide?.Kill();
		_broomHide = DOVirtual.DelayedCall(real + 0.05f, () =>
		{
			BroomDone = true;
			if (_broomSpine != null) _broomSpine.SetActive(false);
		}).SetLink(gameObject);
	}

	void AlignBroom(Spine.Unity.SkeletonGraphic sg, Vector2 holeMin, Vector2 holeMax, int holeCount)
	{
		var parent = _broomRt.parent as RectTransform;
		if (parent == null) return;
		var cam = UiCam(sg);
		// 1. co giãn đều để khung khay của animation phủ đúng hàng lỗ (thêm nửa khoảng cách lỗ mỗi bên)
		float frameW = Mathf.Abs(SpineToScreen(sg, FrameMaxX, FrameY).x - SpineToScreen(sg, FrameMinX, FrameY).x);
		float span = Mathf.Abs(holeMax.x - holeMin.x);
		float spacing = holeCount > 1 ? span / (holeCount - 1) : Screen.width * 0.12f;
		float targetW = span + spacing;
		if (frameW > 1f)
		{
			float k = Mathf.Clamp(targetW / frameW, 0.5f, 1.8f);
			_broomRt.localScale = _broomBaseScale * k;
		}
		// 2. dời để tâm khung khay trùng tâm hàng lỗ
		var frameMid = SpineToScreen(sg, (FrameMinX + FrameMaxX) * 0.5f, FrameY);
		var holeMid = (holeMin + holeMax) * 0.5f;
		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, frameMid, cam, out var a) &&
			RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, holeMid, cam, out var b))
			_broomRt.anchoredPosition += b - a;
#if UNITY_EDITOR
		Debug.Log("[Broom] align holes=" + holeMin.ToString("F0") + ".." + holeMax.ToString("F0") + " n=" + holeCount +
			" frameW=" + frameW.ToString("F0") + " target=" + targetW.ToString("F0") + " scale=" + _broomRt.localScale.x.ToString("F2") +
			" pos=" + _broomRt.anchoredPosition.ToString("F0") + " meshScale=" + sg.MeshScale.ToString("F3") + " frameNow=" + SpineToScreen(sg, FrameMinX, FrameY).ToString("F0") + ".." + SpineToScreen(sg, FrameMaxX, FrameY).ToString("F0"));
#endif
	}
}
