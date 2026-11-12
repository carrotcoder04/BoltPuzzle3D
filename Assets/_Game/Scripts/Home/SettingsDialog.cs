using System;
using Snui;
using UnityEngine;

/// <summary>Cài đặt: Nhạc, Âm thanh, Rung, Chế độ mù màu, Khôi phục giao dịch. Ẩn các mục online.</summary>
public class SettingsDialog : MonoBehaviour
{
	void Awake()
	{
		var content = transform.FindDeep("Content") ?? transform;
		var keep = new[] { "MusicBtn", "SoundBtn", "VibrationBtn", "BlindModeBtn", "RestoreBtn" };
		foreach (Transform c in content)
			if (c.GetComponent<UnityEngine.UI.Button>() != null && Array.IndexOf(keep, c.name) < 0) c.gameObject.SetActive(false);

		// Chữ gốc được bản địa hoá lúc chạy nên prefab rip ra để trống: gán lại nhãn tiếng Anh.
		SetLabel(content, "MusicBtn", "Music");
		SetLabel(content, "SoundBtn", "Sound");
		SetLabel(content, "VibrationBtn", "Vibration");
		SetLabel(content, "BlindModeBtn", "Color Blind Mode");
		SetLabel(content, "RestoreBtn", "Restore Purchases");

		var ud = UserDataManager.Instance;
		if (ud != null)
		{
			Toggle(content, "MusicBtn", () => ud.MusicOn, v => ud.MusicOn = v);
			Toggle(content, "SoundBtn", () => ud.SoundOn, v => ud.SoundOn = v);
			Toggle(content, "VibrationBtn", () => ud.VibrationOn, v => ud.VibrationOn = v);
			Toggle(content, "BlindModeBtn", () => ud.ColorBlindMode, v => ud.ColorBlindMode = v);
		}
		content.BindButton("RestoreBtn", () => { if (IapManager.Instance != null) IapManager.Instance.Restore(); });

		var dialog = GetComponent<Dialog>();
		foreach (var n in new[] { "CloseBtn", "CloseButton", "BackBtn", "BackButton" })
			transform.BindButton(n, () => { if (dialog != null) dialog.Close(); else Destroy(gameObject); });
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
