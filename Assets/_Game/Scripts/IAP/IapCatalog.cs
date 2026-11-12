using System;
using System.Collections.Generic;

public enum IapProductKind { Consumable, NonConsumable }

[Serializable]
public class IapProduct
{
	public string Id;
	public IapProductKind Kind;
	public string Title;
	public string Desc;
	public string MockPrice;   // giá hiển thị khi chưa có store
	public decimal MockPriceValue;
	// Phần thưởng
	public int Coins;
	public int[] Boosters;     // theo BoosterType: Drill, Hammer, Broom, AddBox
	public int UnlimitedHeartMinutes;
	public bool RemoveAds;
}

/// <summary>Danh mục gói IAP (khớp mục 4.5 trong kế hoạch). Sửa Id cho trùng với Google Play / App Store Connect.</summary>
public static class IapCatalog
{
	public const string RemoveAds = "screw3d.removeads";
	public const string Starter = "screw3d.starter.bundle";
	public const string Vip = "screw3d.vip.bundle";
	public const string Super = "screw3d.super.bundle";

	public static readonly List<IapProduct> Products = new List<IapProduct>
	{
		new IapProduct { Id = RemoveAds, Kind = IapProductKind.NonConsumable, Title = "Remove Ads", Desc = "Remove interstitial ads forever", MockPrice = "$2.99", MockPriceValue = 2.99m, RemoveAds = true },
		new IapProduct { Id = Starter, Kind = IapProductKind.NonConsumable, Title = "Starter Bundle", Desc = "1,000 coins + 2 of each tool + 1h unlimited lives", MockPrice = "$1.99", MockPriceValue = 1.99m, Coins = 1000, Boosters = new[] { 2, 2, 2, 0 }, UnlimitedHeartMinutes = 60 },
		new IapProduct { Id = Vip, Kind = IapProductKind.Consumable, Title = "VIP Bundle", Desc = "5,000 coins + 5 of each tool + 6h unlimited lives", MockPrice = "$9.99", MockPriceValue = 9.99m, Coins = 5000, Boosters = new[] { 5, 5, 5, 0 }, UnlimitedHeartMinutes = 360 },
		new IapProduct { Id = Super, Kind = IapProductKind.Consumable, Title = "Super Bundle", Desc = "15,000 coins + 15 of each tool + 24h unlimited lives + no ads", MockPrice = "$29.99", MockPriceValue = 29.99m, Coins = 15000, Boosters = new[] { 15, 15, 15, 0 }, UnlimitedHeartMinutes = 1440, RemoveAds = true },
		new IapProduct { Id = "screw3d.coin.1", Kind = IapProductKind.Consumable, Title = "200", MockPrice = "$0.99", MockPriceValue = 0.99m, Coins = 200 },
		new IapProduct { Id = "screw3d.coin.2", Kind = IapProductKind.Consumable, Title = "500", MockPrice = "$1.99", MockPriceValue = 1.99m, Coins = 500 },
		new IapProduct { Id = "screw3d.coin.3", Kind = IapProductKind.Consumable, Title = "1200", MockPrice = "$4.99", MockPriceValue = 4.99m, Coins = 1200 },
		new IapProduct { Id = "screw3d.coin.4", Kind = IapProductKind.Consumable, Title = "2500", MockPrice = "$9.99", MockPriceValue = 9.99m, Coins = 2500 },
		new IapProduct { Id = "screw3d.coin.5", Kind = IapProductKind.Consumable, Title = "6500", MockPrice = "$24.99", MockPriceValue = 24.99m, Coins = 6500 },
		new IapProduct { Id = "screw3d.coin.6", Kind = IapProductKind.Consumable, Title = "15000", MockPrice = "$49.99", MockPriceValue = 49.99m, Coins = 15000 },
	};

	public static IapProduct Get(string id) => Products.Find(p => p.Id == id);
	public static IEnumerable<IapProduct> Coins => Products.FindAll(p => p.Id.StartsWith("screw3d.coin."));
}
