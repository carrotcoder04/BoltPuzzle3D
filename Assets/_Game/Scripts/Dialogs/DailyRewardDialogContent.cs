using System;
using DG.Tweening;
using Snui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bảng "Daily Gift" (DailyRewardDialog.prefab, dựng trên khung BuyToolDialog):
/// - Ruy băng tiêu đề hồng, lịch "DAY N" (tổng ngày điểm danh) + thanh tiến độ với 5 hộp quà mốc 10/12/16/22/30.
/// - Lưới 3x2 ô ngày 1-6 + ô ngày 7 rộng (2 phần quà). Ô đã nhận: đầu xanh + dấu tích; ô hôm nay: vàng, nảy;
///   ô sắp tới: hồng. Bấm ô hôm nay để nhận.
/// </summary>
public class DailyRewardDialogContent : DialogContent
{
	const float W = 680f, H = 1060f;
	const float CellW = 186f, CellH = 200f, Gap = 12f;

	TMP_Text _fontSrc;
	RectTransform _root;
	RectTransform[] _cells;
	Image[] _gifts;
	Image _trackFill;
	TMP_Text _dayText, _footer;
	bool _claimed;
	float _tick;

	public static void Open()
	{
		var cfg = GameConfig.Instance;
		if (cfg == null || cfg.DailyRewardDialog == null) return;
		DialogManager.Show(cfg.DailyRewardDialog);
	}

	protected override void Awake()
	{
		base.Awake();
		var cfg = GameConfig.Instance;
		_root = (RectTransform)transform;
		_root.sizeDelta = new Vector2(W, H);
		_fontSrc = transform.FindComp<TMP_Text>("BgTitle/Title");
		UIUtil.SetText(_fontSrc, "DAILY GIFT");
		var title = transform.FindComp<Image>("BgTitle");
		if (title != null && cfg != null && cfg.DailyTitleSprite != null) title.sprite = cfg.DailyTitleSprite;
		foreach (var n in new[] { "BgContent", "Ip", "BtnAd", "BtnClaim", "BtnCoin" }) transform.SetActive(n, false);
		transform.BindButton("CloseBtn", () => Close(-1));
		var close = transform.Find("CloseBtn");
		if (close != null) close.SetAsLastSibling();

		BuildHeader(cfg);
		BuildGrid(cfg);
		_footer = Label(_root, "Footer", "", new Vector2(0f, -H * 0.5f + 62f), new Vector2(W - 80f, 46f), 32f);
		Refresh();
	}

	// ---------- dựng giao diện ----------

	Image Img(Transform parent, string name, Sprite sp, Vector2 pos, Vector2 size, bool sliced = false)
	{
		var go = new GameObject(name, typeof(RectTransform), typeof(Image));
		var rt = (RectTransform)go.transform;
		rt.SetParent(parent, false);
		rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
		rt.anchoredPosition = pos;
		rt.sizeDelta = size;
		var im = go.GetComponent<Image>();
		im.sprite = sp;
		im.raycastTarget = false;
		if (sp == null) im.color = new Color(1f, 1f, 1f, 0.25f);
		else if (sliced && sp.border != Vector4.zero) im.type = Image.Type.Sliced;
		else im.preserveAspect = true;
		return im;
	}

	void BuildHeader(GameConfig cfg)
	{
		float y = H * 0.5f - 210f;
		// Lịch "DAY N"
		var cal = Img(_root, "Calendar", cfg != null ? cfg.DailyCalendar : null, new Vector2(-W * 0.5f + 100f, y), new Vector2(130f, 130f));
		var dayLbl = Label(cal.rectTransform, "Day", "DAY", new Vector2(0f, -4f), new Vector2(100f, 28f), 24f);
		dayLbl.color = new Color32(214, 64, 64, 255);
		_dayText = Label(cal.rectTransform, "Count", "0", new Vector2(0f, -34f), new Vector2(100f, 40f), 40f);
		_dayText.color = new Color32(214, 64, 64, 255);

		// Thanh tiến độ + 5 hộp quà mốc
		float x0 = -W * 0.5f + 185f, x1 = W * 0.5f - 50f;
		float ty = y - 22f;
		var track = Img(_root, "Track", cfg != null ? cfg.DailyTrack : null, new Vector2((x0 + x1) * 0.5f, ty), new Vector2(x1 - x0, 28f), true);
		_trackFill = Img(track.rectTransform, "Fill", cfg != null ? cfg.DailyTrackFill : null, Vector2.zero, new Vector2(x1 - x0, 28f), true);
		var frt = _trackFill.rectTransform;
		frt.anchorMin = new Vector2(0f, 0f); frt.anchorMax = new Vector2(0f, 1f); frt.pivot = new Vector2(0f, 0.5f);
		frt.anchoredPosition = Vector2.zero;

		int m = DailyReward.Milestones.Length;
		_gifts = new Image[m];
		for (int i = 0; i < m; i++)
		{
			float gx = x0 + (x1 - x0) * (i + 1) / m - 30f;
			var gs = cfg != null && cfg.DailyGifts != null && cfg.DailyGifts.Length > 0 ? cfg.DailyGifts[i % cfg.DailyGifts.Length] : null;
			var g = Img(_root, "Gift" + (i + 1), gs, new Vector2(gx, ty + 46f), new Vector2(76f, 76f));
			var lbl = Label(_root, "Mark" + (i + 1), DailyReward.Milestones[i].ToString(), new Vector2(gx, ty - 34f), new Vector2(70f, 34f), 28f);
			lbl.transform.SetAsLastSibling();
			_gifts[i] = g;
		}
	}

