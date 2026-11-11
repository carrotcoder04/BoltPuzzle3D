using Snui;
using UnityEngine;
using UnityEngine.UI;

namespace ShopUI
{
	/// <summary>Danh sách gói trong Shop (dùng cho tab Shop ở Home hoặc mở dạng dialog).</summary>
	public class ShopDialogContent : DialogContent
	{
		public string Route = "unknown";
		public RectOffset Padding = new RectOffset();
		public float Spacing = 15f;
		public bool UploadShowingLog = true;

		/// <summary>true khi nằm trong tab ShopPage (ẩn nút đóng).</summary>
		public bool IsPageMode;

		Transform _content;

		protected override void Awake()
		{
			base.Awake();
			_content = transform.Find("ShopList/Viewport/Content");
			var close = transform.FindComp<Button>("Ribbon/CloseButton");
			if (close != null) close.onClick.AddListener(() => Close(-1));
		}

		protected override void Start()
		{
			var close = transform.Find("Ribbon/CloseButton");
			if (close != null) close.gameObject.SetActive(!IsPageMode);
			Build();
			if (IapManager.Instance != null)
			{
				IapManager.Instance.OnPurchased += OnPurchased;
				IapManager.Instance.OnPricesUpdated += Build;
			}
			base.Start();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (IapManager.Instance != null)
			{
				IapManager.Instance.OnPurchased -= OnPurchased;
				IapManager.Instance.OnPricesUpdated -= Build;
			}
		}

		void OnPurchased(IapProduct p) => Build();

		string Placement => IsPageMode ? "shop_tab" : Route;

		public void Build()
		{
			var cfg = GameConfig.Instance;
			if (_content == null || cfg == null) return;
			for (int i = _content.childCount - 1; i >= 0; i--) Destroy(_content.GetChild(i).gameObject);
			EnsureLayout();

			var iap = IapManager.Instance;
			var ud = UserDataManager.Instance;
			int index = 0;
			foreach (var id in new[] { IapCatalog.Starter, IapCatalog.RemoveAds, IapCatalog.Vip, IapCatalog.Super })
			{
				var p = IapCatalog.Get(id);
				if (p == null || cfg.ShopBundleItem == null) continue;
				if (p.Kind == IapProductKind.NonConsumable && iap != null && iap.IsOwned(id)) continue;
				if (id == IapCatalog.RemoveAds && ud != null && ud.IsRemoveAds) continue;
				var go = Instantiate(cfg.ShopBundleItem, _content, false);
				var item = go.GetComponent<ShopBundleItem>() ?? go.AddComponent<ShopBundleItem>();
				item.Index = index++;
				item.Setup(p, Placement);
				FixHeight(go);
			}
			index = 0;
			foreach (var p in IapCatalog.Coins)
			{
				if (cfg.ShopCoinItem == null) break;
				var go = Instantiate(cfg.ShopCoinItem, _content, false);
				var item = go.GetComponent<ShopCoinItem>() ?? go.AddComponent<ShopCoinItem>();
				item.Index = index++;
				item.Setup(p, Placement);
				FixHeight(go);
			}
		}

		void EnsureLayout()
		{
			var v = _content.GetComponent<VerticalLayoutGroup>() ?? _content.gameObject.AddComponent<VerticalLayoutGroup>();
			v.spacing = Spacing;
			v.padding = Padding ?? new RectOffset();
			v.childAlignment = TextAnchor.UpperCenter;
			v.childControlWidth = false;
			v.childControlHeight = true;
			v.childForceExpandWidth = false;
			v.childForceExpandHeight = false;
			var f = _content.GetComponent<ContentSizeFitter>() ?? _content.gameObject.AddComponent<ContentSizeFitter>();
			f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		}

		static void FixHeight(GameObject go)
		{
			var rt = go.transform as RectTransform;
			var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
			if (rt != null) le.preferredHeight = rt.rect.height;
		}
	}
}
