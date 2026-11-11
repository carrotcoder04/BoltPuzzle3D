using System;
using UnityEngine;

public enum BoosterType
{
	Drill = 0,
	Hammer = 1,
	Broom = 2,
	AddBox = 3
}

[Serializable]
public class InventoryData
{
	public int Hearts = InventoryManager.MaxHearts;
	public long NextHeartUtc;          // ticks: thời điểm hồi tim kế tiếp (0 = đầy)
	public long UnlimitedHeartUntilUtc; // ticks: tim vô hạn tới thời điểm này
	public int[] Boosters = { 1, 1, 1, 1 }; // theo thứ tự BoosterType
}

/// <summary>
/// Quản lý Tim (mạng chơi, tự hồi) và 4 Booster. Lưu riêng trong PlayerPrefs.
/// </summary>
public class InventoryManager : MonoBehaviour
{
	public const int MaxHearts = 5;
	public static readonly TimeSpan HeartRegen = TimeSpan.FromMinutes(30);
	const string PrefKey = "bp3d_inventory";

	public static InventoryManager Instance { get; private set; }

	public InventoryData Data { get; private set; } = new InventoryData();

	public event Action OnHeartsChanged;
	public event Action<BoosterType, int> OnBoosterChanged;

	float _tick;

	public void Init()
	{
		Instance = this;
		Load();
		RegenHearts();
	}

	void Load()
	{
		var json = PlayerPrefs.GetString(PrefKey, string.Empty);
		if (!string.IsNullOrEmpty(json))
		{
			try { Data = JsonUtility.FromJson<InventoryData>(json) ?? new InventoryData(); }
			catch { Data = new InventoryData(); }
		}
		int n = Enum.GetValues(typeof(BoosterType)).Length;
		if (Data.Boosters == null || Data.Boosters.Length < n)
		{
			var b = new int[n];
			if (Data.Boosters != null) Array.Copy(Data.Boosters, b, Data.Boosters.Length);
			Data.Boosters = b;
		}
	}

	public void Save()
	{
		PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(Data));
		PlayerPrefs.Save();
	}

	void Update()
	{
		_tick += Time.unscaledDeltaTime;
		if (_tick < 1f) return;
		_tick = 0f;
		if (RegenHearts()) OnHeartsChanged?.Invoke();
	}

	// ---------- Hearts ----------
	static DateTime Now => DateTime.UtcNow;

	public bool IsUnlimitedHeart => Data.UnlimitedHeartUntilUtc > Now.Ticks;
	public TimeSpan UnlimitedHeartRemaining => IsUnlimitedHeart ? new DateTime(Data.UnlimitedHeartUntilUtc) - Now : TimeSpan.Zero;
	public int Hearts => Data.Hearts;
	public bool HasHeart => IsUnlimitedHeart || Data.Hearts > 0;

	/// <summary>Thời gian còn lại tới khi hồi thêm 1 tim (Zero nếu đã đầy).</summary>
	public TimeSpan NextHeartRemaining
	{
		get
		{
			if (Data.Hearts >= MaxHearts || Data.NextHeartUtc == 0) return TimeSpan.Zero;
			var t = new DateTime(Data.NextHeartUtc) - Now;
			return t < TimeSpan.Zero ? TimeSpan.Zero : t;
		}
	}

	/// <summary>Cộng tim theo thời gian đã trôi qua. Trả về true nếu có thay đổi.</summary>
	bool RegenHearts()
	{
		if (Data.Hearts >= MaxHearts) { if (Data.NextHeartUtc != 0) { Data.NextHeartUtc = 0; Save(); } return false; }
		if (Data.NextHeartUtc == 0) { Data.NextHeartUtc = (Now + HeartRegen).Ticks; Save(); return false; }
		bool changed = false;
		while (Data.Hearts < MaxHearts && Now.Ticks >= Data.NextHeartUtc)
		{
			Data.Hearts++;
			Data.NextHeartUtc += HeartRegen.Ticks;
			changed = true;
		}
		if (Data.Hearts >= MaxHearts) Data.NextHeartUtc = 0;
		if (changed) Save();
		return changed;
	}

	/// <summary>Trừ 1 tim (khi thua / chơi lại). Không trừ nếu đang tim vô hạn.</summary>
	public bool UseHeart()
	{
		if (IsUnlimitedHeart) return true;
		if (Data.Hearts <= 0) return false;
		if (Data.Hearts >= MaxHearts) Data.NextHeartUtc = (Now + HeartRegen).Ticks;
		Data.Hearts--;
		Save();
		AnalyticsManager.CurrencySpend("heart", 1, "level");
		OnHeartsChanged?.Invoke();
		return true;
	}

	public void AddHearts(int amount)
	{
		Data.Hearts = Mathf.Max(0, Data.Hearts + amount);
		if (Data.Hearts >= MaxHearts) Data.NextHeartUtc = 0;
		Save();
		OnHeartsChanged?.Invoke();
	}

	public void RefillHearts() => AddHearts(MaxHearts - Data.Hearts);

	public void AddUnlimitedHeart(TimeSpan duration)
	{
		long start = Math.Max(Data.UnlimitedHeartUntilUtc, Now.Ticks);
		Data.UnlimitedHeartUntilUtc = start + duration.Ticks;
		Save();
		OnHeartsChanged?.Invoke();
	}

	// ---------- Boosters ----------
	public int GetBooster(BoosterType type) => Data.Boosters[(int)type];

	public void AddBooster(BoosterType type, int amount)
	{
		Data.Boosters[(int)type] = Mathf.Max(0, Data.Boosters[(int)type] + amount);
		Save();
		OnBoosterChanged?.Invoke(type, Data.Boosters[(int)type]);
	}

	public bool TryUseBooster(BoosterType type)
	{
		if (Data.Boosters[(int)type] <= 0) return false;
		AddBooster(type, -1);
		return true;
	}

#if UNITY_EDITOR
	[ContextMenu("Reset Inventory")]
	void ResetData()
	{
		Data = new InventoryData();
		Save();
		OnHeartsChanged?.Invoke();
	}
#endif
}
