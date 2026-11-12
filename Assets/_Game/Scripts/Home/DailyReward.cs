using System;
using UnityEngine;

/// <summary>
/// Điểm danh hằng ngày: 7 ô quà xoay vòng (mỗi ngày theo giờ máy nhận 1 lần, lỡ ngày thì chỉ tạm dừng,
/// không mất chuỗi) + đếm tổng số ngày điểm danh với các mốc quà 10/12/16/22/30 (lặp lại mỗi 30 ngày).
/// </summary>
public static class DailyReward
{
	public struct Reward
	{
		public int Coins;
		public int Booster;        // -1 = không có, còn lại theo BoosterType
		public int BoosterCount;
		public int AllBoosters;    // cộng thêm mỗi loại Khoan/Búa/Chổi
		public int UnlimitedMinutes;
	}

	public static readonly Reward[] Rewards =
	{
		new Reward { Coins = 50, Booster = -1 },
		new Reward { Booster = (int)BoosterType.Drill, BoosterCount = 1 },
		new Reward { Coins = 100, Booster = -1 },
		new Reward { Booster = (int)BoosterType.Hammer, BoosterCount = 1 },
		new Reward { Coins = 50, Booster = -1 },
		new Reward { Booster = (int)BoosterType.Drill, BoosterCount = 1 },
		new Reward { Coins = 250, Booster = (int)BoosterType.Broom, BoosterCount = 1 },
	};

	public const int Cycle = 30;
	public static readonly int[] Milestones = { 10, 12, 16, 22, 30 };
	public static readonly Reward[] MilestoneRewards =
	{
		new Reward { Coins = 100, Booster = (int)BoosterType.Drill, BoosterCount = 1 },
		new Reward { Coins = 150, Booster = (int)BoosterType.Hammer, BoosterCount = 1 },
		new Reward { Coins = 200, Booster = (int)BoosterType.Broom, BoosterCount = 1 },
		new Reward { Coins = 300, Booster = -1, AllBoosters = 1 },
		new Reward { Coins = 500, Booster = -1, AllBoosters = 2, UnlimitedMinutes = 60 },
	};

	static UserData Data => UserDataManager.Instance != null ? UserDataManager.Instance.Data : null;

	static int Today => ToKey(DateTime.Now);
	static int ToKey(DateTime d) => d.Year * 10000 + d.Month * 100 + d.Day;

	/// <summary>Đã nhận hôm nay chưa.</summary>
	public static bool ClaimedToday => Data != null && Data.DailyLastDate == Today;
	public static bool CanClaim => Data != null && !ClaimedToday;

	/// <summary>Thời gian còn lại tới lượt quà kế tiếp (0 nếu nhận được ngay).</summary>
	public static TimeSpan TimeToNext => CanClaim ? TimeSpan.Zero : DateTime.Today.AddDays(1) - DateTime.Now;

	/// <summary>Chỉ số ô (0..6) nhận hôm nay (hoặc đã nhận hôm nay).</summary>
	public static int CurrentDay
	{
		get
		{
			var d = Data;
			if (d == null) return 0;
			int n = Rewards.Length;
			int next = ((d.DailyDay % n) + n) % n;
			return ClaimedToday ? (next + n - 1) % n : next;
		}
	}

	/// <summary>Số ô đã nhận trong vòng 7 ngày hiện tại.</summary>
	public static int ClaimedCount
	{
		get
		{
			var d = Data;
			if (d == null) return 0;
			int n = d.DailyDay % Rewards.Length;
			return ClaimedToday && n == 0 ? Rewards.Length : n;
		}
	}

	/// <summary>Tổng số ngày đã điểm danh.</summary>
	public static int Total => Data != null ? Data.DailyTotal : 0;

	/// <summary>Tiến độ trong vòng mốc hiện tại (0..30).</summary>
	public static int CycleProgress => Total <= 0 ? 0 : (Total - 1) % Cycle + 1;

	/// <summary>Nhận quà hôm nay (kèm quà mốc nếu vừa chạm mốc). Trả về chỉ số mốc vừa đạt, -1 nếu không.</summary>
	public static int Claim()
	{
		var ud = UserDataManager.Instance;
		if (ud == null || !CanClaim) return -1;
		int day = CurrentDay;
		var r = Rewards[day];
		ud.Data.DailyDay = (day + 1) % Rewards.Length;
		ud.Data.DailyLastDate = Today;
		ud.Data.DailyTotal++;
		string msg = Grant(r, "daily_reward");
		int ms = Array.IndexOf(Milestones, CycleProgress);
		if (ms >= 0) msg += "\nMilestone gift: " + Grant(MilestoneRewards[ms], "daily_milestone");
		ud.Save();
		RewardToast.Show(msg);
		return ms;
	}

	static string Grant(Reward r, string src)
	{
		var ud = UserDataManager.Instance;
		if (r.Coins > 0) ud.AddCoins(r.Coins, src);
		var boosters = new int[4];
		if (r.Booster >= 0 && r.BoosterCount > 0) boosters[r.Booster] += r.BoosterCount;
		if (r.AllBoosters > 0)
		{
			boosters[(int)BoosterType.Drill] += r.AllBoosters;
			boosters[(int)BoosterType.Hammer] += r.AllBoosters;
			boosters[(int)BoosterType.Broom] += r.AllBoosters;
		}
		var inv = InventoryManager.Instance;
		if (inv != null)
		{
			for (int i = 0; i < boosters.Length; i++) if (boosters[i] > 0) inv.AddBooster((BoosterType)i, boosters[i]);
			if (r.UnlimitedMinutes > 0) inv.AddUnlimitedHeart(TimeSpan.FromMinutes(r.UnlimitedMinutes));
		}
		return RewardToast.Describe(r.Coins, boosters, r.UnlimitedMinutes, false);
	}

	public static string CoinLabel(int coins) => coins.ToString();
	public static string BoosterLabel(Reward r) => "x" + r.BoosterCount;

	public static Sprite BoosterIcon(int booster)
	{
		var cfg = GameConfig.Instance;
		if (cfg == null) return null;
		if (booster == (int)BoosterType.Drill) return cfg.RewardDrillSprite;
		if (booster == (int)BoosterType.Hammer) return cfg.RewardHammerSprite;
		if (booster == (int)BoosterType.Broom) return cfg.RewardBroomSprite;
		return null;
	}

	public static Sprite CoinIcon => GameConfig.Instance != null ? GameConfig.Instance.RewardCoinSprite : null;

	/// <summary>Chuỗi "HH:MM:SS" hoặc "23h 41m" cho thời gian còn lại.</summary>
	public static string FormatShort(TimeSpan t) => t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes:00}m" : $"{t.Minutes}m {t.Seconds:00}s";
	public static string FormatLong(TimeSpan t) => $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
}
