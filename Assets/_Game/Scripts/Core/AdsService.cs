using System;

/// <summary>
/// Điểm nối quảng cáo thưởng (rewarded). Hiện CHƯA tích hợp SDK (LevelPlay/AdMob):
/// IsRewardedReady = false nên UI tự ẩn các nút xem quảng cáo. Khi tích hợp, thay phần thân 2 hàm này.
/// </summary>
public static class AdsService
{
	public static bool IsRewardedReady => false;

	public static void ShowRewarded(string placement, Action<bool> onDone)
	{
		onDone?.Invoke(false);
	}
}
