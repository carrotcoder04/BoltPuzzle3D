using System;
using DG.Tweening;
using Snui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Popup mua vật phẩm bằng coin (BuyToolDialog.prefab): dùng cho công cụ Khoan/Búa/Chổi và thêm khay.
/// Mở bằng BuyItemDialogContent.Open(...). Bấm nút coin → trừ coin → gọi onBought.
/// </summary>
public class BuyItemDialogContent : DialogContent
{
	public enum Kind { Drill, Hammer, Broom, Box }

	[Tooltip("Hình minh hoạ cho popup thêm khay")]
	public Sprite BoxSprite;

	Kind _kind;
	int _price;
	Action _onBought;
	bool _done;

	public static void Open(Kind kind, int price, Action onBought)
	{
		var cfg = GameConfig.Instance;
		if (cfg == null || cfg.BuyItemDialog == null)
		{
			// không có prefab: mua thẳng như cũ
			if (Managers.UserData != null && Managers.UserData.TrySpendCoins(price, "buy_" + kind)) onBought?.Invoke();
			return;
		}
		var go = DialogManager.Show(cfg.BuyItemDialog);
		var c = go != null ? go.GetComponentInChildren<BuyItemDialogContent>(true) : null;
		if (c != null) c.Setup(kind, price, onBought);
	}

	public static string KindName(Kind k) => k == Kind.Box ? "Add Box" : k.ToString();

	void Setup(Kind kind, int price, Action onBought)
	{
		_kind = kind;
		_price = price;
		_onBought = onBought;

		UIUtil.SetText(transform.FindComp<TMP_Text>("BgTitle/Title"), KindName(kind).ToUpperInvariant());
		string node = kind == Kind.Box ? "Hammer" : kind.ToString();
		foreach (var n in new[] { "Drill", "Hammer", "Broom" })
			transform.SetActive("BgContent/Content/" + n, n == node);
		if (kind == Kind.Box)
		{
			var img = transform.FindComp<Image>("BgContent/Content/Hammer/Image");
			if (img != null && BoxSprite != null)
			{
				img.sprite = BoxSprite;
				img.preserveAspect = true;
				((RectTransform)img.transform).sizeDelta = new Vector2(256f, 256f);
			}
			UIUtil.SetText(transform.FindComp<TMP_Text>("BgContent/Content/Hammer/Desc"), "Add 1 extra box");
		}

		// chỉ dùng nút coin, căn giữa (chưa có quảng cáo)
		transform.SetActive("BtnAd", false);
		transform.SetActive("BtnClaim", false);
		var coin = transform.Find("BtnCoin") as RectTransform;
		if (coin != null) coin.anchoredPosition = new Vector2(0f, coin.anchoredPosition.y);
		UIUtil.SetText(transform.FindComp<TMP_Text>("BtnCoin/Content/Text"), price.ToString());
		transform.BindButton("BtnCoin", OnBuy);
		transform.BindButton("CloseBtn", () => Close(-1));
		if (coin != null) coin.DOScale(coin.localScale * 1.05f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(0.4f).SetUpdate(true).SetLink(gameObject);
	}

	protected override void OnCloseClicked() => Close(-1);

	void OnBuy()
	{
		if (_done) return;
		var ud = Managers.UserData;
		if (ud == null || !ud.TrySpendCoins(_price, "buy_" + _kind.ToString().ToLowerInvariant()))
		{
			UIUtil.Deny(transform.Find("BtnCoin"));
			RewardToast.Show("Not enough coins!\nNeed " + _price + " coins (you have " + (ud != null ? ud.Coins : 0) + ")");
			return;
		}
		_done = true;
		if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.BigCoinGet);
		Close(1);
		_onBought?.Invoke();
	}
}
