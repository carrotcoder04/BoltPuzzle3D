using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snui
{
	/// <summary>
	/// Quản lý ngăn xếp Dialog: tạo dialog từ prefab dưới Canvas cao nhất trong scene,
	/// xử lý nút Back (Android) cho dialog trên cùng.
	/// </summary>
	public static class DialogManager
	{
		static readonly List<Dialog> _stack = new List<Dialog>();

		public static int Count => _stack.Count;
		public static Dialog Top => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

		/// <summary>Root mặc định để đặt dialog. Gán từ scene (ví dụ UiCanvas/DialogRoot).</summary>
		public static Transform DefaultRoot;

		public static T Show<T>(T prefab, Transform root = null, Action<Dialog, int> onClosed = null) where T : Component
		{
			if (prefab == null) return null;
			root = root != null ? root : FindRoot();
			var inst = UnityEngine.Object.Instantiate(prefab, root, false);
			var rt = inst.transform as RectTransform;
			if (rt != null)
			{
				rt.anchorMin = Vector2.zero;
				rt.anchorMax = Vector2.one;
				rt.offsetMin = Vector2.zero;
				rt.offsetMax = Vector2.zero;
			}
			inst.transform.SetAsLastSibling();
			var dialog = inst.GetComponent<Dialog>();
			if (dialog != null && onClosed != null) dialog.Closed += onClosed;
			return inst;
		}

		public static GameObject Show(GameObject prefab, Transform root = null, Action<Dialog, int> onClosed = null)
		{
			var t = Show(prefab != null ? prefab.transform : null, root, onClosed);
			return t != null ? t.gameObject : null;
		}

		internal static void Register(Dialog d)
		{
			if (!_stack.Contains(d)) _stack.Add(d);
		}

		internal static void Unregister(Dialog d) => _stack.Remove(d);

		/// <summary>Trả về true nếu đã có dialog xử lý nút Back.</summary>
		public static bool HandleBack()
		{
			var top = Top;
			if (top == null) return false;
			top.OnBackPressed();
			return true;
		}

		public static void CloseAll()
		{
			for (int i = _stack.Count - 1; i >= 0; i--)
				if (_stack[i] != null) _stack[i].Close();
		}

		static Transform FindRoot()
		{
			if (DefaultRoot != null) return DefaultRoot;
			Canvas best = null;
			foreach (var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
			{
				if (!c.isRootCanvas || !c.gameObject.activeInHierarchy) continue;
				if (best == null || c.sortingOrder > best.sortingOrder) best = c;
			}
			return best != null ? best.transform : null;
		}
	}
}
