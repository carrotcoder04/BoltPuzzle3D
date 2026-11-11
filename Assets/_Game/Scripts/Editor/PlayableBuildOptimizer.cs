using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Tools ▸ BoltPuzzle3D ▸ Build Optimizer: chuyển qua lại Full Mode ↔ Playable Mode.
/// Playable: giữ 1–3 màn, chỉ GameScene trong build, bật PLAYABLE_BUILD, nén texture/audio
/// bằng override riêng cho WebGL (không đụng tới thiết lập Android/iOS), stripping cao.
/// Full: khôi phục danh sách màn + scene + define từ file backup.
/// </summary>
public class PlayableBuildOptimizer : EditorWindow
{
	const string Define = "PLAYABLE_BUILD";
	const string BackupPath = "ProjectSettings/BoltPlayableBackup.json";
	const string ConfigPath = "Assets/_Game/Resources/GameConfig.asset";
	const string GameScene = "Assets/_Game/Scenes/GameScene.unity";
	const string WebGL = "WebGL";

	[Serializable]
	class Backup
	{
		public List<string> LevelGuids = new List<string>();
		public List<string> Scenes = new List<string>();
	}

	static readonly NamedBuildTarget[] Targets =
	{
		NamedBuildTarget.Android, NamedBuildTarget.iOS, NamedBuildTarget.WebGL, NamedBuildTarget.Standalone
	};

	string _levels = "Cat, C_Bird, B_Camel";
	int _maxTexture = 512;
	float _audioQuality = 0.4f;
	Vector2 _scroll;

	[MenuItem("Tools/BoltPuzzle3D/Build Optimizer")]
	static void Open() => GetWindow<PlayableBuildOptimizer>("Build Optimizer");

	static bool IsPlayableMode => HasDefine(NamedBuildTarget.WebGL) || HasDefine(NamedBuildTarget.Android);

	void OnGUI()
	{
		_scroll = EditorGUILayout.BeginScrollView(_scroll);
		EditorGUILayout.HelpBox("Chế độ hiện tại: " + (IsPlayableMode ? "PLAYABLE" : "FULL") +
			"\nNên commit git trước khi chuyển chế độ.", MessageType.Info);

		var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
		EditorGUILayout.LabelField("Số màn trong GameConfig", cfg != null ? cfg.Levels.Count.ToString() : "(không tìm thấy " + ConfigPath + ")");

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Playable Mode", EditorStyles.boldLabel);
		_levels = EditorGUILayout.TextField(new GUIContent("Màn giữ lại", "Tên prefab, cách nhau dấu phẩy"), _levels);
		_maxTexture = EditorGUILayout.IntPopup("Max texture (WebGL)", _maxTexture, new[] { "256", "512", "1024" }, new[] { 256, 512, 1024 });
		_audioQuality = EditorGUILayout.Slider("Chất lượng audio (WebGL)", _audioQuality, 0.1f, 1f);

		using (new EditorGUI.DisabledScope(IsPlayableMode))
			if (GUILayout.Button("Chuyển sang PLAYABLE MODE", GUILayout.Height(32))) SwitchToPlayable(_levels);
		if (GUILayout.Button("Nén Texture + Audio cho WebGL")) { CompressTextures(_maxTexture); CompressAudio(_audioQuality); }
		if (GUILayout.Button("Thiết lập stripping WebGL (High + Strip Engine Code)")) ApplyStripping();

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Full Mode", EditorStyles.boldLabel);
		using (new EditorGUI.DisabledScope(!IsPlayableMode && !File.Exists(BackupPath)))
			if (GUILayout.Button("Khôi phục FULL MODE", GUILayout.Height(32))) SwitchToFull();
		if (GUILayout.Button("Xoá override WebGL của Texture + Audio")) ClearWebGLOverrides();
		EditorGUILayout.EndScrollView();
	}

