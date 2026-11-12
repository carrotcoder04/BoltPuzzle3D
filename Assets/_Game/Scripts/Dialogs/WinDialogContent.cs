using DG.Tweening;
using Snui;
using TMPro;
using UnityEngine;

/// <summary>Bảng nhận thưởng khi thắng: hiện số coin, bấm để nhận rồi về Home.</summary>
public class WinDialogContent : DialogContent
{
	bool _claimed;

	protected override void Awake()
	{
		base.Awake();
		var gc = GameController.Instance;
		int reward = gc != null ? gc.WinReward : 0;
		UIUtil.SetText(transform.FindText("RewardNode/Coin/Count"), "+" + reward);
		transform.SetActive("RewardNode/Special", false);
		transform.SetActive("RewardNode/ThemeTarget", false);
		bool ads = AdsService.IsRewardedReady;
		transform.SetActive("BtnRv", ads);
		// các node trang trí bị tắt sẵn trong prefab: bật lại cho bảng thắng đầy đặn
		transform.SetActive("ArtTitle", true);
		// Ribbon "∞ 1h" chỉ dành cho thưởng có thời hạn; thưởng thắng màn là coin thường nên luôn tắt
		transform.SetActive("RewardNode/Coin/Ribbon", false);
		transform.BindButton("BtnTapTo", () => Claim(1));
		transform.BindButton("BtnClaim", () => Claim(1));
		if (ads) transform.BindButton("BtnRv", () => AdsService.ShowRewarded("win_x2", ok => Claim(ok ? 2 : 1)));
	}

	protected override void OnShown()
	{
		if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.BigCoinGet);
		var link = gameObject;
		// tiêu đề WELL DONE! bật ra rồi nhún nhẹ
		var title = transform.Find("ArtTitle");
		if (title != null)
		{
			var s0 = title.localScale;
			title.localScale = s0 * 0.3f;
			var seq = DOTween.Sequence().SetUpdate(true).SetLink(link);
			seq.Append(title.DOScale(s0, 0.45f).SetEase(Ease.OutBack));
			seq.Append(title.DOScale(s0 * 1.04f, 0.8f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo));
		}
		// ngôi sao xoay nhẹ + nảy
		var star = transform.Find("StarNode/Star");
		if (star != null)
		{
			star.DOPunchScale(Vector3.one * 0.25f, 0.5f, 6, 0.5f).SetDelay(0.2f).SetUpdate(true).SetLink(link);
			star.DOLocalRotate(new Vector3(0f, 0f, 6f), 1.2f).From(new Vector3(0f, 0f, -6f)).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(link);
		}
		// đồng xu nảy lên và đếm số thưởng
		var coin = transform.Find("RewardNode/Coin");
		if (coin != null)
		{
			coin.DOPunchScale(Vector3.one * 0.2f, 0.45f, 6, 0.5f).SetDelay(0.35f).SetUpdate(true).SetLink(link);
			var light = coin.Find("BackLight");
			if (light != null) light.DOLocalRotate(new Vector3(0f, 0f, -360f), 6f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1).SetUpdate(true).SetLink(link);
		}
		var count = transform.FindComp<TMP_Text>("RewardNode/Coin/Count");
		var gc = GameController.Instance;
		int reward = gc != null ? gc.WinReward : 0;
		if (count != null && reward > 0)
			DOVirtual.Int(0, reward, 0.7f, v => count.text = "+" + v).SetDelay(0.35f).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(link);
		// nút nhận thưởng nhịp nhẹ
		var btn = transform.Find("BtnTapTo");
		if (btn != null) btn.DOScale(btn.localScale * 1.05f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(0.8f).SetUpdate(true).SetLink(link);
	}

	protected override void OnCloseClicked() => Claim(1);

	void Claim(int mult)
	{
		if (_claimed) return;
		_claimed = true;
		var gc = GameController.Instance;
		if (gc != null) gc.ClaimWinAndGoHome(mult);
		else Close(0);
	}
}
