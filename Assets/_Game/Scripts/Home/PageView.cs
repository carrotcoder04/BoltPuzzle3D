using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Sugar.UI
{
	/// <summary>Các trang ngang (ScrollRect) – mỗi PageViewCell rộng đúng bằng viewport.</summary>
	[RequireComponent(typeof(ScrollRect))]
	public class PageView : MonoBehaviour
	{
		public bool Interactable;
		public float BounceBackTime = 0.2f;
		public float PageSwipeThreshold = 150f;

		ScrollRect _scroll;
		readonly List<PageViewCell> _pages = new List<PageViewCell>();
		float _lastWidth = -1f;
		int _current;

		public int Current => _current;
		public IReadOnlyList<PageViewCell> Pages => _pages;

		void Awake()
		{
			_scroll = GetComponent<ScrollRect>();
			_scroll.horizontal = Interactable;
			_scroll.vertical = false;
			_scroll.inertia = false;
			Refresh();
		}

		public void Refresh()
		{
			_pages.Clear();
			if (_scroll.content == null) return;
			foreach (Transform t in _scroll.content)
			{
				if (!t.gameObject.activeSelf) continue;
				var c = t.GetOrAdd<PageViewCell>();
				_pages.Add(c);
			}
			_lastWidth = -1f;
			UpdateSizes();
		}

		void LateUpdate() => UpdateSizes();

		void UpdateSizes()
		{
			var vp = _scroll.viewport != null ? _scroll.viewport : (RectTransform)transform;
			float w = vp.rect.width;
			if (Mathf.Approximately(w, _lastWidth) || w <= 0f) return;
			_lastWidth = w;
			float h = vp.rect.height;
			// Layout group gốc không điều khiển kích thước con (childControlWidth = false) nên LayoutElement bị bỏ qua,
			// trang giữ nguyên bề rộng 750 của thiết kế và lộ trang bên cạnh trên màn hình rộng (iPad, máy gập).
			var hlg = _scroll.content.GetComponent<HorizontalLayoutGroup>();
			if (hlg != null)
			{
				hlg.childControlWidth = hlg.childControlHeight = true;
				hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;
				hlg.spacing = 0f;
				hlg.padding = new RectOffset();
			}
			foreach (var p in _pages)
			{
				var prt = (RectTransform)p.transform;
				prt.sizeDelta = new Vector2(w, h);
				var le = p.GetOrAdd<LayoutElement>();
				le.minWidth = le.preferredWidth = w;
				le.minHeight = le.preferredHeight = h;
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
			ScrollTo(_current, false);
		}

		public int IndexOf(string pageName) => _pages.FindIndex(p => p.name == pageName);

		public void ScrollTo(int index, bool animate = true)
		{
			if (_pages.Count == 0) return;
			_current = Mathf.Clamp(index, 0, _pages.Count - 1);
			float target = _pages.Count > 1 ? (float)_current / (_pages.Count - 1) : 0f;
			DOTween.Kill(this);
			if (!animate) { _scroll.horizontalNormalizedPosition = target; return; }
			DOTween.To(() => _scroll.horizontalNormalizedPosition, v => _scroll.horizontalNormalizedPosition = v, target, Mathf.Max(0.05f, BounceBackTime * 1.5f))
				.SetEase(Ease.OutCubic).SetTarget(this);
		}
	}
}
