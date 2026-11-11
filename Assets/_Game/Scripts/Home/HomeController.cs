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
