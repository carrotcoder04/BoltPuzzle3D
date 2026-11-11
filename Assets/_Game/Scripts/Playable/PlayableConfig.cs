using UnityEngine;

/// <summary>
/// Cấu hình bản Playable Ads. Bật bằng Scripting Define Symbol PLAYABLE_BUILD
/// (công cụ Tools ▸ BoltPuzzle3D ▸ Build Optimizer tự bật/tắt).
/// </summary>
public static class PlayableConfig
{
#if PLAYABLE_BUILD
	public static bool IsPlayable => true;
#else
	public static bool IsPlayable => false;
#endif
	/// <summary>Số lần tháo ốc tối đa trước khi hiện Endcard (0 = chỉ hiện khi thắng/thua).</summary>
	public const int MaxScrewTaps = 15;

	// TODO: thay bằng link store thật
	public const string AndroidStoreUrl = "https://play.google.com/store/apps/details?id=com.example.boltpuzzle3d";
	public const string IosStoreUrl = "https://apps.apple.com/app/id0000000000";

	public static string StoreUrl => Application.platform == RuntimePlatform.IPhonePlayer ? IosStoreUrl : AndroidStoreUrl;
}
