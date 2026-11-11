using UnityEngine;

/// <summary>
/// Rung phản hồi. Android: dùng Vibrator với thời lượng ngắn qua JNI; iOS/khác: Handheld.Vibrate.
/// Có giới hạn tần suất để tránh rung liên tục.
/// </summary>
public class VibrationManager : MonoBehaviour
{
	public static VibrationManager Instance { get; private set; }

	const float MinInterval = 0.05f;
	float _last;

#if UNITY_ANDROID && !UNITY_EDITOR
	AndroidJavaObject _vibrator;
#endif

	public bool Enabled => UserDataManager.Instance == null || UserDataManager.Instance.VibrationOn;

	public void Init()
	{
		Instance = this;
#if UNITY_ANDROID && !UNITY_EDITOR
		try
		{
			using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
				_vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
		}
		catch (System.Exception e) { Debug.LogWarning("[Vibration] " + e.Message); }
#endif
	}

	public void Light() => Vibrate(15);
	public void Medium() => Vibrate(35);
	public void Heavy() => Vibrate(70);

	public void Vibrate(long milliseconds)
	{
		if (!Enabled || Time.unscaledTime - _last < MinInterval) return;
		_last = Time.unscaledTime;
#if UNITY_ANDROID && !UNITY_EDITOR
		if (_vibrator != null)
		{
			try { _vibrator.Call("vibrate", milliseconds); } catch { }
			return;
		}
#endif
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
		Handheld.Vibrate();
#endif
	}
}
