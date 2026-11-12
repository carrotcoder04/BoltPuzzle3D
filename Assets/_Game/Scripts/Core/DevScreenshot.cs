#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Chỉ chạy trong Editor: tự chụp Game view khi Play để rà UI.
/// Ảnh lưu ở thư mục Screenshots/ cạnh Assets (không commit).
/// </summary>
public class DevScreenshot : MonoBehaviour
{
	const string Dir = "Screenshots";
	const float Interval = 4f;
	const int MaxShots = 40;
	static int _index;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	static void Boot()
	{
		var go = new GameObject("[DevScreenshot]");
		DontDestroyOnLoad(go);
		go.hideFlags = HideFlags.HideAndDontSave;
		go.AddComponent<DevScreenshot>();
	}

	IEnumerator Start()
	{
		Directory.CreateDirectory(Dir);
		_index = 0;
		while (_index < MaxShots)
		{
			yield return new WaitForSecondsRealtime(_index == 0 ? 1.5f : Interval);
			string scene = SceneManager.GetActiveScene().name;
			ScreenCapture.CaptureScreenshot(Path.Combine(Dir, _index.ToString("00") + "_" + scene + ".png"));
			_index++;
		}
	}
}
#endif