	void BuildGrid(GameConfig cfg)
	{
		float gridW = CellW * 3f + Gap * 2f, gridH = CellH * 3f + Gap * 2f;
		float cy = H * 0.5f - 300f - gridH * 0.5f - 10f;
		Img(_root, "Panel", cfg != null ? cfg.DailyPanel : null, new Vector2(0f, cy), new Vector2(gridW + 36f, gridH + 36f), true);

		int n = DailyReward.Rewards.Length;
		_cells = new RectTransform[n];
		for (int i = 0; i < n; i++)
		{
			bool wide = i == n - 1;
			int row = i / 3, col = i % 3;
			float x = wide ? 0f : (col - 1) * (CellW + Gap);
			float y = cy + (1 - row) * (CellH + Gap);
			var bg = Img(_root, "Day" + (i + 1), cfg != null ? cfg.DailyCellPink : null, new Vector2(x, y), new Vector2(wide ? gridW : CellW, CellH), true);
			bg.raycastTarget = true;
			var rt = bg.rectTransform;
			Label(rt, "Title", "DAY " + (i + 1), new Vector2(0f, CellH * 0.5f - 30f), new Vector2(CellW - 20f, 40f), 32f);

			var r = DailyReward.Rewards[i];
			bool hasCoin = r.Coins > 0, hasBooster = r.Booster >= 0 && r.BoosterCount > 0;
			if (hasCoin && hasBooster)
			{
				RewardItem(rt, DailyReward.CoinIcon, DailyReward.CoinLabel(r.Coins), -gridW * 0.2f);
				RewardItem(rt, DailyReward.BoosterIcon(r.Booster), DailyReward.BoosterLabel(r), gridW * 0.2f);
			}
			else if (hasBooster) RewardItem(rt, DailyReward.BoosterIcon(r.Booster), DailyReward.BoosterLabel(r), 0f);
			else RewardItem(rt, DailyReward.CoinIcon, DailyReward.CoinLabel(r.Coins), 0f);

			var check = Img(rt, "Check", cfg != null ? cfg.DailyCheck : null, new Vector2(0f, -22f), new Vector2(84f, 84f));
			check.gameObject.SetActive(false);

			int day = i;
			var btn = bg.gameObject.AddComponent<Button>();
			btn.transition = Selectable.Transition.None;
			btn.onClick.AddListener(() => OnCellClicked(day));
			_cells[i] = rt;
		}
	}

	void RewardItem(RectTransform cell, Sprite icon, string amount, float x)
	{
		var im = Img(cell, "Icon", icon, new Vector2(x, -12f), new Vector2(96f, 82f));
		im.name = "Icon";
		var t = Label(cell, "Amount", amount, new Vector2(x, -CellH * 0.5f + 28f), new Vector2(CellW - 16f, 40f), 34f);
		t.color = new Color32(255, 255, 255, 255);
	}

