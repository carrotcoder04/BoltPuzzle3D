using TMPro;
using UnityEngine;

namespace ShopUI
{
	/// <summary>Ô gói coin (CoinItem.prefab).</summary>
	public class ShopCoinItem : MonoBehaviour
	{
		public int Type;
		public int Index;

		IapProduct _product;
		string _placement;

		public void Setup(IapProduct p, string placement)
		{
			_product = p;
			_placement = placement;
			UIUtil.SetText(transform.FindComp<TMP_Text>("Count"), p.Coins.ToString("N0"));
			UIUtil.SetText(transform.FindComp<TMP_Text>("BuyBtn/Content/Price"), IapManager.Instance != null ? IapManager.Instance.GetPrice(p.Id) : p.MockPrice);
			transform.SetActive("PriceOld", false);
			transform.SetActive("Loading", false);
			transform.BindButton("BuyBtn", OnBuy);
		}

		void OnBuy()
		{
			if (_product != null && IapManager.Instance != null) IapManager.Instance.Buy(_product.Id, _placement);
		}
	}
}
