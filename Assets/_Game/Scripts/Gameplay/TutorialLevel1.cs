using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hướng dẫn màn 1 (chỉ chạy một lần): dùng bảng GuideCat gốc ở cuối màn hình.
/// 1) "Match screws with the same color boxes." + bàn tay chỉ vào con ốc nên bấm, lặp 3 lần.
/// 2) "Finger swipe to rotate the object." + mũi tên xoay, chờ người chơi xoay mô hình.
/// 3) "Holes hold unmatched screws temporarily!" khi có ốc đầu tiên vào lỗ chờ.
/// 4) "Let's collect all the screws!" rồi tự ẩn.
/// </summary>
public class TutorialLevel1 : MonoBehaviour
{
	public const string DoneKey = "tutorial_lv1_done";
	const int TapSteps = 3;

	GameController _gc;
	RectTransform _safe;
	GameObject _panel;
	RectTransform _hand, _ring, _rotateImg;
	Screw _target;
	int _moved;
	bool _rotated, _holeShown;
	int _step;

	public static bool ShouldRun(int level) => level == 1 && PlayerPrefs.GetInt(DoneKey, 0) == 0;

	public static void Begin(GameController gc)
	{
		var cfg = GameConfig.Instance;
		if (gc == null || gc.GameCanvas == null || cfg == null || cfg.TutorialGuide == null) return;
		var t = gc.gameObject.AddComponent<TutorialLevel1>();
		t._gc = gc;
	}

	IEnumerator Start()
	{
		var cfg = GameConfig.Instance;
		_safe = _gc.GameCanvas.transform.Find("SafeArea") as RectTransform;
		if (_safe == null) _safe = _gc.GameCanvas.transform as RectTransform;
		_panel = Instantiate(cfg.TutorialGuide, _safe, false);
		_panel.SetActive(true);
		var prt = _panel.transform as RectTransform;
		// đặt bảng sát đáy màn hình, trên hàng nút công cụ
		prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f);
		prt.anchoredPosition = new Vector2(0f, 330f);
		foreach (var g in _panel.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
		var cg = _panel.GetOrAdd<CanvasGroup>();
		cg.blocksRaycasts = false;
		cg.interactable = false;

		_hand = MakeImage("TutorialHand", cfg.TutorialHand, new Vector2(120f, 110f), new Vector2(0.06f, 0.94f));
		_ring = MakeImage("TutorialRing", cfg.TutorialTapRing, new Vector2(110f, 110f), new Vector2(0.5f, 0.5f));
		_rotateImg = MakeImage("TutorialRotate", cfg.TutorialRotate, new Vector2(221f, 183f), new Vector2(0.5f, 0.5f));
		if (_ring != null) _ring.SetAsLastSibling();
		if (_hand != null) _hand.SetAsLastSibling();
		SetVisible(_rotateImg, false);
		SetVisible(_hand, false);
		SetVisible(_ring, false);

		_gc.OnScrewMoved += OnScrewMoved;
		_gc.OnScrewToHole += OnScrewToHole;
		var rot = _gc.GameRoot != null ? _gc.GameRoot.GetComponent<RotateHelper>() : null;
		if (rot != null) rot.OnRotate.AddListener(OnRotated);

		// chờ hộp xuất hiện xong
		yield return new WaitForSeconds(0.6f);
		ShowText(1);
		PopPanel();
		_step = 1;
		while (_step == 1)
		{
			if (_target == null || _target.IsRemoved) _target = _gc.FindHintScrew();
			PointAt(_target);
			yield return null;
		}
		HideHand();

		// bước 2: xoay mô hình
		_rotated = false;
		ShowText(2);
		PopPanel();
		SetVisible(_rotateImg, true);
		if (_rotateImg != null)
		{
			_rotateImg.anchoredPosition = new Vector2(0f, _safe.rect.height * 0.12f);
			_rotateImg.DOKill();
			_rotateImg.DOAnchorPosX(80f, 0.9f).From(new Vector2(-80f, 0f)).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(_rotateImg.gameObject);
		}
		float t = 0f;
		while (!_rotated && t < 8f) { t += Time.deltaTime; yield return null; }
		SetVisible(_rotateImg, false);
		yield return new WaitForSeconds(0.4f);

		// bước 3: lỗ chờ (nếu đã có ốc vào lỗ)
		if (_holeShown)
		{
			ShowText(4);
			PopPanel();
			yield return new WaitForSeconds(3f);
		}

		// bước 4: chốt
		ShowText(5);
		PopPanel();
		yield return new WaitForSeconds(2.5f);
		Finish();
	}

	void Update()
	{
		// hiện lời nhắc lỗ chờ ngay khi có ốc đầu tiên vào lỗ trong lúc đang ở bước 2
		if (_step == 1 && _moved >= TapSteps) _step = 2;
	}

	void OnScrewMoved(Screw s)
	{
		_moved++;
		_target = null;
	}

	void OnScrewToHole() => _holeShown = true;
	void OnRotated() => _rotated = true;

