using Snui;
using UnityEngine;

/// <summary>Điều phối HomeScene: khởi tạo managers, gốc dialog, tự mở rương Level Chest khi đủ mốc.</summary>
public class HomeController : MonoBehaviour
{
	public HomePage HomePage;
	public Canvas HomeCanvas;

	public static HomeController Instance { get; private set; }

	void Awake()
	{
		Instance = this;
		Managers.Ensure();
		if (HomeCanvas == null)
		{
			var go = GameObject.Find("HomeUICanvas");
			HomeCanvas = go != null ? go.GetComponent<Canvas>() : null;
		}
		if (HomePage == null) HomePage = FindAnyObjectByType<HomePage>(FindObjectsInactive.Include);
		DialogManager.DefaultRoot = HomeCanvas != null ? HomeCanvas.transform : null;
		var mask = HomeCanvas != null ? HomeCanvas.transform.Find("ClickMask") : null;
		if (mask != null) mask.gameObject.SetActive(false);
	}

	void Start()
	{
		if (Managers.Audio != null) Managers.Audio.PlaySfx(Sfx.HomeIn);
		if (LevelChest.IsReady) Invoke(nameof(OpenChest), 0.6f);
		// nút điểm danh ở cột sự kiện + tự mở bảng nếu hôm nay chưa nhận (sau rương mốc nếu có)
		var events = HomePage != null ? HomePage.transform.FindDeep("HomeEvents") as RectTransform : null;
		if (events != null && HomeDailyButton.Instance == null) HomeDailyButton.Create(events);
		if (DailyReward.CanClaim) Invoke(nameof(TryOpenDaily), LevelChest.IsReady ? 1.6f : 0.8f);
	}

	void TryOpenDaily()
	{
		if (!DailyReward.CanClaim) return;
		if (DialogManager.Count > 0) { Invoke(nameof(TryOpenDaily), 1f); return; }
		DailyRewardDialogContent.Open();
	}

	void OnDestroy() { if (Instance == this) Instance = null; }

	public void OpenChest()
	{
		var cfg = GameConfig.Instance;
		if (cfg != null && cfg.StageRewardDialog != null && LevelChest.IsReady && DialogManager.Count == 0)
			DialogManager.Show(cfg.StageRewardDialog);
	}

	public void OpenSettings()
	{
		var cfg = GameConfig.Instance;
		if (cfg != null && cfg.SettingsDialog != null) DialogManager.Show(cfg.SettingsDialog);
	}
}
