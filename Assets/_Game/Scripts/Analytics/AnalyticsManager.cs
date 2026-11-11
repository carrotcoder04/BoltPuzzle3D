using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Điểm gửi tracking duy nhất của game. Editor/Playable: log Console (bật/tắt bằng LogToConsole).
/// Bản Full có FIREBASE_ENABLED: chuyển sang FirebaseManager.
/// </summary>
public class AnalyticsManager : MonoBehaviour
{
	public static bool LogToConsole = Application.isEditor || Debug.isDebugBuild;

	public static void Log(string evt, params object[] kv)
	{
		var p = new Dictionary<string, object>();
		for (int i = 0; i + 1 < kv.Length; i += 2) p[kv[i].ToString()] = kv[i + 1];
		if (LogToConsole)
		{
			var sb = new StringBuilder("[Analytics] ").Append(evt);
			foreach (var x in p) sb.Append(' ').Append(x.Key).Append('=').Append(x.Value);
			Debug.Log(sb.ToString());
		}
		FirebaseManager.LogEvent(evt, p);
	}

	static int Level => UserDataManager.Instance != null ? UserDataManager.Instance.CurrentLevel : 0;

	// ---- Game flow
	public static void AppLaunch() => Log("app_launch", "version", Application.version, "platform", Application.platform.ToString());

	// ---- Level
	public static void LevelStart(int level, string puzzle, int attempt) => Log("level_start", "level_id", level, "puzzle_name", puzzle, "attempt_count", attempt);
	public static void LevelWin(int level, float duration, int screws, int boosters) => Log("level_win", "level_id", level, "duration_sec", Mathf.RoundToInt(duration), "screws_cleared", screws, "boosters_used", boosters);
	public static void LevelFail(int level, string reason, int remaining) => Log("level_fail", "level_id", level, "reason", reason, "remaining_screws", remaining);
	public static void LevelRevive(int level, string method) => Log("level_revive", "level_id", level, "method", method);

	// ---- Economy
	public static void BoosterUse(BoosterType t) => Log("booster_use", "level_id", Level, "booster_type", BoosterName(t));
	public static void CurrencyEarn(string type, int amount, string source) => Log("currency_earn", "currency_type", type, "amount", amount, "source", source);
	public static void CurrencySpend(string type, int amount, string source) => Log("currency_spend", "currency_type", type, "amount", amount, "source", source);

	// ---- Monetization
	public static void IapClick(string productId, string placement) => Log("iap_click", "product_id", productId, "placement", placement);
	public static void IapSuccess(string productId, decimal price, string currency) => Log("iap_purchase_success", "product_id", productId, "price", (double)price, "currency", currency);
	public static void IapFail(string productId, string reason) => Log("iap_purchase_fail", "product_id", productId, "error_reason", reason);

	public static string BoosterName(BoosterType t)
	{
		switch (t)
		{
			case BoosterType.Drill: return "drill";
			case BoosterType.Hammer: return "hammer";
			case BoosterType.Broom: return "broom";
			default: return "add_box";
		}
	}
}
