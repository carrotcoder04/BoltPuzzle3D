using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Nút "Daily Gift" ở cột sự kiện Home (HomeEvents): lịch trong vòng vàng nền xanh, pill xanh bên dưới
/// đếm ngược tới lượt quà kế tiếp ("CLAIM!" khi nhận được); chấm đỏ + nảy khi còn quà hôm nay.
/// </summary>
public class HomeDailyButton : MonoBehaviour
{
	public static HomeDailyButton Instance { get; private set; }

	GameObject _dot;
	TextMeshProUGUI _label;
	float _tick;
	bool _lastCan;

	/// <summary>Tạo nút dưới parent (gọi từ HomeController).</summary>
	public static HomeDailyButton Create(RectTransform parent)
	{
		var cfg = GameConfig.Instance;
		var go = new GameObject("DailyButton", typeof(RectTransform), typeof(Image), typeof(Button));
		var rt = (RectTransform)go.transform;
		rt.SetParent(parent, false);
		rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
		rt.pivot = new Vector2(0.5f, 0.5f);
		rt.anchoredPosition = new Vector2(70f, -330f);
		rt.sizeDelta = new Vector2(110f, 110f);
		var img = go.GetComponent<Image>();
		img.sprite = cfg != null && cfg.DailyBtnFrame != null ? cfg.DailyBtnFrame : cfg != null ? cfg.DailyIconSprite : null;
		img.preserveAspect = true;
		if (img.sprite == null) img.color = new Color(0.3f, 0.75f, 0.3f);

		var cal = new GameObject("Calendar", typeof(RectTransform), typeof(Image));
		var crt = (RectTransform)cal.transform;
		crt.SetParent(rt, false);
		crt.sizeDelta = new Vector2(66f, 66f);
		crt.anchoredPosition = new Vector2(0f, 4f);
		var ci = cal.GetComponent<Image>();
		ci.sprite = cfg != null && cfg.DailyCalendar != null ? cfg.DailyCalendar : cfg != null ? cfg.DailyIconSprite : null;
		ci.preserveAspect = true;
		ci.raycastTarget = false;

		// pill xanh đếm ngược
		var pill = new GameObject("Pill", typeof(RectTransform), typeof(Image));
		var prt = (RectTransform)pill.transform;
		prt.SetParent(rt, false);
		prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f);
		prt.sizeDelta = new Vector2(132f, 40f);
		prt.anchoredPosition = new Vector2(0f, 4f);
		var pi = pill.GetComponent<Image>();
		pi.sprite = cfg != null ? cfg.DailyPill : null;
		if (pi.sprite != null && pi.sprite.border != Vector4.zero) pi.type = Image.Type.Sliced;
		else if (pi.sprite == null) pi.color = new Color(0.3f, 0.7f, 0.25f);
		pi.raycastTarget = false;

		// chữ lấy font từ một chữ có sẵn trong scene
		var src = Object.FindAnyObjectByType<TextMeshProUGUI>();
		var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
		label.transform.SetParent(prt, false);
		if (src != null) { label.font = src.font; label.fontSharedMaterial = src.fontSharedMaterial; }
		label.enableAutoSizing = true;
		label.fontSizeMin = 14f;
		label.fontSizeMax = 24f;
		label.enableWordWrapping = false;
		label.alignment = TextAlignmentOptions.Center;
		label.color = Color.white;
		label.raycastTarget = false;
		var lrt = label.rectTransform;
		lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
		lrt.offsetMin = new Vector2(8f, 2f); lrt.offsetMax = new Vector2(-8f, -2f);
		label.outlineWidth = 0.2f;
		label.outlineColor = new Color32(20, 70, 20, 255);

		var dot = new GameObject("Dot", typeof(RectTransform), typeof(Image));
		var drt = (RectTransform)dot.transform;
		drt.SetParent(rt, false);
		drt.anchorMin = drt.anchorMax = new Vector2(1f, 1f);
		drt.sizeDelta = new Vector2(24f, 24f);
		drt.anchoredPosition = new Vector2(-14f, -14f);
		var di = dot.GetComponent<Image>();
		di.color = new Color(0.95f, 0.2f, 0.2f);
		di.sprite = null;
		di.raycastTarget = false;

		var b = go.AddComponent<HomeDailyButton>();
		b._dot = dot;
		b._label = label;
		go.GetComponent<Button>().onClick.AddListener(() =>
		{
			go.transform.DOKill(true);
			go.transform.DOPunchScale(Vector3.one * 0.15f, 0.25f, 6, 0.5f);
			DailyRewardDialogContent.Open();
		});
		b.Refresh();
		return b;
	}

	void Awake() => Instance = this;
	void OnDestroy() { if (Instance == this) Instance = null; transform.DOKill(); }
	void OnEnable() => Refresh();

	void Update()
	{
		_tick -= Time.unscaledDeltaTime;
		if (_tick > 0f) return;
		_tick = 1f;
		if (DailyReward.CanClaim != _lastCan) Refresh();
		else UpdateLabel();
	}

	void UpdateLabel()
	{
		if (_label == null) return;
		_label.text = DailyReward.CanClaim ? "CLAIM!" : DailyReward.FormatShort(DailyReward.TimeToNext);
	}

	public void Refresh()
	{
		bool can = DailyReward.CanClaim;
		_lastCan = can;
		if (_dot != null) _dot.SetActive(can);
		UpdateLabel();
		transform.DOKill(true);
		transform.localScale = Vector3.one;
		if (can) transform.DOScale(1.08f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
	}
}
