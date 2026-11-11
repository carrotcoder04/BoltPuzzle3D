using System;
using UnityEngine;

/// <summary>Dữ liệu người chơi được lưu dưới dạng JSON trong PlayerPrefs.</summary>
[Serializable]
public class UserData
{
	public int Version = 1;
	public int CurrentLevel = 1;      // 1-based, tăng dần mãi (map sang puzzle bằng LevelDatabase)
	public int Coins = 200;
	public int Stars;
	public bool SoundOn = true;
	public bool MusicOn = true;
	public bool VibrationOn = true;
	public bool ColorBlindMode;
	public bool IsRemoveAds;
	public int LevelAttempt;           // số lần thử của level hiện tại
	public long FirstLaunchUtc;
	public int SessionCount;
	public int StageRewardClaimed;     // số mốc rương Level Chest đã nhận
	public int SpecialScrewProgress;   // tiến trình Special Screw
}

/// <summary>
/// Đọc/ghi UserData. Truy cập qua UserDataManager.Instance hoặc Managers.UserData.
/// Tự lưu khi app pause/quit và có thể gọi Save() sau thay đổi quan trọng.
/// </summary>
public class UserDataManager : MonoBehaviour
{
	const string PrefKey = "bp3d_user_data";

	public static UserDataManager Instance { get; private set; }

	public UserData Data { get; private set; } = new UserData();

	public event Action<int> OnCoinsChanged;
	public event Action<int> OnLevelChanged;
	public event Action OnSettingsChanged;

	bool _dirty;

	public void Init()
	{
		Instance = this;
		Load();
		if (Data.FirstLaunchUtc == 0) Data.FirstLaunchUtc = DateTime.UtcNow.Ticks;
		Data.SessionCount++;
		Save();
	}

	public void Load()
	{
		var json = PlayerPrefs.GetString(PrefKey, string.Empty);
		if (string.IsNullOrEmpty(json)) { Data = new UserData(); return; }
		try { Data = JsonUtility.FromJson<UserData>(json) ?? new UserData(); }
		catch (Exception e)
		{
			Debug.LogWarning("[UserData] Load failed, reset: " + e.Message);
			Data = new UserData();
		}
	}

	public void Save()
	{
		PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(Data));
		PlayerPrefs.Save();
		_dirty = false;
	}

	public void MarkDirty() => _dirty = true;

	void OnApplicationPause(bool paused) { if (paused && _dirty) Save(); }
	void OnApplicationQuit() { if (_dirty) Save(); }

	// ---------- Coins ----------
	public int Coins => Data.Coins;

	public void AddCoins(int amount, string source = "")
	{
		if (amount == 0) return;
		Data.Coins = Mathf.Max(0, Data.Coins + amount);
		Save();
		if (amount > 0) AnalyticsManager.CurrencyEarn("coin", amount, source);
		OnCoinsChanged?.Invoke(Data.Coins);
	}

	public bool TrySpendCoins(int amount, string reason = "")
	{
		if (amount < 0 || Data.Coins < amount) return false;
		Data.Coins -= amount;
		Save();
		AnalyticsManager.CurrencySpend("coin", amount, reason);
		OnCoinsChanged?.Invoke(Data.Coins);
		return true;
	}

	// ---------- Level ----------
	public int CurrentLevel => Data.CurrentLevel;

	public void OnLevelStart()
	{
		Data.LevelAttempt++;
		MarkDirty();
	}

	public void CompleteLevel()
	{
		Data.CurrentLevel++;
		Data.LevelAttempt = 0;
		Save();
		OnLevelChanged?.Invoke(Data.CurrentLevel);
	}

	// ---------- Settings ----------
	public bool SoundOn { get => Data.SoundOn; set { Data.SoundOn = value; SettingsChanged(); } }
	public bool MusicOn { get => Data.MusicOn; set { Data.MusicOn = value; SettingsChanged(); } }
	public bool VibrationOn { get => Data.VibrationOn; set { Data.VibrationOn = value; SettingsChanged(); } }
	public bool ColorBlindMode { get => Data.ColorBlindMode; set { Data.ColorBlindMode = value; SettingsChanged(); } }

	public bool IsRemoveAds
	{
		get => Data.IsRemoveAds;
		set { Data.IsRemoveAds = value; Save(); }
	}

	void SettingsChanged()
	{
		Save();
		OnSettingsChanged?.Invoke();
	}

#if UNITY_EDITOR
	[ContextMenu("Reset User Data")]
	void ResetData()
	{
		PlayerPrefs.DeleteKey(PrefKey);
		Data = new UserData();
		Save();
	}
#endif
}
