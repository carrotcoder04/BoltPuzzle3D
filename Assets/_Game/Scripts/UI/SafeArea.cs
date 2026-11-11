using UnityEngine;

namespace Snui
{
	/// <summary>Co RectTransform vào Screen.safeArea (tai thỏ, thanh home iOS...).</summary>
	[RequireComponent(typeof(RectTransform))]
	public class SafeArea : MonoBehaviour
	{
		RectTransform _rect;
		Rect _lastSafe;
		Vector2Int _lastScreen;

		void OnEnable()
		{
			_rect = GetComponent<RectTransform>();
			Apply(true);
		}

		void Update() => Apply(false);

		void Apply(bool force)
		{
			if (_rect == null) return;
			Rect safe = Screen.safeArea;
			var screen = new Vector2Int(Screen.width, Screen.height);
			if (!force && safe == _lastSafe && screen == _lastScreen) return;
			_lastSafe = safe;
			_lastScreen = screen;
			if (screen.x <= 0 || screen.y <= 0) return;

			Vector2 min = safe.position;
			Vector2 max = safe.position + safe.size;
			min.x /= screen.x; min.y /= screen.y;
			max.x /= screen.x; max.y /= screen.y;
			_rect.anchorMin = min;
			_rect.anchorMax = max;
			_rect.offsetMin = Vector2.zero;
			_rect.offsetMax = Vector2.zero;
		}
	}
}
