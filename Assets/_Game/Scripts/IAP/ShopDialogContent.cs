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
				var prefab = BundlePrefab(cfg, id);
				if (p == null || prefab == null) continue;
				if (p.Kind == IapProductKind.NonConsumable && iap != null && iap.IsOwned(id)) continue;
				if (id == IapCatalog.RemoveAds && ud != null && ud.IsRemoveAds) continue;
				var go = Instantiate(prefab, _content, false);
				var item = go.GetOrAdd<ShopBundleItem>();
				item.Index = index++;
				item.Setup(p, Placement);
				FixHeight(go);
			}
			index = 0;
			foreach (var p in IapCatalog.Coins)
			{
				if (cfg.ShopCoinItem == null) break;
				var go = Instantiate(cfg.ShopCoinItem, _content, false);
				var item = go.GetOrAdd<ShopCoinItem>();
				item.Index = index++;
				item.Setup(p, Placement);
				FixHeight(go);
			}
		}

		static GameObject BundlePrefab(GameConfig cfg, string id)
		{
			GameObject p = null;
			if (id == IapCatalog.Starter) p = cfg.StarterBundleItem;
			else if (id == IapCatalog.Vip) p = cfg.VipBundleItem;
			else if (id == IapCatalog.Super) p = cfg.SuperBundleItem;
			else if (id == IapCatalog.RemoveAds) p = cfg.RemoveAdsItem;
			return p != null ? p : cfg.ShopBundleItem;
		}

		void EnsureLayout()
		{
			var v = _content.GetOrAdd<VerticalLayoutGroup>();
			v.spacing = Spacing;
			var pad = Padding ?? new RectOffset();
			// tab Shop ở Home: chừa chỗ cho thanh điều hướng để gói cuối không bị che
			var cfg = GameConfig.Instance;
			if (IsPageMode && cfg != null) pad = new RectOffset(pad.left, pad.right, pad.top, Mathf.Max(pad.bottom, Mathf.RoundToInt(cfg.ShopPageBottomPadding)));
			v.padding = pad;
			v.childAlignment = TextAnchor.UpperCenter;
			v.childControlWidth = false;
			v.childControlHeight = true;
			v.childForceExpandWidth = false;
			v.childForceExpandHeight = false;
			var f = _content.GetOrAdd<ContentSizeFitter>();
			f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		}

		static void FixHeight(GameObject go)
		{
			var rt = go.transform as RectTransform;
			var le = go.GetOrAdd<LayoutElement>();
			if (rt != null) le.preferredHeight = rt.rect.height;
		}
	}
}
