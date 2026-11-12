#if UNITY_EDITOR
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Sinh lại file .sln/.csproj cho IDE (VS Code, Visual Studio, Rider).
/// Chạy tự động một lần mỗi phiên Editor, hoặc qua menu Tools/BoltPuzzle3D/Regenerate Project Files.
/// </summary>
static class ProjectFilesGenerator
{
	const string SessionKey = "BoltPuzzle3D.ProjectFilesSynced";

	[InitializeOnLoadMethod]
	static void AutoSync()
	{
		if (SessionState.GetBool(SessionKey, false)) return;
		SessionState.SetBool(SessionKey, true);
		EditorApplication.delayCall += Regenerate;
	}

	[MenuItem("Tools/BoltPuzzle3D/Regenerate Project Files")]
	static void Regenerate()
	{
		var editor = CodeEditor.CurrentEditor;
		if (editor == null)
		{
			Debug.LogWarning("[ProjectFiles] Chưa chọn External Script Editor (Preferences > External Tools).");
			return;
		}
		editor.SyncAll();
		Debug.Log("[ProjectFiles] Đã sinh lại .sln/.csproj bằng " + editor.GetType().Name);
	}
}
#endif
