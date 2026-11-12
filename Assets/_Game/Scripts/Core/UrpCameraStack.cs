using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// URP không hỗ trợ nhiều camera "Depth only" chồng nhau như Built-in.
/// Khi nạp scene: camera có depth thấp nhất làm Base, các camera còn lại thành Overlay trong stack theo thứ tự depth.
/// </summary>
public static class UrpCameraStack
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void Boot()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { Setup(); }

	static bool IsUi(Camera c) => c.name.Contains("UI") || ((c.cullingMask & (1 << 5)) != 0 && (c.cullingMask & ~(1 << 5)) == 0);

	public static void Setup()
	{
		var cams = new List<Camera>(Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
		cams.RemoveAll(c => c == null || c.targetTexture != null);
		if (cams.Count < 2) return;
		// Base = camera depth thấp nhất; overlay theo depth, nhưng camera vẽ UI (layer 5) luôn ở cuối
		// để dialog che được ốc đang ở lớp Top (TopCamera depth cao hơn UICamera trong scene gốc).
		cams.Sort((a, b) => a.depth.CompareTo(b.depth));
		var baseCam = cams[0];
		cams.RemoveAt(0);
		cams.Sort((a, b) =>
		{
			int ua = IsUi(a) ? 1 : 0, ub = IsUi(b) ? 1 : 0;
			return ua != ub ? ua.CompareTo(ub) : a.depth.CompareTo(b.depth);
		});
		cams.Insert(0, baseCam);
		var baseData = cams[0].GetUniversalAdditionalCameraData();
		if (baseData == null) return;
		baseData.renderType = CameraRenderType.Base;
		baseData.cameraStack.Clear();
		for (int i = 1; i < cams.Count; i++)
		{
			var d = cams[i].GetUniversalAdditionalCameraData();
			if (d == null) continue;
			d.renderType = CameraRenderType.Overlay;
			baseData.cameraStack.Add(cams[i]);
		}
	}
}
