using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>Hiển thị số coin (Bg/Text), chạy số khi thay đổi.</summary>
public class CoinHud : MonoBehaviour
{
	public string InventoryKey;

	TMP_Text _text;
	int _shown;
	Tween _tw;

	void Awake() => _text = transform.FindText("Text");

	void OnEnable()
	{
		Managers.Ensure();
		var ud = Managers.UserData;
		if (ud == null) return;
		ud.OnCoinsChanged += OnCoins;
		_shown = ud.Coins;
		UIUtil.SetText(_text, UIUtil.ShortNumber(_shown));
	}

	void OnDisable()
	{
		if (Managers.UserData != null) Managers.UserData.OnCoinsChanged -= OnCoins;
		_tw?.Kill();
	}

	void OnCoins(int v)
	{
		_tw?.Kill();
		_tw = DOVirtual.Int(_shown, v, 0.5f, x => { _shown = x; UIUtil.SetText(_text, UIUtil.ShortNumber(x)); }).SetUpdate(true);
		transform.DOKill(true);
		transform.DOPunchScale(Vector3.one * 0.1f, 0.25f).SetUpdate(true);
	}
}
