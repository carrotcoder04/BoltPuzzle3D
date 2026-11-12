using System;
using UnityEngine;

/// <summary>
/// Điểm danh hằng ngày: 7 ngày quà xoay vòng. Mỗi ngày (theo giờ máy) nhận 1 lần;
/// bỏ lỡ 1 ngày thì chuỗi quay lại Ngày 1.
/// </summary>
public static class DailyReward
{
	public struct Reward
	{
		public int Coins;
		public int Booster;        // -1 = không có, còn lại theo BoosterType
		public int BoosterCount;
		public int UnlimitedMinutes;
	}

	public static readonly Reward[] Rewards =
	{
		new Reward { Coins = 100, Booster = -1 },
		new Reward { Booster = (int)BoosterType.Drill, BoosterCount = 1 },
		new Reward { Coins = 200, Booster = -1 },
		new Reward { Booster = (int)BoosterType.Hammer, BoosterCount = 1 },
		new Reward { Coins = 300, Booster = -1 },
		new Reward { Booster = (int)BoosterType.Broom, BoosterCount = 1 },
		new Reward { Coins = 500, Booster = -1, UnlimitedMinutes = 60 },
	};

	static UserData Data => UserDataManager.Instance != null ? UserDataManager.Instance.Data : null;

	static int Today => ToKey(DateTime.Now);
	static int ToKey(DateTime d) => d.Year * 10000 + d.Month * 100 + d.Day;
	static DateTime FromKey(int k) => new DateTime(k / 10000, Mathf.Clamp(k / 100 % 100, 1, 12), Mathf.Clamp(k % 100, 1, 31));

	/// <summary>Đã nhận hôm nay chưa.</summary>
	public static bool ClaimedToday => Data != null && Data.DailyLastDate == Today;
	public static bool CanClaim => Data != null && !ClaimedToday;

	/// <summary>Chỉ số ngày (0..6) sẽ nhận hôm nay (hoặc đã nhận hôm nay).</summary>
	public static int CurrentDay
	{
		get
		{
			var d = Data;
			if (d == null) return 0;
			if (ClaimedToday) return (d.DailyDay + Rewards.Length - 1) % Rewards.Length;
			return StreakBroken ? 0 : d.DailyDay % Rewards.Length;
		}
	}

	/// <summary>Số ngày đã nhận trong chuỗi hiện tại (để tô các ô đã nhận).</summary>
	public static int ClaimedCount
	{
		get
		{
			var d = Data;
			if (d == null || StreakBroken) return 0;
			int n = d.DailyDay % Rewards.Length;
			return ClaimedToday && n == 0 ? Rewards.Length : n;
		}
	}

	static bool StreakBroken
	{
		get
		{
			var d = Data;
			if (d == null || d.DailyLastDate <= 0 || ClaimedToday) return false;
			try { return (DateTime.Now.Date - FromKey(d.DailyLastDate).Date).TotalDays > 1.5; }
			catch { return true; }
		}
	}

	/// <summary>Nhận quà hôm nay. Trả về quà đã nhận (null nếu đã nhận rồi).</summary>
	public static Reward? Claim()
	{
		var ud = UserDataManager.Instance;
		if (ud == null || !CanClaim) return null;
		int day = CurrentDay;
		var r = Rewards[day];
		if (r.Coins > 0) ud.AddCoins(r.Coins, "daily_reward");
		var inv = InventoryManager.Instance;
		if (inv != null)
		{
			if (r.Booster >= 0 && r.BoosterCount > 0) inv.AddBooster((BoosterType)r.Booster, r.BoosterCount);
			if (r.UnlimitedMinutes > 0) inv.AddUnlimitedHeart(TimeSpan.FromMinutes(r.UnlimitedMinutes));
		}
		ud.Data.DailyDay = (day + 1) % Rewards.Length;
		ud.Data.DailyLastDate = Today;
		ud.Save();
		int[] boosters = null;
		if (r.Booster >= 0) { boosters = new int[4]; boosters[r.Booster] = r.BoosterCount; }
		RewardToast.Show(RewardToast.Describe(r.Coins, boosters, r.UnlimitedMinutes, false));
		return r;
	}

	public static string Label(Reward r)
	{
		if (r.Booster >= 0) return "x" + r.BoosterCount;
		if (r.UnlimitedMinutes > 0) return r.Coins + " + " + (r.UnlimitedMinutes >= 60 ? r.UnlimitedMinutes / 60 + "h" : r.UnlimitedMinutes + "m") + " lives";
		return r.Coins.ToString();
	}

	public static Sprite Icon(Reward r)
	{
		var cfg = GameConfig.Instance;
		if (cfg == null) return null;
		if (r.Booster == (int)BoosterType.Drill) return cfg.RewardDrillSprite;
		if (r.Booster == (int)BoosterType.Hammer) return cfg.RewardHammerSprite;
		if (r.Booster == (int)BoosterType.Broom) return cfg.RewardBroomSprite;
		if (r.UnlimitedMinutes > 0 && cfg.RewardHeartSprite != null) return cfg.RewardHeartSprite;
		return cfg.RewardCoinSprite;
	}
}
