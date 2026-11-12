using System.Collections.Generic;
using Snui;
using Sugar.UI;
using UnityEngine;

/// <summary>
/// Nối thanh tab với PageView. Bản rebuild chỉ dùng tab Shop và Home;
/// Rank / Team / Card bị ẩn (tính năng online đã bỏ).
/// </summary>
public class NavigationController : MonoBehaviour
{
	[SerializeField] PageView _pageView;

	public static NavigationController Instance { get; private set; }

	static readonly string[] Disabled = { "Rank", "Team", "Card" };

	NavigationBar _bar;
	readonly Dictionary<string, NavigationCell> _cellByPage = new Dictionary<string, NavigationCell>();

	void Awake()
	{
		Instance = this;
		_bar = GetComponent<NavigationBar>() ?? gameObject.AddComponent<NavigationBar>();
		if (_pageView == null) _pageView = FindAnyObjectByType<PageView>(FindObjectsInactive.Include);
	}

	void Start()
	{
		var content = _pageView != null ? _pageView.GetComponent<UnityEngine.UI.ScrollRect>().content : null;
		foreach (var key in Disabled)
		{
			if (content != null) content.SetActive(key + "Page", false);
			transform.SetActive(key + "Cell", false);
		}
		if (_pageView != null) _pageView.Refresh();
		LayoutCells();

		foreach (var cell in _bar.Cells)
		{
			if (cell == null || !cell.gameObject.activeSelf) continue;
			string page = cell.name.Replace("Cell", "Page");
			_cellByPage[page] = cell;
			var c = cell;
			if (c.Button != null) c.Button.onClick.AddListener(() => GoTo(page));
		}
		GoTo("HomePage", false);
	}

	/// <summary>Chia đều bề rộng thanh tab cho các tab còn hiện (thiết kế gốc đặt cứng vị trí cho 5 tab trên màn 750).</summary>
	void LayoutCells()
	{
		var active = new List<NavigationCell>();
		foreach (var c in _bar.Cells) if (c != null && c.gameObject.activeSelf) active.Add(c);
		active.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
		int n = active.Count;
		for (int i = 0; i < n; i++)
		{
			var rt = (RectTransform)active[i].transform;
			rt.anchorMin = new Vector2((float)i / n, rt.anchorMin.y);
			rt.anchorMax = new Vector2((float)(i + 1) / n, rt.anchorMax.y);
			rt.pivot = new Vector2(0.5f, rt.pivot.y);
			rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
			rt.offsetMax = new Vector2(0f, rt.offsetMax.y);
		}
		Canvas.ForceUpdateCanvases();
	}

	void OnDestroy() { if (Instance == this) Instance = null; }

	public void GoTo(string pageName, bool animate = true)
	{
		if (_pageView == null) return;
		int i = _pageView.IndexOf(pageName);
		if (i < 0) return;
		_pageView.ScrollTo(i, animate);
		_cellByPage.TryGetValue(pageName, out var cell);
		_bar.Select(cell, animate);
	}
}
