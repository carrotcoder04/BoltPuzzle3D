using DG.Tweening;
using Snui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bảng điểm danh 7 ngày (DailyRewardDialog.prefab, dựng trên khung BuyToolDialog):
/// lưới 7 ô quà (4 + 3), ô hôm nay sáng vàng, ô đã nhận phủ xanh; nút CLAIM nhận quà hôm nay.
/// </summary>
public class DailyRewardDialogContent : DialogContent
{
	static readonly Color CellNormal = new Color(1f, 1f, 1f, 1f);
	static readonly Color CellToday = new Color(1f, 0.86f, 0.35f, 1f);
	static readonly Color CellClaimed = new Color(0.62f, 0.9f, 0.55f, 1f);

	TMP_Text _fontSrc;
	Sprite _cellSprite;
	RectTransform[] _cells;
	bool _claimed;

	public static void Open()
	{
		var cfg = GameConfig.Instance;
		if (cfg == null || cfg.DailyRewardDialog == null) return;
		DialogManager.Show(cfg.DailyRewardDialog);
	}

	protected override void Awake()
	{
		base.Awake();
		_fontSrc = transform.FindComp<TMP_Text>("BgTitle/Title");
		UIUtil.SetText(_fontSrc, "DAILY REWARD");
		var content = transform.Find("BgContent/Content") as RectTransform;
		var contentImg = content != null ? content.GetComponent<Image>() : null;
		_cellSprite = contentImg != null ? contentImg.sprite : null;
		if (content != null) foreach (Transform c in content) c.gameObject.SetActive(false);
		transform.SetActive("BtnAd", false);
		transform.SetActive("BtnClaim", false);
		transform.SetActive("BtnCoin/Content/Icon", false);
		var coin = transform.Find("BtnCoin") as RectTransform;
		if (coin != null) coin.anchoredPosition = new Vector2(0f, coin.anchoredPosition.y);
		var txt = transform.FindComp<TMP_Text>("BtnCoin/Content/Text");
		if (txt != null)
		{
			txt.rectTransform.anchoredPosition = new Vector2(0f, txt.rectTransform.anchoredPosition.y);
			txt.rectTransform.sizeDelta = new Vector2(250f, txt.rectTransform.sizeDelta.y);
			txt.enableAutoSizing = true;
			txt.fontSizeMin = 20f;
			txt.enableWordWrapping = false;
		}
		transform.BindButton("BtnCoin", OnClaim);
		transform.BindButton("CloseBtn", () => Close(-1));
		if (content != null) BuildGrid(content);
		Refresh();
	}

	void BuildGrid(RectTransform content)
	{
		int n = DailyReward.Rewards.Length;
		_cells = new RectTransform[n];
		const float w = 118f, h = 140f, gap = 10f;
		for (int i = 0; i < n; i++)
		{
			int row = i < 4 ? 0 : 1;
			int col = row == 0 ? i : i - 4;
			int inRow = row == 0 ? 4 : n - 4;
			float x = (col - (inRow - 1) * 0.5f) * (w + gap);
			float y = row == 0 ? (h + gap) * 0.5f : -(h + gap) * 0.5f;
			var r = DailyReward.Rewards[i];

			var cell = new GameObject("Day" + (i + 1), typeof(RectTransform), typeof(Image));
			var rt = (RectTransform)cell.transform;
			rt.SetParent(content, false);
			rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
			rt.sizeDelta = new Vector2(i == n - 1 ? w * 1.6f : w, h);
			rt.anchoredPosition = new Vector2(i == n - 1 ? x : x, y);
			var bg = cell.GetComponent<Image>();
			bg.sprite = _cellSprite;
			bg.type = _cellSprite != null && _cellSprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
			bg.raycastTarget = false;

			Label(rt, "Day", "Day " + (i + 1), new Vector2(0f, h * 0.5f - 18f), new Vector2(w * 1.5f, 30f), 24f);
			var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
			var irt = (RectTransform)icon.transform;
			irt.SetParent(rt, false);
			irt.sizeDelta = new Vector2(70f, 70f);
			irt.anchoredPosition = new Vector2(0f, 2f);
			var im = icon.GetComponent<Image>();
			im.sprite = DailyReward.Icon(r);
			im.preserveAspect = true;
			im.raycastTarget = false;
			Label(rt, "Amount", DailyReward.Label(r), new Vector2(0f, -h * 0.5f + 20f), new Vector2(rt.sizeDelta.x - 8f, 32f), 24f);
			var done = Label(rt, "Done", "CLAIMED", Vector2.zero, new Vector2(rt.sizeDelta.x, 40f), 22f);
			done.color = new Color(0.1f, 0.45f, 0.1f, 1f);
			done.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
			_cells[i] = rt;
		}
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
		rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
		rt.pivot = new Vector2(0.5f, 0.5f);
		rt.anchoredPosition = pos;
		rt.sizeDelta = size;
		rt.localScale = Vector3.one;
		t.text = text;
		t.enableAutoSizing = true;
		t.fontSizeMin = 14f;
		t.fontSizeMax = fontSize;
		t.enableWordWrapping = false;
		t.alignment = TextAlignmentOptions.Center;
		t.raycastTarget = false;
		return t;
	}

	void Refresh()
	{
		int today = DailyReward.CurrentDay;
		int claimedCount = DailyReward.ClaimedCount;
		bool canClaim = DailyReward.CanClaim;
		if (_cells != null)
			for (int i = 0; i < _cells.Length; i++)
			{
				bool claimed = i < claimedCount;
				bool isToday = canClaim && i == today;
				var bg = _cells[i].GetComponent<Image>();
				if (bg != null) bg.color = claimed ? CellClaimed : isToday ? CellToday : CellNormal;
				_cells[i].SetActive("Done", claimed);
				_cells[i].DOKill(true);
				_cells[i].localScale = Vector3.one;
				if (isToday) _cells[i].DOScale(1.06f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(gameObject);
			}
		UIUtil.SetText(transform.FindComp<TMP_Text>("BtnCoin/Content/Text"), canClaim ? "CLAIM" : "TOMORROW");
		var btn = transform.FindComp<Button>("BtnCoin");
		if (btn != null) btn.interactable = canClaim;
	}

	void OnClaim()
	{
		if (_claimed || !DailyReward.CanClaim) return;
		_claimed = true;
		int day = DailyReward.CurrentDay;
		DailyReward.Claim();
		if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.BigCoinGet);
		if (_cells != null && day < _cells.Length) _cells[day].DOPunchScale(Vector3.one * 0.25f, 0.4f, 6, 0.5f).SetUpdate(true).SetLink(gameObject);
		Refresh();
		if (HomeDailyButton.Instance != null) HomeDailyButton.Instance.Refresh();
		DOVirtual.DelayedCall(0.9f, () => Close(1)).SetUpdate(true).SetLink(gameObject);
	}

	protected override void OnCloseClicked() => Close(-1);
}
