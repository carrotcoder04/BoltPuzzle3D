using System.Collections;
using DG.Tweening;
using TMPro;
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
	const float MinDuration = 4f;
	// Các mốc tiến trình giả lập: (tiến trình, thời điểm 0..1). Thanh chạy nhanh rồi khựng lại vài nhịp như đang nạp dữ liệu thật.
	static readonly Vector2[] Stages =
	{
		new Vector2(0f, 0f), new Vector2(0.32f, 0.18f), new Vector2(0.38f, 0.34f),
		new Vector2(0.66f, 0.52f), new Vector2(0.72f, 0.70f), new Vector2(0.93f, 0.86f), new Vector2(1f, 1f)
	};
	static readonly string[] TipLines =
	{
		"Tip: Unscrew bolts into boxes of the same color",
		"Tip: Rotate the model to find hidden bolts",
		"Tip: Watch the holes, when they are full you lose!",
		"Tip: Use tools when you get stuck"
	};

	TMP_Text _tips;
	RectTransform _pattern;
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

		var tips = transform.Find("Tips");
		_tips = tips != null ? tips.GetComponent<TMP_Text>() : null;
		var pat = transform.Find("Progress/Mask/Pattern");
		_pattern = pat != null ? pat as RectTransform : null;
		Animate();

		BindUrl("TermBtn", TermsUrl);
		BindUrl("PrivacyBtn", PrivacyUrl);
	}

	/// <summary>Icon nhún nhảy, vân sọc trên thanh trượt liên tục cho màn chờ sinh động.</summary>
	void Animate()
	{
		var icon = transform.Find("Icon");
		if (icon != null)
		{
			var s0 = icon.localScale;
			icon.localScale = Vector3.zero;
			var seq = DOTween.Sequence();
			seq.Append(icon.DOScale(s0, 0.6f).SetEase(Ease.OutBack));
			seq.Append(icon.DOScale(s0 * 1.05f, 0.9f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo));
			seq.SetLink(gameObject);
			var rt = icon as RectTransform;
			if (rt != null)
				rt.DOAnchorPosY(rt.anchoredPosition.y + 18f, 1.4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(0.6f).SetLink(gameObject);
		}
		var progress = transform.Find("Progress");
		if (progress != null)
		{
			var s1 = progress.localScale;
			progress.localScale = new Vector3(0f, s1.y, s1.z);
			progress.DOScaleX(s1.x, 0.4f).SetDelay(0.3f).SetEase(Ease.OutBack).SetLink(gameObject);
		}
		if (_pattern != null)
		{
			var p0 = _pattern.anchoredPosition;
			_pattern.DOAnchorPosX(p0.x + 60f, 0.8f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart).SetLink(gameObject);
		}
	}

	static float StagedProgress(float t)
	{
		for (int i = 1; i < Stages.Length; i++)
		{
			if (t > Stages[i].y) continue;
			var a = Stages[i - 1];
			var b = Stages[i];
			float u = Mathf.InverseLerp(a.y, b.y, t);
			return Mathf.Lerp(a.x, b.x, Mathf.SmoothStep(0f, 1f, u));
		}
		return 1f;
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
			float elapsed = Time.unscaledTime - start;
			float time = Mathf.Clamp01(elapsed / MinDuration);
			float target = Mathf.Min(load, StagedProgress(time));
			shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 1.5f);
			if (_bar != null) _bar.fillAmount = shown;
			if (_tips != null)
			{
				int dots = (int)(elapsed * 3f) % 4;
				string tip = TipLines[(int)(elapsed / 2f) % TipLines.Length];
				_tips.text = tip + "\n<size=80%>Loading" + new string('.', dots) + "  " + Mathf.RoundToInt(shown * 100f) + "%</size>";
			}
			if (load >= 1f && time >= 1f && shown >= 0.999f) break;
			yield return null;
		}
		if (_bar != null) _bar.transform.parent.parent.DOPunchScale(Vector3.one * 0.06f, 0.25f, 6, 0.6f).SetLink(gameObject);
		yield return new WaitForSecondsRealtime(0.3f);
		op.allowSceneActivation = true;
	}
}
