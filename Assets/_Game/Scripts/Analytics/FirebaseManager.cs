using System.Collections.Generic;
using UnityEngine;
#if FIREBASE_ENABLED
using Firebase;
using Firebase.Analytics;
#endif

/// <summary>
/// Adapter Firebase. Chỉ gọi SDK thật khi có Scripting Define Symbol FIREBASE_ENABLED
/// (sau khi import Firebase Unity SDK + google-services.json / GoogleService-Info.plist).
/// Không có define → không phụ thuộc native, dùng được cho Editor / WebGL / Playable.
/// </summary>
public class FirebaseManager : MonoBehaviour
{
	public static FirebaseManager Instance { get; private set; }
	public static bool IsReady { get; private set; }

	public void Init()
	{
		Instance = this;
#if FIREBASE_ENABLED
		FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(t =>
		{
			IsReady = t.Result == DependencyStatus.Available;
			if (IsReady) FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
			else Debug.LogWarning("[Firebase] Dependency: " + t.Result);
		});
#else
		IsReady = false;
#endif
	}

	public static void LogEvent(string name, Dictionary<string, object> p)
	{
#if FIREBASE_ENABLED
		if (!IsReady) return;
		if (p == null || p.Count == 0) { FirebaseAnalytics.LogEvent(name); return; }
		var arr = new Parameter[p.Count];
		int i = 0;
		foreach (var kv in p)
		{
			var v = kv.Value;
			if (v is int || v is long) arr[i++] = new Parameter(kv.Key, System.Convert.ToInt64(v));
			else if (v is float || v is double) arr[i++] = new Parameter(kv.Key, System.Convert.ToDouble(v));
			else arr[i++] = new Parameter(kv.Key, v != null ? v.ToString() : "");
		}
		FirebaseAnalytics.LogEvent(name, arr);
#endif
	}

	public static void SetUserProperty(string name, string value)
	{
#if FIREBASE_ENABLED
		if (IsReady) FirebaseAnalytics.SetUserProperty(name, value);
#endif
	}
}
