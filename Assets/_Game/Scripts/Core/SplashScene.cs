using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Màn hình Splash: chạy thanh loading, hiện icon theo nền tảng, nạp HomeScene bất đồng bộ.
/// Các phần tử được tìm theo tên trong hierarchy của SplashPanel.
/// </summary>
public class SplashScene : MonoBehaviour
{
	public const string HomeSceneName = "HomeScene";
	const float MinDuration = 1.5f;
	const string TermsUrl = "https://example.com/terms";
	const string PrivacyUrl = "https://example.com/privacy";

	Image _bar;

	void Awake()
	{
		var icAndroid = transform.Find("Icon/IconAndroid");
		var icIos = transform.Find("Icon/IconIOS");
#if UNITY_IOS
		bool ios = true;
#else
		bool ios = false;
#endif
		if (icAndroid != null) icAndroid.gameObject.SetActive(!ios);
		if (icIos != null) icIos.gameObject.SetActive(ios);

		var bar = transform.Find("Progress/Mask/Bar");
		if (bar != null)
		{
			_bar = bar.GetComponent<Image>();
			if (_bar != null)
			{
				_bar.type = Image.Type.Filled;
				_bar.fillMethod = Image.FillMethod.Horizontal;
				_bar.fillOrigin = 0;
				_bar.fillAmount = 0f;
			}
		}

		BindUrl("TermBtn", TermsUrl);
		BindUrl("PrivacyBtn", PrivacyUrl);
	}

	void BindUrl(string child, string url)
	{
		var t = transform.Find(child);
		var btn = t != null ? t.GetComponent<Button>() : null;
		if (btn != null) btn.onClick.AddListener(() => Application.OpenURL(url));
	}

	IEnumerator Start()
	{
		Managers.Ensure();
		AnalyticsManager.AppLaunch();
		float start = Time.unscaledTime;
		var op = SceneManager.LoadSceneAsync(PlayableConfig.IsPlayable ? "GameScene" : HomeSceneName);
		if (op == null)
		{
			Debug.LogError("[Splash] HomeScene chưa có trong Build Settings");
			yield break;
		}
		op.allowSceneActivation = false;
		float shown = 0f;
		while (true)
		{
			float load = Mathf.Clamp01(op.progress / 0.9f);
			float time = Mathf.Clamp01((Time.unscaledTime - start) / MinDuration);
			float target = Mathf.Min(load, time);
			shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 2f);
			if (_bar != null) _bar.fillAmount = shown;
			if (load >= 1f && time >= 1f && shown >= 0.999f) break;
			yield return null;
		}
		op.allowSceneActivation = true;
	}
}
