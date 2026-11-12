using System.Collections.Generic;
using DG.Tweening;
using Snui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Bảng thua: hồi sinh bằng coin (giá tăng dần theo RevivePriceList) / quảng cáo, chơi lại hoặc bỏ cuộc.</summary>
public class FailDialogContent : DialogContent
{
	public List<int> RevivePriceList = new List<int> { 450, 600, 900 };

	Button _coinBtn;
	bool _warned;
	int _closeFrame = -1;
	const string Revive = "TipsBg/Bg/Revive";
	const string LoseItem = "TipsBg/Bg/LoseItem";

	protected override void Awake()
	{
		base.Awake();
		var gc = GameController.Instance;
		int price = Price(gc);
		bool ads = AdsService.IsRewardedReady;

		transform.SetActive("ReviveBtnGroup", ads);
		transform.SetActive("ReviveBtnCoinOnly", !ads);
		transform.SetActive("FreeBtn", false);

		_coinBtn = transform.BindButton(ads ? "ReviveBtnGroup/CoinBtn" : "ReviveBtnCoinOnly", () => ReviveWithCoins(price));
		var countText = ads ? transform.FindText("CoinBtn") : transform.FindComp<TMP_Text>("ReviveBtnCoinOnly/Content/Count");
		UIUtil.SetText(countText, price.ToString());
		if (ads) transform.BindButton("ReviveBtnGroup/RvBtn", () => AdsService.ShowRewarded("revive", ok => { if (ok) DoRevive("ad"); }));

		transform.BindButton("RetryBtn", () => { if (GameController.Instance != null) GameController.Instance.Retry(); });
		transform.BindButton("Ribbon/CloseButton", OnCloseClicked);
		SetupContent(gc);
	}

	/// <summary>
	/// Khung giữa bảng: mặc định giới thiệu phần hồi sinh (thêm lỗ / dọn lỗ) kèm ảnh minh hoạ;
	/// bấm X lần đầu chuyển sang cảnh báo mất tim, bấm lần nữa mới bỏ cuộc.
	/// </summary>
	void SetupContent(GameController gc)
	{
		bool addHole = gc == null || gc.ReviveAddsHole;
		transform.SetActive(Revive, true);
		transform.SetActive(Revive + "/AddBox", addHole);
		transform.SetActive(Revive + "/ClearHole", !addHole);
		UIUtil.SetText(transform.FindComp<TMP_Text>(Revive + "/AddBox/Describe"), "Add 1 hole and continue!");
		transform.SetActive(LoseItem, false);

		var img = transform.Find(Revive + (addHole ? "/AddBox/Image" : "/ClearHole/Image")) as RectTransform;
		if (img != null)
		{
			var s0 = img.localScale;
			img.localScale = s0 * 0.6f;
			img.DOScale(s0, 0.45f).SetDelay(0.15f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
			img.DOAnchorPosY(img.anchoredPosition.y + 10f, 0.9f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(0.6f).SetUpdate(true).SetLink(gameObject);
		}
		// nút hồi sinh nhịp nhẹ để hút mắt
		var btn = transform.Find(AdsService.IsRewardedReady ? "ReviveBtnGroup/CoinBtn" : "ReviveBtnCoinOnly");
		if (btn != null)
			btn.DOScale(btn.localScale * 1.06f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(0.6f).SetUpdate(true).SetLink(gameObject);
	}

	void ShowLoseWarning()
	{
		transform.SetActive(Revive, false);
		transform.SetActive(LoseItem, true);
		var group = transform.Find(LoseItem + "/ChatBg/Group3");
		for (int i = 0; i < 4; i++) transform.SetActive(LoseItem + "/ChatBg/Group" + i, i == 3);
		if (group == null) return;
		var hc = group.Find("LoseHeartContainer");
		if (hc != null)
		{
			foreach (Transform c in hc) c.gameObject.SetActive(c.name == "Heart");
			var heart = hc.Find("Heart") as RectTransform;
			if (heart != null)
			{
				heart.anchoredPosition = new Vector2(0f, heart.anchoredPosition.y);
				heart.DOKill();
				heart.localScale = Vector3.one * 0.5f;
				heart.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
				heart.DOPunchRotation(new Vector3(0f, 0f, 12f), 0.6f, 8, 0.5f).SetDelay(0.4f).SetUpdate(true).SetLink(gameObject);
			}
		}
		UIUtil.SetText(group.FindComp<TMP_Text>("Describe"), "You will lose a life!");
	}

	int Price(GameController gc)
	{
		if (RevivePriceList == null || RevivePriceList.Count == 0) return gc != null ? gc.NextRevivePrice : 0;
		int i = gc != null ? gc.ReviveCount : 0;
		return RevivePriceList[Mathf.Min(i, RevivePriceList.Count - 1)];
	}

	void ReviveWithCoins(int price)
	{
		var ud = Managers.UserData;
		if (ud == null || !ud.TrySpendCoins(price, "revive")) { UIUtil.Deny(_coinBtn); return; }
		DoRevive("coin");
	}

	void DoRevive(string method)
	{
		var gc = GameController.Instance;
		Close(0);
		if (gc != null) gc.Revive(method);
	}

	// Đóng (X / nền / back): lần đầu cảnh báo mất tim, lần sau mới bỏ cuộc
	protected override void OnCloseClicked()
	{
		// nút X có thể vừa được DialogContent gắn sẵn vừa được bind ở trên: bỏ lần gọi trùng trong cùng khung hình
		if (_closeFrame == Time.frameCount) return;
		_closeFrame = Time.frameCount;
		bool unlimited = Managers.Inventory != null && Managers.Inventory.IsUnlimitedHeart;
		if (!_warned && !unlimited) { _warned = true; ShowLoseWarning(); return; }
		if (GameController.Instance != null) GameController.Instance.QuitLevel();
	}
}
