using UnityEngine;

/// <summary>Rương mốc level: mỗi GameConfig.ChestEveryLevels màn thắng được nhận 1 rương (coin + 1 công cụ ngẫu nhiên).</summary>
public static class LevelChest
{
	static int Every => GameConfig.Instance != null ? Mathf.Max(1, GameConfig.Instance.ChestEveryLevels) : 5;
	static int Passed => UserDataManager.Instance != null ? UserDataManager.Instance.CurrentLevel - 1 : 0;
	static int Claimed => UserDataManager.Instance != null ? UserDataManager.Instance.Data.StageRewardClaimed : 0;

	public static int Earned => Passed / Every;
	public static bool IsReady => Earned > Claimed;
	/// <summary>Tiến độ tới rương kế tiếp (đã qua / cần).</summary>
	public static int Progress => IsReady ? Every : Passed - Claimed * Every;
	public static int Target => Every;
	public static int Coins => GameConfig.Instance != null ? GameConfig.Instance.ChestCoins : 100;

	/// <summary>Nhận rương: trả về loại công cụ được tặng.</summary>
	public static BoosterType Claim()
	{
		var ud = UserDataManager.Instance;
		var t = (BoosterType)Random.Range(0, 3);
		if (ud == null || !IsReady) return t;
		ud.Data.StageRewardClaimed++;
		ud.AddCoins(Coins, "level_chest");
		if (InventoryManager.Instance != null) InventoryManager.Instance.AddBooster(t, 1);
		ud.Save();
		return t;
	}
}
