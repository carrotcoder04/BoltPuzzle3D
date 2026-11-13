using System;
using DG.Tweening;
using Snui;
using UnityEngine;

/// <summary>Cài đặt: Nhạc, Âm thanh, Rung, Chế độ mù màu. Ẩn các mục online.</summary>
public class SettingsDialog : MonoBehaviour
{
	void Awake()
	{
		var content = transform.FindDeep("Content") ?? transform;
		var keep = new[] { "MusicBtn", "SoundBtn", "VibrationBtn", "BlindModeBtn" };
		foreach (Transform c in content)
			if (c.GetComponent<UnityEngine.UI.Button>() != null && Array.IndexOf(keep, c.name) < 0) c.gameObject.SetActive(false);

		// Chữ gốc được bản địa hoá lúc chạy nên prefab rip ra để trống: gán lại nhãn tiếng Anh.
		SetLabel(content, "MusicBtn", "Music");
		SetLabel(content, "SoundBtn", "Sound");
		SetLabel(content, "VibrationBtn", "Vibration");
		content.SetActive("BlindModeBtn", false);
		content.SetActive("RestoreBtn", false);

		var ud = UserDataManager.Instance;
		if (ud != null)
		{
			Toggle(content, "MusicBtn", () => ud.MusicOn, v => ud.MusicOn = v);
			Toggle(content, "SoundBtn", () => ud.SoundOn, v => ud.SoundOn = v);
			Toggle(content, "VibrationBtn", () => ud.VibrationOn, v => ud.VibrationOn = v);
		}

		var dialog = GetComponent<Dialog>();
		SetupSlide(dialog);
		foreach (var n in new[] { "CloseBtn", "CloseButton", "BackBtn", "BackButton" })
			transform.BindButton(n, () => { if (dialog != null) dialog.Close(); else Destroy(gameObject); });
	}

	// ---------- hiệu ứng mở/đóng: trượt từ phải vào + mờ dần ----------

	RectTransform _page;
	CanvasGroup _pageCg;
	const float InTime = 0.32f, OutTime = 0.24f;

	void SetupSlide(Dialog dialog)
	{
		_page = transform.Find("SafeArea") as RectTransform;
		if (_page == null) return;
		_pageCg = _page.gameObject.GetOrAdd<CanvasGroup>();
		if (dialog != null) dialog.ExitAnimation = PlayExit;
	}

	void OnEnable()
	{
		if (_page == null) return;
		float w = ((RectTransform)transform).rect.width;
		if (w <= 0f) w = 750f;
		_page.DOKill();
		_pageCg.DOKill();
		_page.anchoredPosition = new Vector2(w * 0.35f, _page.anchoredPosition.y);
		_pageCg.alpha = 0f;
		_page.DOAnchorPosX(0f, InTime).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
		_pageCg.DOFade(1f, InTime * 0.8f).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
	}

	void PlayExit(Action done)
	{
		if (_page == null) { done(); return; }
		float w = ((RectTransform)transform).rect.width;
		if (w <= 0f) w = 750f;
		_pageCg.blocksRaycasts = false;
		_page.DOKill();
		_pageCg.DOKill();
		_page.DOAnchorPosX(w * 0.35f, OutTime).SetEase(Ease.InCubic).SetUpdate(true).SetLink(gameObject);
		_pageCg.DOFade(0f, OutTime).SetEase(Ease.InQuad).SetUpdate(true).SetLink(gameObject).OnComplete(() => done());
	}

	static void SetLabel(Transform root, string btn, string text)
	{
		var b = root.Find(btn);
		var t = b != null ? b.Find("Text") : null;
		var tmp = t != null ? t.GetComponent<TMPro.TMP_Text>() : null;
		if (tmp != null) tmp.text = text;
	}

	static void Toggle(Transform root, string name, Func<bool> get, Action<bool> set)
	{
		var btn = root.Find(name);
		if (btn == null) return;
		var on = btn.Find("ToggleOn");
		var off = btn.Find("ToggleOff");
		Action refresh = () =>
		{
			bool v = get();
			if (on != null) on.gameObject.SetActive(v);
			if (off != null) off.gameObject.SetActive(!v);
		};
		refresh();
		btn.BindButton(name, () => { set(!get()); refresh(); });
	}
}
