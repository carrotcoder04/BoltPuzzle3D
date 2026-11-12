using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Thông báo nổi ngắn "Đã nhận: ..." sau khi mua gói / mở rương, để người chơi thấy rõ vật phẩm vừa vào kho.</summary>
public static class RewardToast
{
	static GameObject _current;

	public static string BoosterName(BoosterType t)
	{
		switch (t)
		{
			case BoosterType.Drill: return "Drill";
			case BoosterType.Hammer: return "Hammer";
			case BoosterType.Broom: return "Broom";
			default: return "Extra Box";
		}
	}

	public static string Describe(int coins, int[] boosters, int unlimitedMinutes, bool removeAds, BoosterType? extra = null, int extraCount = 0)
	{
		var parts = new List<string>();
		if (coins > 0) parts.Add("+" + coins.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " coins");
		if (boosters != null)
			for (int i = 0; i < boosters.Length; i++)
				if (boosters[i] > 0) parts.Add("+" + boosters[i] + " " + BoosterName((BoosterType)i));
		if (extra.HasValue && extraCount > 0) parts.Add("+" + extraCount + " " + BoosterName(extra.Value));
		if (unlimitedMinutes > 0)
		{
			string d = unlimitedMinutes >= 60 ? (unlimitedMinutes / 60) + "h" : unlimitedMinutes + "m";
			parts.Add(d + " unlimited lives");
		}
		if (removeAds) parts.Add("No ads");
		return parts.Count == 0 ? "" : "Received!\n" + string.Join("  •  ", parts);
	}

	public static void Show(string text)
	{
		if (string.IsNullOrEmpty(text)) return;
		if (_current != null) Object.Destroy(_current);

		var root = new GameObject("RewardToast", typeof(RectTransform));
		Object.DontDestroyOnLoad(root);
		_current = root;
		var canvas = root.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 5000;
		var scaler = root.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1080, 1920);
		scaler.matchWidthOrHeight = 0.5f;

		var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
		var prt = (RectTransform)panel.transform;
		prt.SetParent(root.transform, false);
		prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.62f);
		prt.sizeDelta = new Vector2(860, 190);
		var img = panel.GetComponent<Image>();
		img.color = new Color(0.08f, 0.1f, 0.22f, 0.9f);
		img.raycastTarget = false;

		var label = new GameObject("Text", typeof(RectTransform));
		var lrt = (RectTransform)label.transform;
		lrt.SetParent(prt, false);
		lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
		lrt.offsetMin = new Vector2(30, 16); lrt.offsetMax = new Vector2(-30, -16);
		var tmp = label.AddComponent<TextMeshProUGUI>();
		if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
		tmp.text = text;
		tmp.alignment = TextAlignmentOptions.Center;
		tmp.enableAutoSizing = true;
		tmp.fontSizeMin = 26; tmp.fontSizeMax = 46;
		tmp.color = Color.white;
		tmp.raycastTarget = false;

		var cg = panel.GetComponent<CanvasGroup>();
		cg.blocksRaycasts = false;
		cg.alpha = 0f;
		prt.localScale = Vector3.one * 0.7f;
		var seq = DOTween.Sequence().SetUpdate(true).SetLink(root);
		seq.Append(cg.DOFade(1f, 0.2f));
		seq.Join(prt.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
		seq.AppendInterval(2.2f);
		seq.Append(cg.DOFade(0f, 0.35f));
		seq.Join(prt.DOAnchorPosY(60f, 0.35f).SetRelative());
		seq.OnComplete(() => { if (root != null) Object.Destroy(root); });
	}
}
