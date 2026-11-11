using System.Collections.Generic;
using Snui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Bảng thua: hồi sinh bằng coin (giá tăng dần theo RevivePriceList) / quảng cáo, chơi lại hoặc bỏ cuộc.</summary>
public class FailDialogContent : DialogContent
{
	public List<int> RevivePriceList = new List<int> { 450, 600, 900 };

	Button _coinBtn;

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
		if (ads) transform.BindButton("ReviveBtnGroup/RvBtn", () => AdsService.ShowRewarded("revive", ok => { if (ok) DoRevive(); }));

		transform.BindButton("RetryBtn", () => { if (GameController.Instance != null) GameController.Instance.Retry(); });
		transform.BindButton("Ribbon/CloseButton", () => { if (GameController.Instance != null) GameController.Instance.QuitLevel(); });
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
		DoRevive();
	}

	void DoRevive()
	{
		var gc = GameController.Instance;
		Close(0);
		if (gc != null) gc.Revive();
	}

	// Đóng bằng nền/back = bỏ cuộc
	protected override void OnCloseClicked()
	{
		if (GameController.Instance != null) GameController.Instance.QuitLevel();
	}
}
