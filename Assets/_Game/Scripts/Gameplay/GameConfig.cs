using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cấu hình gameplay (Resources/GameConfig): danh sách màn chơi, prefab hộp, dialog, bảng màu ốc.
/// </summary>
[CreateAssetMenu(menuName = "BoltPuzzle3D/Game Config", fileName = "GameConfig")]
public class GameConfig : ScriptableObject
{
	static GameConfig _instance;
	public static GameConfig Instance => _instance != null ? _instance : (_instance = Resources.Load<GameConfig>("GameConfig"));

	[Header("Levels (thứ tự chơi)")]
	public List<GameObject> Levels = new List<GameObject>();
	[Tooltip("Sau khi hết danh sách, quay vòng từ level này (1-based)")]
	public int LoopFromLevel = 11;

	[Header("Prefabs")]
	public GameObject BoxPrefab;
	public GameObject PauseDialog;
	public GameObject WinDialog;
	public GameObject FailDialog;

	[Header("Home / Shop")]
	public GameObject ShopContent;
	public GameObject ShopCoinItem;
	public GameObject ShopBundleItem;
	[Tooltip("Prefab riêng cho từng gói (có ảnh coin, công cụ, tim); để trống thì dùng ShopBundleItem")]
	public GameObject StarterBundleItem;
	public GameObject VipBundleItem;
	public GameObject SuperBundleItem;
	public GameObject RemoveAdsItem;
	[Tooltip("Khoảng trống dưới danh sách Shop ở tab Home để không bị thanh điều hướng che")]
	public float ShopPageBottomPadding = 220f;
	public GameObject SettingsDialog;

	[Header("Tutorial màn 1")]
	public GameObject TutorialGuide;
	public Sprite TutorialHand;
	public Sprite TutorialTapRing;
	public Sprite TutorialRotate;
	public GameObject StageRewardDialog;
	[Tooltip("Popup mua công cụ / thêm khay bằng coin (BuyToolDialog)")]
	public GameObject BuyItemDialog;
	[Tooltip("Node ToolsSpine (spine Khoan/Búa) trong UiCanvas.prefab, dùng khi UiCanvas của scene không có")]
	public GameObject ToolsSpine;

	[Header("Zoom (màn Game)")]
	public Sprite ZoomInSprite;
	public Sprite ZoomOutSprite;
	public Sprite FocusSprite;
	public Sprite ZoomHandleSprite;

	[Header("Điểm danh hằng ngày")]
	public GameObject DailyRewardDialog;
	public Sprite DailyIconSprite;
	public Sprite RewardCoinSprite;
	public Sprite RewardDrillSprite;
	public Sprite RewardHammerSprite;
	public Sprite RewardBroomSprite;
	public Sprite RewardHeartSprite;
	public int ChestEveryLevels = 5;
	public int ChestCoins = 100;

	[Header("Gameplay")]
	public int StartHoleCount = 5;
	public int StartBoxSlots = 2;
	public int WinCoinReward = 20;
	public int[] RevivePrices = { 450, 600, 900 };
	public int ToolPrice = 300;
	public int BoxSlotPrice = 200;

	[Header("13 màu ốc – khớp box_1..box_13")]
	public Color[] ScrewColors =
	{
		// Lấy theo màu nắp hộp box_N_lid (index i ứng với box_{i+1}) để ốc và hộp luôn khớp màu.
		new Color32(0xFE, 0xD3, 0x36, 0xFF), // 1 vàng
		new Color32(0x60, 0x78, 0xFE, 0xFF), // 2 xanh dương
		new Color32(0xB1, 0x51, 0xF5, 0xFF), // 3 tím
		new Color32(0xE2, 0x48, 0x5E, 0xFF), // 4 đỏ
		new Color32(0xF4, 0x91, 0x3A, 0xFF), // 5 cam
		new Color32(0x62, 0xC2, 0xEE, 0xFF), // 6 xanh trời
		new Color32(0xA8, 0xDC, 0x43, 0xFF), // 7 xanh lá mạ
		new Color32(0xB5, 0xBE, 0xD2, 0xFF), // 8 xám
		new Color32(0xFE, 0x86, 0xB9, 0xFF), // 9 hồng
		new Color32(0xA0, 0x76, 0x5E, 0xFF), // 10 nâu
		new Color32(0xF4, 0xF4, 0xF4, 0xFF), // 11 trắng
		new Color32(0x3D, 0xD4, 0x96, 0xFF), // 12 ngọc
		new Color32(0x15, 0xA2, 0x21, 0xFF), // 13 xanh lá
	};

	public GameObject GetLevelPrefab(int level)
	{
		if (Levels == null || Levels.Count == 0) return null;
		int idx = level - 1;
		if (idx < Levels.Count) return Levels[Mathf.Max(0, idx)];
		int loopStart = Mathf.Clamp(LoopFromLevel - 1, 0, Levels.Count - 1);
		int span = Levels.Count - loopStart;
		return Levels[loopStart + (idx - Levels.Count) % span];
	}

	/// <summary>Số màu dùng cho màn có n ốc.</summary>
	public int ColorCountFor(int screwCount, int level)
	{
		int triples = screwCount / 3;
		int byLevel = 3 + level / 3;
		return Mathf.Clamp(Mathf.Min(byLevel, Mathf.Max(3, triples / 2)), 3, ScrewColors.Length);
	}
}
