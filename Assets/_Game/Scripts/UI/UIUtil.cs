using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Tiện ích tìm node UI theo tên/đường dẫn khi các field gốc không có tham chiếu.</summary>
public static class UIUtil
{
	public static Transform FindDeep(this Transform root, string name)
	{
		if (root == null) return null;
		if (name.Contains("/")) return root.Find(name);
		if (root.name == name) return root;
		foreach (Transform c in root)
		{
			var r = FindDeep(c, name);
			if (r != null) return r;
		}
		return null;
	}

	public static T FindComp<T>(this Transform root, string name) where T : Component
	{
		var t = FindDeep(root, name);
		return t != null ? t.GetComponent<T>() : null;
	}

	public static TMP_Text FindText(this Transform root, string name)
	{
		var t = FindDeep(root, name);
		return t != null ? t.GetComponentInChildren<TMP_Text>(true) : null;
	}

	public static Button BindButton(this Transform root, string name, Action onClick)
	{
		var b = FindComp<Button>(root, name);
		if (b != null) b.onClick.AddListener(() => onClick());
		return b;
	}

	public static void SetActive(this Transform root, string name, bool active)
	{
		var t = FindDeep(root, name);
		if (t != null) t.gameObject.SetActive(active);
	}

	/// <summary>Số gọn: 999 → "999", 12 345 → "12.3K", 111 910 → "111K", 2 500 000 → "2.5M".</summary>
	public static string ShortNumber(long v)
	{
		var ci = System.Globalization.CultureInfo.InvariantCulture;
		long a = Math.Abs(v);
		if (a < 10000) return v.ToString(ci);
		if (a < 100000) return (Math.Floor(v / 100.0) / 10.0).ToString("0.#", ci) + "K";
		if (a < 1000000) return (v / 1000).ToString(ci) + "K";
		if (a < 100000000) return (Math.Floor(v / 100000.0) / 10.0).ToString("0.#", ci) + "M";
		return (v / 1000000).ToString(ci) + "M";
	}

	/// <summary>Thời gian gọn: "2d 7h", "3h 05m", "12:34".</summary>
	public static string ShortDuration(TimeSpan t)
	{
		if (t < TimeSpan.Zero) t = TimeSpan.Zero;
		if (t.TotalDays >= 1) return $"{(int)t.TotalDays}d {t.Hours}h";
		if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes:00}m";
		return $"{t.Minutes:00}:{t.Seconds:00}";
	}

	public static void SetText(TMP_Text t, string s)
	{
		if (t != null) t.text = s;
	}

	/// <summary>Lắc ngang báo không đủ điều kiện (ví dụ thiếu coin).</summary>
	public static void Deny(Component c)
	{
		if (c == null) return;
		c.transform.DOKill(true);
		c.transform.DOShakePosition(0.3f, new Vector3(12f, 0f, 0f), 20).SetUpdate(true);
		if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.ScrewUnmoveable);
	}
}
