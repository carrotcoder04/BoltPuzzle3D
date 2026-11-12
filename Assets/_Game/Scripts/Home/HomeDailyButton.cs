using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Nút "Daily" ở cột sự kiện Home (HomeEvents): mở bảng điểm danh, chấm đỏ + nảy khi còn quà hôm nay.</summary>
public class HomeDailyButton : MonoBehaviour
{
	public static HomeDailyButton Instance { get; private set; }

	GameObject _dot;

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
		img.sprite = cfg != null ? cfg.DailyIconSprite : null;
		img.preserveAspect = true;
		if (img.sprite == null) img.color = new Color(1f, 0.8f, 0.3f);

		// nhãn "DAILY" lấy font từ một chữ có sẵn trong scene
		var src = Object.FindAnyObjectByType<TextMeshProUGUI>();
		var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
		label.transform.SetParent(rt, false);
		if (src != null) { label.font = src.font; label.fontSharedMaterial = src.fontSharedMaterial; }
		label.text = "DAILY";
		label.fontSize = 26f;
		label.alignment = TextAlignmentOptions.Center;
		label.color = Color.white;
		label.raycastTarget = false;
		var lrt = label.rectTransform;
		lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0f);
		lrt.sizeDelta = new Vector2(140f, 34f);
		lrt.anchoredPosition = new Vector2(0f, -6f);
		label.outlineWidth = 0.25f;
		label.outlineColor = new Color32(40, 30, 80, 255);

		var dot = new GameObject("Dot", typeof(RectTransform), typeof(Image));
		var drt = (RectTransform)dot.transform;
		drt.SetParent(rt, false);
		drt.anchorMin = drt.anchorMax = new Vector2(1f, 1f);
		drt.sizeDelta = new Vector2(26f, 26f);
		drt.anchoredPosition = new Vector2(-12f, -12f);
		var di = dot.GetComponent<Image>();
		di.color = new Color(0.95f, 0.2f, 0.2f);
		di.raycastTarget = false;

		var b = go.AddComponent<HomeDailyButton>();
		b._dot = dot;
		go.GetComponent<Button>().onClick.AddListener(() =>
		{
			go.transform.DOKill(true);
			go.transform.DOPunchScale(Vector3.one * 0.15f, 0.25f, 6, 0.5f);
			DailyRewardDialogContent.Open();
		});
		return b;
	}

	void Awake() => Instance = this;
	void OnDestroy() { if (Instance == this) Instance = null; transform.DOKill(); }
	void OnEnable() => Refresh();

	public void Refresh()
	{
		bool can = DailyReward.CanClaim;
		if (_dot != null) _dot.SetActive(can);
		transform.DOKill(true);
		transform.localScale = Vector3.one;
		if (can) transform.DOScale(1.08f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
	}
}