	void Finish()
	{
		PlayerPrefs.SetInt(DoneKey, 1);
		PlayerPrefs.Save();
		if (_panel != null)
		{
			var cg = _panel.GetComponent<CanvasGroup>();
			if (cg != null) cg.DOFade(0f, 0.3f).SetLink(_panel).OnComplete(() => Destroy(_panel));
			else Destroy(_panel);
		}
		Cleanup();
		Destroy(this);
	}

	void OnDestroy() => Cleanup();

	void Cleanup()
	{
		if (_gc != null)
		{
			_gc.OnScrewMoved -= OnScrewMoved;
			_gc.OnScrewToHole -= OnScrewToHole;
			var rot = _gc.GameRoot != null ? _gc.GameRoot.GetComponent<RotateHelper>() : null;
			if (rot != null) rot.OnRotate.RemoveListener(OnRotated);
		}
		foreach (var r in new[] { _hand, _ring, _rotateImg }) if (r != null) Destroy(r.gameObject);
	}

	// ------------------------------------------------------------------------------------

	RectTransform MakeImage(string name, Sprite sprite, Vector2 size, Vector2 pivot)
	{
		if (sprite == null) return null;
		var go = new GameObject(name, typeof(RectTransform), typeof(Image));
		var rt = go.GetComponent<RectTransform>();
		rt.SetParent(_safe, false);
		rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
		rt.pivot = pivot;
		rt.sizeDelta = size;
		var img = go.GetComponent<Image>();
		img.sprite = sprite;
		img.raycastTarget = false;
		img.preserveAspect = true;
		return rt;
	}

	static void SetVisible(RectTransform rt, bool v)
	{
		if (rt != null && rt.gameObject.activeSelf != v) rt.gameObject.SetActive(v);
	}

	void ShowText(int i)
	{
		if (_panel == null) return;
		var tut = _panel.transform.Find("Tutorial");
		if (tut == null) return;
		for (int k = 1; k <= 5; k++)
		{
			var t = tut.Find("Text" + k);
			if (t != null) t.gameObject.SetActive(k == i);
		}
		// đổi mặt mèo cho sinh động
		var c1 = _panel.transform.Find("Icon/Cat1");
		var c2 = _panel.transform.Find("Icon/Cat2");
		if (c1 != null) c1.gameObject.SetActive(i % 2 == 1);
		if (c2 != null) c2.gameObject.SetActive(i % 2 == 0);
	}

	void PopPanel()
	{
		if (_panel == null) return;
		var t = _panel.transform;
		t.DOKill(true);
		t.localScale = Vector3.one * 0.85f;
		t.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetLink(_panel);
	}

	// Chỉ hiện tay khi vị trí ốc trên màn hình đã đứng yên (mô hình/camera xong hiệu ứng vào màn).
	const float StableTime = 0.2f, StableDist = 3f;
	Screw _lastTarget;
	Vector2 _lastOffset;
	float _stableFor;
	bool _handShown;

	void HideHand()
	{
		_handShown = false;
		_stableFor = 0f;
		SetVisible(_hand, false);
		SetVisible(_ring, false);
	}

	void PointAt(Screw s)
	{
		if (s == null || s.IsRemoved || _gc.InputLocked) { HideHand(); return; }
		var cam = _gc.MainCamera;
		var canvas = _gc.GameCanvas.GetComponentInParent<Canvas>();
		if (cam == null || canvas == null) { HideHand(); return; }
		Camera uiCam = canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
		Vector2 screen = cam.WorldToScreenPoint(s.transform.position);
		if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_safe, screen, uiCam, out var local)) { HideHand(); return; }
		// toạ độ local của SafeArea → anchoredPosition (neo giữa)
		var offset = local - _safe.rect.center;

		if (!_handShown)
		{
			// chờ đúng mục tiêu và vị trí ổn định một chút rồi mới hiện
			if (s != _lastTarget || (offset - _lastOffset).sqrMagnitude > StableDist * StableDist) _stableFor = 0f;
			else _stableFor += Time.deltaTime;
			_lastTarget = s;
			_lastOffset = offset;
			if (_stableFor < StableTime) return;
			_handShown = true;
			SetVisible(_hand, true);
			SetVisible(_ring, true);
			if (_hand != null)
			{
				var hi = _hand.GetComponent<Image>();
				if (hi != null) { hi.DOKill(); hi.color = new Color(1f, 1f, 1f, 0f); hi.DOFade(1f, 0.2f).SetLink(_hand.gameObject); }
			}
		}
		else if (s != _lastTarget) { _lastTarget = s; HideHand(); return; }
		if (_ring != null) _ring.anchoredPosition = offset;
		if (_hand != null)
		{
			// bàn tay nhún chạm vào ốc theo nhịp
			float k = Mathf.PingPong(Time.time * 2.2f, 1f);
			k = k * k;
			_hand.anchoredPosition = offset + new Vector2(10f, -10f) + new Vector2(18f, -18f) * k;
			_hand.localScale = Vector3.one * Mathf.Lerp(1f, 0.9f, 1f - k);
		}
		if (_ring != null)
		{
			float r = Mathf.Repeat(Time.time * 1.1f, 1f);
			_ring.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.25f, r);
			var img = _ring.GetComponent<Image>();
			if (img != null) img.color = new Color(1f, 1f, 1f, 1f - r);
		}
	}
}
