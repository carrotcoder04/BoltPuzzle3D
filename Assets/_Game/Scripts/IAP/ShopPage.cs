using ShopUI;
using UnityEngine;

/// <summary>Tab Shop ở Home: chèn ShopDialogContent (chế độ trang) vào SafeArea.</summary>
public class ShopPage : MonoBehaviour
{
	ShopDialogContent _content;

	void Awake()
	{
		var safe = transform.Find("SafeArea") ?? transform;
		_content = safe.GetComponentInChildren<ShopDialogContent>(true);
		var cfg = GameConfig.Instance;
		if (_content == null && cfg != null && cfg.ShopContent != null)
		{
			var go = Instantiate(cfg.ShopContent, safe, false);
			var rt = go.transform as RectTransform;
			if (rt != null)
			{
				rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
				rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
			}
			_content = go.GetComponent<ShopDialogContent>();
		}
		if (_content != null) _content.IsPageMode = true;
	}
}
