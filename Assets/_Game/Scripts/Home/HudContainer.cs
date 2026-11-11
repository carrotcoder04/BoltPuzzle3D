using UnityEngine;

/// <summary>Nhóm HUD trên cùng (Heart + Coin). Bấm Coin → mở tab Shop (ở Home).</summary>
public class HudContainer : MonoBehaviour
{
	void Start()
	{
		transform.BindButton("CoinHud", () =>
		{
			if (NavigationController.Instance != null) NavigationController.Instance.GoTo("ShopPage");
		});
	}
}
