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
		new Color32(0xF0, 0xD0, 0x30, 0xFF), // 1 vàng
		new Color32(0x60, 0x70, 0xF0, 0xFF), // 2 xanh dương
		new Color32(0xB0, 0x50, 0xF0, 0xFF), // 3 tím
		new Color32(0xE0, 0x40, 0x50, 0xFF), // 4 đỏ
		new Color32(0xF0, 0x90, 0x30, 0xFF), // 5 cam
		new Color32(0x50, 0xC0, 0xF0, 0xFF), // 6 xanh trời
		new Color32(0xA0, 0xD0, 0x40, 0xFF), // 7 xanh lá mạ
		new Color32(0x9A, 0xA0, 0xB8, 0xFF), // 8 xám
		new Color32(0xF0, 0x80, 0xB0, 0xFF), // 9 hồng
		new Color32(0xA0, 0x70, 0x50, 0xFF), // 10 nâu
		new Color32(0x4A, 0x4A, 0x5A, 0xFF), // 11 đen
		new Color32(0x30, 0xD0, 0x90, 0xFF), // 12 ngọc
		new Color32(0x10, 0xA0, 0x20, 0xFF), // 13 xanh lá
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
