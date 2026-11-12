using TMPro;
using UnityEngine;

namespace ShopUI
{
	/// <summary>Ô gói Bundle (BundleBase.prefab): mô tả, giá, nút mua.</summary>
	public class ShopBundleItem : MonoBehaviour
	{
		public int Type;
		public int Index;

		IapProduct _product;
		string _placement;

		public void Setup(IapProduct p, string placement)
		{
			_product = p;
			_placement = placement;
			bool rich = transform.Find("Rewards/MainReward") != null || transform.Find("Rewards/NoAdsIcon") != null;
			if (rich)
			{
				// prefab gói có sẵn ảnh: tên gói ở Desc, ruy băng giữ chữ gốc (LIMITED OFFER!/MOST POPULAR!/BEST VALUE!)
				UIUtil.SetText(transform.FindComp<TMP_Text>("Desc"), p.Title.ToUpperInvariant());
				FillRewards(p);
			}
			else
			{
				UIUtil.SetText(transform.FindComp<TMP_Text>("Ribbon/Text"), p.Title);
				UIUtil.SetText(transform.FindComp<TMP_Text>("Desc"), p.Desc);
			}
			var price = transform.FindDeep("Snui_Price") ?? transform.FindDeep("Price");
			UIUtil.SetText(price != null ? price.GetComponent<TMP_Text>() : null, IapManager.Instance != null ? IapManager.Instance.GetPrice(p.Id) : p.MockPrice);
			transform.SetActive("PriceOld", false);
			transform.SetActive("Loading", false);
			transform.BindButton("BuyBtn", OnBuy);
		}

		/// <summary>Ghi số lượng thật của gói vào các ô thưởng của prefab (coin, khoan/búa/chổi, tim vô hạn).</summary>
		void FillRewards(IapProduct p)
		{
			UIUtil.SetText(transform.FindComp<TMP_Text>("Rewards/MainReward/Count"), p.Coins.ToString("N0", System.Globalization.CultureInfo.InvariantCulture));
			transform.SetActive("Rewards/MainReward", p.Coins > 0);
			var box = transform.Find("Rewards/RewardContainer");
			if (box != null)
			{
				foreach (Transform cell in box)
				{
					var icon = cell.Find("Icon");
					var img = icon != null ? icon.GetComponent<UnityEngine.UI.Image>() : null;
					string n = img != null && img.sprite != null ? img.sprite.name.ToLowerInvariant() : "";
					int idx = n.Contains("drill") ? 0 : n.Contains("hammer") ? 1 : n.Contains("broom") ? 2 : -1;
					if (idx < 0) continue;
					int count = p.Boosters != null && idx < p.Boosters.Length ? p.Boosters[idx] : 0;
					UIUtil.SetText(cell.FindComp<TMP_Text>("Text"), "x" + count);
					cell.gameObject.SetActive(count > 0);
				}
				box.gameObject.SetActive(p.Boosters != null && System.Array.Exists(p.Boosters, x => x > 0));
			}
			var heart = transform.Find("Rewards/RewardContainer2");
			if (heart != null)
			{
				heart.gameObject.SetActive(p.UnlimitedHeartMinutes > 0);
				var t = System.TimeSpan.FromMinutes(p.UnlimitedHeartMinutes);
				string s = t.TotalHours >= 1 ? ((int)t.TotalHours) + "h" : t.Minutes + "m";
				var txt = heart.GetComponentInChildren<TMP_Text>(true);
				UIUtil.SetText(txt, s);
			}
		}

		void OnBuy()
		{
			if (_product != null && IapManager.Instance != null) IapManager.Instance.Buy(_product.Id, _placement);
		}
	}
}
