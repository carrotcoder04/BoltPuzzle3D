using System;
using Snui;
using UnityEngine;

/// <summary>Bảng tạm dừng: Tiếp tục, Về Home (mất 1 tim), bật/tắt Nhạc/Âm thanh/Rung/Chế độ mù màu.</summary>
public class PauseDialogContent : DialogContent
{
	protected override void Awake()
	{
		base.Awake();
		transform.BindButton("ContinueBtn", () => Close(0));
		transform.BindButton("HomeBtn", () =>
		{
			var gc = GameController.Instance;
			if (gc != null) gc.QuitLevel();
			else Close(1);
		});
		Toggle("MusicBtn", () => Managers.UserData.MusicOn, v => Managers.UserData.MusicOn = v);
		Toggle("SoundBtn", () => Managers.UserData.SoundOn, v => Managers.UserData.SoundOn = v);
		Toggle("VibrateBtn", () => Managers.UserData.VibrationOn, v => Managers.UserData.VibrationOn = v);
		transform.SetActive("ColorBlindnessSwitchBtn", false);
		transform.SetActive("SettingBtn", false);
	}

	void Toggle(string btnName, Func<bool> get, Action<bool> set)
	{
		var btn = transform.FindDeep(btnName);
		if (btn == null || Managers.UserData == null) return;
		var on = btn.Find("Content/On");
		var off = btn.Find("Content/Off");
		Action Refresh = () =>
		{
			bool v = get();
			if (on != null) on.gameObject.SetActive(v);
			if (off != null) off.gameObject.SetActive(!v);
		};
		Refresh();
		btn.BindButton(btnName, () => { set(!get()); Refresh(); });
	}
}
