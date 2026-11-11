using Snui;
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
		transform.BindButton("BtnTapTo", () => Claim(1));
		transform.BindButton("BtnClaim", () => Claim(1));
		if (ads) transform.BindButton("BtnRv", () => AdsService.ShowRewarded("win_x2", ok => Claim(ok ? 2 : 1)));
	}

	protected override void OnShown()
	{
		if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.BigCoinGet);
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
