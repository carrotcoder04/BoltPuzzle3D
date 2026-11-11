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
			UIUtil.SetText(transform.FindComp<TMP_Text>("Ribbon/Text"), p.Title);
			UIUtil.SetText(transform.FindComp<TMP_Text>("Desc"), p.Desc);
			var price = transform.FindDeep("Snui_Price") ?? transform.FindDeep("Price");
			UIUtil.SetText(price != null ? price.GetComponent<TMP_Text>() : null, IapManager.Instance != null ? IapManager.Instance.GetPrice(p.Id) : p.MockPrice);
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
