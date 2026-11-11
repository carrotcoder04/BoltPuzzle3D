using UnityEngine;

/// <summary>
/// Cầu nối CTA cho Playable Ads. Mặc định mở StoreUrl; với mạng quảng cáo yêu cầu
/// mraid.open / dapi / FbPlayableAd.onCTAClick, thêm file .jslib định nghĩa hàm PlayableCTA rồi bật define PLAYABLE_JSLIB.
/// </summary>
public static class PlayableBridge
{
#if UNITY_WEBGL && PLAYABLE_JSLIB && !UNITY_EDITOR
	[System.Runtime.InteropServices.DllImport("__Internal")] static extern void PlayableCTA(string url);
#endif

	public static void OpenStore()
	{
		AnalyticsManager.Log("playable_cta");
#if UNITY_WEBGL && PLAYABLE_JSLIB && !UNITY_EDITOR
		PlayableCTA(PlayableConfig.StoreUrl);
#else
		Application.OpenURL(PlayableConfig.StoreUrl);
#endif
	}
}
