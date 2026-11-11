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