	TMP_Text Label(RectTransform parent, string name, string text, Vector2 pos, Vector2 size, float fontSize)
	{
		TMP_Text t;
		if (_fontSrc != null)
		{
			t = Instantiate(_fontSrc, parent, false);
			foreach (Transform c in t.transform) Destroy(c.gameObject);
			foreach (var mb in t.GetComponents<MonoBehaviour>()) if (!(mb is TMP_Text)) Destroy(mb);
		}
		else t = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
		t.name = name;
		t.transform.SetParent(parent, false);
		var rt = t.rectTransform;
		rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
		rt.anchoredPosition = pos;
		rt.sizeDelta = size;
		rt.localScale = Vector3.one;
		rt.localRotation = Quaternion.identity;
		t.text = text;
		t.enableAutoSizing = true;
		t.fontSizeMin = 14f;
		t.fontSizeMax = fontSize;
		t.enableWordWrapping = false;
		t.alignment = TextAlignmentOptions.Center;
		t.raycastTarget = false;
		return t;
	}

	// ---------- trạng thái ----------

	void Refresh()
	{
		var cfg = GameConfig.Instance;
		int today = DailyReward.CurrentDay;
		int claimedCount = DailyReward.ClaimedCount;
		bool canClaim = DailyReward.CanClaim;
		for (int i = 0; i < _cells.Length; i++)
		{
			bool claimed = i < claimedCount;
			bool isToday = canClaim && i == today;
			var bg = _cells[i].GetComponent<Image>();
			if (cfg != null) bg.sprite = claimed ? cfg.DailyCellBlue : isToday ? cfg.DailyCellGold : cfg.DailyCellPink;
			_cells[i].SetActive("Check", claimed);
			foreach (Transform c in _cells[i])
				if (c.name == "Icon") { var im = c.GetComponent<Image>(); if (im != null) im.color = claimed ? new Color(1f, 1f, 1f, 0.55f) : Color.white; }
			_cells[i].DOKill(true);
			_cells[i].localScale = Vector3.one;
			if (isToday) _cells[i].DOScale(1.05f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(gameObject);
		}

		int prog = DailyReward.CycleProgress;
		UIUtil.SetText(_dayText, DailyReward.Total.ToString());
		var ms = DailyReward.Milestones;
		// thanh tiến độ: các mốc chia đều, nội suy giữa 2 mốc
		float frac = 0f;
		for (int i = 0; i < ms.Length; i++)
		{
			int lo = i == 0 ? 0 : ms[i - 1];
			if (prog >= ms[i]) { frac = (i + 1f) / ms.Length; continue; }
			frac = (i + Mathf.Clamp01((prog - lo) / (float)(ms[i] - lo))) / ms.Length;
			break;
		}
		var track = (RectTransform)_trackFill.rectTransform.parent;
		_trackFill.rectTransform.sizeDelta = new Vector2(track.sizeDelta.x * frac, 0f);
		_trackFill.enabled = frac > 0.001f;
		for (int i = 0; i < _gifts.Length; i++)
		{
			bool got = prog >= ms[i];
			_gifts[i].color = got ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
		}
		UpdateFooter();
	}

	void UpdateFooter()
	{
		if (_footer == null) return;
		_footer.text = DailyReward.CanClaim ? "Tap today's gift to claim!" : "Next gift in " + DailyReward.FormatLong(DailyReward.TimeToNext);
	}

	void Update()
	{
		_tick -= Time.unscaledDeltaTime;
		if (_tick > 0f) return;
		_tick = 1f;
		if (!_claimed && DailyReward.CanClaim && _footer != null && !_footer.text.StartsWith("Tap")) Refresh();
		else UpdateFooter();
	}

	void OnCellClicked(int day)
	{
		if (_claimed || !DailyReward.CanClaim || day != DailyReward.CurrentDay)
		{
			if (day < _cells.Length) { _cells[day].DOKill(true); _cells[day].DOShakeAnchorPos(0.3f, 8f, 20).SetUpdate(true).SetLink(gameObject); Refresh(); }
			return;
		}
		_claimed = true;
		int ms = DailyReward.Claim();
		if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.BigCoinGet);
		Refresh();
		_cells[day].DOPunchScale(Vector3.one * 0.2f, 0.4f, 6, 0.5f).SetUpdate(true).SetLink(gameObject);
		if (ms >= 0 && ms < _gifts.Length) _gifts[ms].rectTransform.DOPunchScale(Vector3.one * 0.4f, 0.5f, 6, 0.5f).SetUpdate(true).SetLink(gameObject);
		if (HomeDailyButton.Instance != null) HomeDailyButton.Instance.Refresh();
		DOVirtual.DelayedCall(1.2f, () => Close(1)).SetUpdate(true).SetLink(gameObject);
	}

	protected override void OnCloseClicked() => Close(-1);
}
