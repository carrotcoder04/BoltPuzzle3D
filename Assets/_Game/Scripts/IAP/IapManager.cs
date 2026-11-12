using System;
using UnityEngine;
#if IAP_ENABLED
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

/// <summary>
/// Mua hàng trong ứng dụng.
/// - Mặc định (Mock): bấm mua → cộng thưởng ngay (dùng cho Editor, test, Playable).
/// - Production: thêm package com.unity.purchasing và define IAP_ENABLED → dùng Google Play Billing / StoreKit.
/// </summary>
public class IapManager : MonoBehaviour
#if IAP_ENABLED
	, IDetailedStoreListener
#endif
{
	public static IapManager Instance { get; private set; }

	public event Action<IapProduct> OnPurchased;
	public event Action<IapProduct, string> OnPurchaseFailed;
	public event Action OnPricesUpdated;

	const string OwnedKey = "bp3d_iap_owned_";

	public bool IsOwned(string id) => PlayerPrefs.GetInt(OwnedKey + id, 0) == 1;

	public void Init()
	{
		Instance = this;
#if IAP_ENABLED
		var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
		foreach (var p in IapCatalog.Products)
			builder.AddProduct(p.Id, p.Kind == IapProductKind.Consumable ? ProductType.Consumable : ProductType.NonConsumable);
		UnityPurchasing.Initialize(this, builder);
#endif
	}

	public string GetPrice(string id)
	{
#if IAP_ENABLED
		var prod = _controller != null ? _controller.products.WithID(id) : null;
		if (prod != null && prod.availableToPurchase) return prod.metadata.localizedPriceString;
#endif
		var p = IapCatalog.Get(id);
		return p != null ? p.MockPrice : "";
	}

	public void Buy(string id, string placement)
	{
		var p = IapCatalog.Get(id);
		if (p == null) return;
		AnalyticsManager.IapClick(id, placement);
		if (p.Kind == IapProductKind.NonConsumable && IsOwned(id)) { Fail(p, "already_owned"); return; }
#if IAP_ENABLED
		if (_controller == null) { Fail(p, "not_initialized"); return; }
		_controller.InitiatePurchase(id);
#else
		Grant(p, p.MockPriceValue, "USD");
#endif
	}

	void Grant(IapProduct p, decimal price, string currency)
	{
		var ud = UserDataManager.Instance;
		var inv = InventoryManager.Instance;
		if (p.Coins > 0 && ud != null) ud.AddCoins(p.Coins, "iap");
		if (p.Boosters != null && inv != null)
			for (int i = 0; i < p.Boosters.Length; i++) if (p.Boosters[i] > 0) inv.AddBooster((BoosterType)i, p.Boosters[i]);
		if (p.UnlimitedHeartMinutes > 0 && inv != null) inv.AddUnlimitedHeart(TimeSpan.FromMinutes(p.UnlimitedHeartMinutes));
		if (p.RemoveAds && ud != null) ud.IsRemoveAds = true;
		if (p.Kind == IapProductKind.NonConsumable) { PlayerPrefs.SetInt(OwnedKey + p.Id, 1); PlayerPrefs.Save(); }
		AnalyticsManager.IapSuccess(p.Id, price, currency);
		if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.BigCoinGet);
		RewardToast.Show(RewardToast.Describe(p.Coins, p.Boosters, p.UnlimitedHeartMinutes, p.RemoveAds));
		OnPurchased?.Invoke(p);
	}

	void Fail(IapProduct p, string reason)
	{
		AnalyticsManager.IapFail(p != null ? p.Id : "", reason);
		OnPurchaseFailed?.Invoke(p, reason);
	}

	/// <summary>Khôi phục giao dịch (iOS bắt buộc có nút Restore).</summary>
	public void Restore(Action<bool> done = null)
	{
#if IAP_ENABLED
		if (_extensions == null) { done?.Invoke(false); return; }
#if UNITY_IOS
		_extensions.GetExtension<IAppleExtensions>().RestoreTransactions((ok, msg) => done?.Invoke(ok));
#else
		_extensions.GetExtension<IGooglePlayStoreExtensions>().RestoreTransactions((ok, msg) => done?.Invoke(ok));
#endif
#else
		done?.Invoke(true);
#endif
	}

#if IAP_ENABLED
	IStoreController _controller;
	IExtensionProvider _extensions;

	public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
	{
		_controller = controller;
		_extensions = extensions;
		OnPricesUpdated?.Invoke();
	}

	public void OnInitializeFailed(InitializationFailureReason error) => Debug.LogWarning("[IAP] Init failed: " + error);
	public void OnInitializeFailed(InitializationFailureReason error, string message) => Debug.LogWarning("[IAP] Init failed: " + error + " " + message);

	public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
	{
		var p = IapCatalog.Get(e.purchasedProduct.definition.id);
		if (p != null) Grant(p, e.purchasedProduct.metadata.localizedPrice, e.purchasedProduct.metadata.isoCurrencyCode);
		return PurchaseProcessingResult.Complete;
	}

	public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) => Fail(IapCatalog.Get(product.definition.id), reason.ToString());
	public void OnPurchaseFailed(Product product, PurchaseFailureDescription d) => Fail(IapCatalog.Get(product.definition.id), d.reason + " " + d.message);
#endif
}
