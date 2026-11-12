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

	public static void Setup()
	{
		var cams = new List<Camera>(Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
		cams.RemoveAll(c => c == null || c.targetTexture != null);
		if (cams.Count < 2) return;
		cams.Sort((a, b) => a.depth.CompareTo(b.depth));
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
