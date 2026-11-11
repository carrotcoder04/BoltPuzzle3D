using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Snui
{
	/// <summary>Thanh tab dưới: quản lý các NavigationCell và con trỏ (Cursor) chạy theo tab đang chọn.</summary>
	public class NavigationBar : MonoBehaviour
	{
		public bool NavigationAnim = true;
		public float CursorExpand = 1.6f;
		[SerializeField] RectTransform _cursor;
		[SerializeField] Transform _cellsNode;
		[SerializeField] List<NavigationCell> _cells = new List<NavigationCell>();

		public List<NavigationCell> Cells
		{
			get
			{
				if (_cells == null || _cells.Count == 0)
				{
					if (_cellsNode == null) _cellsNode = transform.Find("Buttons");
					_cells = new List<NavigationCell>(_cellsNode != null ? _cellsNode.GetComponentsInChildren<NavigationCell>(true) : GetComponentsInChildren<NavigationCell>(true));
				}
				return _cells;
			}
		}

		void Awake()
		{
			if (_cursor == null) _cursor = transform.Find("Cursor") as RectTransform;
		}

		public void Select(NavigationCell cell, bool animate = true)
		{
			foreach (var c in Cells) if (c != null) c.SetSelected(c == cell, animate);
			if (_cursor == null || cell == null) return;
			var crt = cell.transform as RectTransform;
			if (crt != null)
			{
				var size = _cursor.sizeDelta;
				size.x = crt.rect.width * Mathf.Max(1f, CursorExpand) / 1.6f;
				_cursor.sizeDelta = size;
			}
			_cursor.DOKill();
			if (animate && NavigationAnim) _cursor.DOMoveX(cell.transform.position.x, 0.2f).SetEase(Ease.OutCubic);
			else _cursor.position = new Vector3(cell.transform.position.x, _cursor.position.y, _cursor.position.z);
		}
	}
}
