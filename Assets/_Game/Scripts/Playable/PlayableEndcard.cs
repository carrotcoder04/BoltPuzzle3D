using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Endcard tối giản tạo bằng code (không cần prefab): nền tối + chữ + nút "Download Now".</summary>
public class PlayableEndcard : MonoBehaviour
{
	public static void Show(string title)
	{
		if (FindAnyObjectByType<PlayableEndcard>() != null) return;
		var go = new GameObject("PlayableEndcard", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PlayableEndcard));
		var canvas = go.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 1000;
		var scaler = go.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1080, 1920);
		scaler.matchWidthOrHeight = 0.5f;

		var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		var bg = Child(go.transform, "Bg", Vector2.zero, Vector2.one, Vector2.zero);
		var bgImg = bg.gameObject.AddComponent<Image>();
		bgImg.color = new Color(0f, 0f, 0f, 0.75f);
		var bgBtn = bg.gameObject.AddComponent<Button>();
		bgBtn.onClick.AddListener(PlayableBridge.OpenStore);

		var t = Child(go.transform, "Title", new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.75f), Vector2.zero);
		var txt = t.gameObject.AddComponent<Text>();
		txt.font = font; txt.text = title; txt.fontSize = 80; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white;

		var b = Child(go.transform, "DownloadBtn", new Vector2(0.2f, 0.35f), new Vector2(0.8f, 0.45f), Vector2.zero);
		var img = b.gameObject.AddComponent<Image>();
		img.color = new Color32(0x30, 0xC0, 0x40, 0xFF);
		var btn = b.gameObject.AddComponent<Button>();
		btn.onClick.AddListener(PlayableBridge.OpenStore);
		var l = Child(b, "Label", Vector2.zero, Vector2.one, Vector2.zero);
		var lt = l.gameObject.AddComponent<Text>();
		lt.font = font; lt.text = "DOWNLOAD NOW"; lt.fontSize = 64; lt.alignment = TextAnchor.MiddleCenter; lt.color = Color.white;
		b.localScale = Vector3.one;
		LeanPulse(b);

		if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
			new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
	}

	static RectTransform Child(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offset)
	{
		var go = new GameObject(name, typeof(RectTransform));
		var rt = (RectTransform)go.transform;
		rt.SetParent(parent, false);
		rt.anchorMin = min; rt.anchorMax = max;
		rt.offsetMin = offset; rt.offsetMax = offset;
		return rt;
	}

	static void LeanPulse(Transform t)
	{
		t.DOScale(1.08f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetLink(t.gameObject);
	}
}
