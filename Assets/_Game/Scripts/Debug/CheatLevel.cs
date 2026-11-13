using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cheat chuyển level để chụp ảnh (chỉ bật trong Editor, Development Build, hoặc bản web mở với ?cheat=1).
/// Hiện trên màn Game: [◀] Level N/M [▶] [Go] [UI] [Shot] [All].
/// Phím tắt: ← / → đổi level, H ẩn/hiện UI, K chụp ảnh, Shift+K chụp toàn bộ level.
/// Ảnh lưu ở &lt;project&gt;/Screenshots/Levels (Editor/Standalone).
/// </summary>
public class CheatLevel : MonoBehaviour
{
	static CheatLevel _inst;
	static bool _hideUi;
	static int _autoTo;           // >0: đang tự chụp tới level này
	const float AutoDelay = 2.5f; // chờ mô hình/hộp vào xong rồi mới chụp

	string _input = "";
	bool _capturing;
	string _toast;
	float _toastUntil;
	readonly List<Canvas> _hidden = new List<Canvas>();

	public static bool Enabled
	{
		get
		{
			if (Application.isEditor || Debug.isDebugBuild) return true;
			var url = Application.absoluteURL;
			return !string.IsNullOrEmpty(url) && url.Contains("cheat=1");
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	static void Init()
	{
		if (!Enabled || _inst != null) return;
		var go = new GameObject("[CheatLevel]");
		DontDestroyOnLoad(go);
		_inst = go.AddComponent<CheatLevel>();
		SceneManager.sceneLoaded += (s, m) => { if (_inst != null) _inst.OnSceneLoaded(); };
		_inst.OnSceneLoaded();
	}

	static int LevelCount => GameConfig.Instance != null && GameConfig.Instance.Levels != null ? GameConfig.Instance.Levels.Count : 1;
	static int Current => UserDataManager.Instance != null ? UserDataManager.Instance.CurrentLevel : 1;
	static bool InGame => GameController.Instance != null;

	void OnSceneLoaded()
	{
		_hidden.Clear();
		StopAllCoroutines();
		StartCoroutine(AfterLoad());
	}

	IEnumerator AfterLoad()
	{
		yield return null;
		if (_hideUi) SetUiHidden(true);
		if (_autoTo > 0 && InGame)
		{
			yield return new WaitForSecondsRealtime(AutoDelay);
			yield return Capture();
			if (Current < _autoTo) GoTo(Current + 1);
			else { _autoTo = 0; Toast("Captured all levels"); }
		}
	}

	public static void GoTo(int level)
	{
		var ud = UserDataManager.Instance;
		if (ud == null) return;
		level = Mathf.Clamp(level, 1, Mathf.Max(1, LevelCount));
		ud.Data.CurrentLevel = level;
		ud.Data.LevelAttempt = 0;
		ud.Save();
		PlayerPrefs.SetInt(TutorialLevel1.DoneKey, 1); // không hiện tutorial khi đang cheat
		Snui.DialogManager.CloseAll();
		Debug.Log("[CheatLevel] Go to level " + level);
		SceneManager.LoadScene(GameController.GameSceneName);
	}

	void SetUiHidden(bool hide)
	{
		if (hide)
		{
			foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
				// chỉ ẩn canvas có tương tác (HUD, nút, dialog); giữ canvas nền
				if (c.isRootCanvas && c.enabled && c.renderMode != RenderMode.WorldSpace && c.GetComponent<UnityEngine.UI.GraphicRaycaster>() != null) { c.enabled = false; _hidden.Add(c); }
		}
		else
		{
			foreach (var c in _hidden) if (c != null) c.enabled = true;
			_hidden.Clear();
		}
	}

	void ToggleUi()
	{
		_hideUi = !_hideUi;
		SetUiHidden(_hideUi);
	}

	IEnumerator Capture()
	{
		_capturing = true;
		yield return new WaitForEndOfFrame();
		var tex = ScreenCapture.CaptureScreenshotAsTexture();
		_capturing = false;
		string name = $"Level_{Current:000}.png";
#if UNITY_WEBGL && !UNITY_EDITOR
		Toast("Web: dùng phím chụp màn hình của máy (" + name + ")");
#else
		var dir = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Screenshots", "Levels");
		Directory.CreateDirectory(dir);
		var path = Path.Combine(dir, name);
		File.WriteAllBytes(path, tex.EncodeToPNG());
		Debug.Log("[CheatLevel] Saved " + path);
		Toast("Saved " + name);
#endif
		Destroy(tex);
	}

	void Toast(string s) { _toast = s; _toastUntil = Time.realtimeSinceStartup + 2f; }

	void Update()
	{
		if (!InGame) return;
		if (Input.GetKeyDown(KeyCode.RightArrow)) GoTo(Current + 1);
		if (Input.GetKeyDown(KeyCode.LeftArrow)) GoTo(Current - 1);
		if (Input.GetKeyDown(KeyCode.H)) ToggleUi();
		if (Input.GetKeyDown(KeyCode.K))
		{
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) StartAll();
			else StartCoroutine(Capture());
		}
		if (Input.GetKeyDown(KeyCode.Escape) && _autoTo > 0) { _autoTo = 0; Toast("Auto capture stopped"); }
	}

	void StartAll()
	{
		_autoTo = LevelCount;
		if (!_hideUi) ToggleUi();
		GoTo(1);
	}

	void OnGUI()
	{
		if (_capturing || !InGame) return;
		// tránh tai thỏ / vùng camera: đặt panel trong Screen.safeArea, co lại nếu màn hẹp
		var safe = Screen.safeArea;
		float s = Mathf.Max(1f, Screen.height / 1000f);
		s = Mathf.Min(s, (safe.width - 12f) / 460f);
		var m = GUI.matrix;
		GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
		float top = (Screen.height - safe.yMax) / s;   // GUI tính y từ trên xuống
		float cx = (safe.x + safe.width * 0.5f) / s;
		var r = new Rect(cx - 230f, top + 6f, 460f, 72f);
		GUI.Box(r, GUIContent.none);
		var big = new GUIStyle(GUI.skin.button) { fontSize = 18 };
		var lbl = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
		float x = r.x + 6f, y = r.y + 6f, h = 30f;
		if (GUI.Button(new Rect(x, y, 34f, h), "◀", big)) GoTo(Current - 1);
		GUI.Label(new Rect(x + 34f, y, 110f, h), $"Lv {Current}/{LevelCount}", lbl);
		if (GUI.Button(new Rect(x + 144f, y, 34f, h), "▶", big)) GoTo(Current + 1);
		_input = GUI.TextField(new Rect(x + 186f, y, 56f, h), _input, 4, new GUIStyle(GUI.skin.textField) { fontSize = 18 });
		if (GUI.Button(new Rect(x + 246f, y, 44f, h), "Go", big) && int.TryParse(_input, out var lv)) GoTo(lv);
		if (GUI.Button(new Rect(x + 296f, y, 50f, h), _hideUi ? "UI+" : "UI-", big)) ToggleUi();
		if (GUI.Button(new Rect(x + 350f, y, 48f, h), "Shot", big)) StartCoroutine(Capture());
		if (GUI.Button(new Rect(x + 402f, y, 46f, h), _autoTo > 0 ? "Stop" : "All", big))
		{
			if (_autoTo > 0) _autoTo = 0; else StartAll();
		}
		string hint = _autoTo > 0 ? $"Auto capture… {Current}/{_autoTo} (Esc to stop)"
			: Time.realtimeSinceStartup < _toastUntil ? _toast : "←/→ level · H UI · K shot · Shift+K all";
		GUI.Label(new Rect(r.x, y + h + 2f, r.width, 26f), hint, new GUIStyle(lbl) { fontSize = 14 });
		GUI.matrix = m;
	}
}