	// ------------------------------------------------------------ mode switch
	public static void SwitchToPlayable(string levelNames)
	{
		var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
		if (cfg == null) { Debug.LogError("[Optimizer] Không thấy " + ConfigPath); return; }

		var backup = new Backup();
		foreach (var go in cfg.Levels)
			if (go != null) backup.LevelGuids.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(go)));
		backup.Scenes = EditorBuildSettings.scenes.Select(s => s.path).ToList();
		if (!File.Exists(BackupPath)) File.WriteAllText(BackupPath, JsonUtility.ToJson(backup, true));

		var wanted = levelNames.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
		var keep = new List<GameObject>();
		foreach (var n in wanted)
		{
			var go = cfg.Levels.FirstOrDefault(l => l != null && l.name == n);
			if (go != null) keep.Add(go);
			else Debug.LogWarning("[Optimizer] Không có màn " + n);
		}
		if (keep.Count == 0) { Debug.LogError("[Optimizer] Không giữ được màn nào – huỷ."); return; }
		Undo.RecordObject(cfg, "Playable levels");
		cfg.Levels = keep;
		cfg.LoopFromLevel = 1;
		EditorUtility.SetDirty(cfg);

		EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GameScene, true) };
		foreach (var t in Targets) SetDefine(t, true);
		ApplyStripping();
		AssetDatabase.SaveAssets();
		Debug.Log($"[Optimizer] PLAYABLE MODE: {keep.Count} màn, chỉ GameScene, define {Define}. Backup: {BackupPath}");
	}

	public static void SwitchToFull()
	{
		var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
		if (File.Exists(BackupPath) && cfg != null)
		{
			var b = JsonUtility.FromJson<Backup>(File.ReadAllText(BackupPath));
			Undo.RecordObject(cfg, "Full levels");
			cfg.Levels = b.LevelGuids.Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).Where(g => g != null).ToList();
			cfg.LoopFromLevel = 11;
			EditorUtility.SetDirty(cfg);
			if (b.Scenes.Count > 0) EditorBuildSettings.scenes = b.Scenes.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
			File.Delete(BackupPath);
		}
		foreach (var t in Targets) SetDefine(t, false);
		AssetDatabase.SaveAssets();
		Debug.Log("[Optimizer] FULL MODE: " + (cfg != null ? cfg.Levels.Count : 0) + " màn.");
	}

	// ------------------------------------------------------------ defines
	static bool HasDefine(NamedBuildTarget t) => PlayerSettings.GetScriptingDefineSymbols(t).Split(';').Contains(Define);

	static void SetDefine(NamedBuildTarget t, bool on)
	{
		var list = PlayerSettings.GetScriptingDefineSymbols(t).Split(';').Where(s => s.Length > 0 && s != Define).ToList();
		if (on) list.Add(Define);
		PlayerSettings.SetScriptingDefineSymbols(t, string.Join(";", list));
	}

	static void ApplyStripping()
	{
		PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
		PlayerSettings.stripEngineCode = true;
	}

	// ------------------------------------------------------------ assets (WebGL override only)
	static IEnumerable<string> FindAssets(string filter) =>
		AssetDatabase.FindAssets(filter, new[] { "Assets/_Game" }).Select(AssetDatabase.GUIDToAssetPath).Distinct();

	public static void CompressTextures(int maxSize)
	{
		int n = 0;
		try
		{
			AssetDatabase.StartAssetEditing();
			foreach (var path in FindAssets("t:Texture2D"))
			{
				if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
				var s = ti.GetPlatformTextureSettings(WebGL);
				s.overridden = true;
				s.maxTextureSize = Mathf.Min(maxSize, ti.maxTextureSize);
				s.crunchedCompression = true;
				s.compressionQuality = 50;
				ti.SetPlatformTextureSettings(s);
				ti.SaveAndReimport();
				n++;
			}
		}
		finally { AssetDatabase.StopAssetEditing(); }
		Debug.Log($"[Optimizer] Đã đặt override WebGL cho {n} texture (max {maxSize}, crunch 50).");
	}

	public static void CompressAudio(float quality)
	{
		int n = 0;
		try
		{
			AssetDatabase.StartAssetEditing();
			foreach (var path in FindAssets("t:AudioClip"))
			{
				if (!(AssetImporter.GetAtPath(path) is AudioImporter ai)) continue;
				var s = ai.defaultSampleSettings;
				s.loadType = AudioClipLoadType.DecompressOnLoad;
				s.compressionFormat = AudioCompressionFormat.Vorbis;
				s.quality = quality;
				s.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
				ai.SetOverrideSampleSettings(WebGL, s);
				ai.SaveAndReimport();
				n++;
			}
		}
		finally { AssetDatabase.StopAssetEditing(); }
		Debug.Log($"[Optimizer] Đã đặt override WebGL cho {n} audio clip (Vorbis q={quality:0.00}).");
	}

	public static void ClearWebGLOverrides()
	{
		try
		{
			AssetDatabase.StartAssetEditing();
			foreach (var path in FindAssets("t:Texture2D"))
				if (AssetImporter.GetAtPath(path) is TextureImporter ti)
				{
					var s = ti.GetPlatformTextureSettings(WebGL);
					if (!s.overridden) continue;
					s.overridden = false;
					ti.SetPlatformTextureSettings(s);
					ti.SaveAndReimport();
				}
			foreach (var path in FindAssets("t:AudioClip"))
				if (AssetImporter.GetAtPath(path) is AudioImporter ai && ai.ContainsSampleSettingsOverride(WebGL))
				{
					ai.ClearSampleSettingOverride(WebGL);
					ai.SaveAndReimport();
				}
		}
		finally { AssetDatabase.StopAssetEditing(); }
		Debug.Log("[Optimizer] Đã xoá override WebGL.");
	}
}
