using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Nút công cụ (Key = drill / hammer / broom). Hết lượt → mua bằng coin (GameConfig.ToolPrice).</summary>
public class ToolBase : MonoBehaviour
{
	public AudioClip CollectSound;
	public string Key;

	GameController _gc;
	Button _btn;
	TMP_Text _count;
	GameObject _countBg, _add, _bgLight, _priceTag;

	static int Price => GameConfig.Instance != null ? GameConfig.Instance.ToolPrice : 300;

	public BoosterType Type
	{
		get
		{
			switch ((Key ?? "").ToLowerInvariant())
			{
				case "hammer": return BoosterType.Hammer;
				case "broom": return BoosterType.Broom;
				default: return BoosterType.Drill;
			}
		}
	}

	void Awake()
	{
		if (string.IsNullOrEmpty(Key)) Key = name.ToLowerInvariant();
		_btn = GetComponent<Button>();
		_count = transform.FindComp<TMP_Text>("CountBg/Count");
		_countBg = Child("CountBg");
		_add = Child("Add");
		_bgLight = Child("BgLight");
		transform.SetActive("Free", false);
		transform.SetActive("UnlockText", false);
		transform.SetActive("AddSpark", false);
		if (_bgLight != null) _bgLight.SetActive(false);
		if (_btn != null) _btn.onClick.AddListener(OnClick);
	}

	GameObject Child(string n)
	{
		var t = transform.Find(n);
		return t != null ? t.gameObject : null;
	}

	public void Bind(GameController gc)
	{
		_gc = gc;
		gc.OnModeChanged += OnMode;
		if (Managers.Inventory != null) Managers.Inventory.OnBoosterChanged += OnBooster;
		Refresh();
	}

	void OnDestroy()
	{
		if (_gc != null) _gc.OnModeChanged -= OnMode;
		if (Managers.Inventory != null) Managers.Inventory.OnBoosterChanged -= OnBooster;
	}

	void OnBooster(BoosterType t, int v) { if (t == Type) Refresh(); }

	void Refresh()
	{
		int n = Managers.Inventory != null ? Managers.Inventory.GetBooster(Type) : 0;
		if (_countBg != null) _countBg.SetActive(n > 0);
		EnsurePriceTag();
		// hết lượt: thay dấu "+" bằng nhãn giá [coin][giá] để người chơi biết bấm là mua bằng coin
		if (_add != null) _add.SetActive(n <= 0 && _priceTag == null);
		if (_priceTag != null) _priceTag.SetActive(n <= 0);
		UIUtil.SetText(_count, n.ToString());
	}

	void EnsurePriceTag()
	{
		if (_priceTag != null || _count == null || _gc == null) return;
		var ui = _gc.GameCanvas;
		var go = new GameObject("PriceTag", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
		var rt = (RectTransform)go.transform;
		rt.SetParent(transform, false);
		var addRt = _add != null ? _add.transform as RectTransform : null;
		rt.anchorMin = rt.anchorMax = addRt != null ? addRt.anchorMin : new Vector2(0.5f, 0.5f);
		rt.anchoredPosition = addRt != null ? addRt.anchoredPosition : new Vector2(0f, -100f);
		var bg = go.GetComponent<Image>();
		bg.raycastTarget = false;
		if (ui != null && ui.PriceTagSprite != null) { bg.sprite = ui.PriceTagSprite; bg.type = Image.Type.Sliced; }
		else bg.color = new Color(0.17f, 0.23f, 0.43f, 0.9f);
		var hlg = go.GetComponent<HorizontalLayoutGroup>();
		hlg.padding = new RectOffset(4, 12, 3, 3);
		hlg.spacing = 3f;
		hlg.childAlignment = TextAnchor.MiddleCenter;
		hlg.childControlWidth = hlg.childControlHeight = true;
		hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;
		var fit = go.GetComponent<ContentSizeFitter>();
		fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		if (ui != null && ui.CoinSprite != null)
		{
			var ic = new GameObject("Coin", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
			ic.transform.SetParent(rt, false);
			var im = ic.GetComponent<Image>();
			im.sprite = ui.CoinSprite; im.preserveAspect = true; im.raycastTarget = false;
			var le = ic.GetComponent<LayoutElement>();
			le.preferredWidth = le.preferredHeight = 38f;
		}
		var num = Instantiate(_count, rt, false);
		num.name = "Price";
		num.gameObject.SetActive(true);
		num.enableAutoSizing = false;
		num.fontSize = 30f;
		num.enableWordWrapping = false;
		num.alignment = TextAlignmentOptions.Midline;
		num.color = Color.white;
		num.raycastTarget = false;
		foreach (Transform c in num.transform) Destroy(c.gameObject);
		foreach (var mb in num.GetComponents<MonoBehaviour>()) if (!(mb is TMP_Text)) Destroy(mb);
		num.text = Price.ToString();
		_priceTag = go;
	}

	void OnMode(ToolMode m)
	{
		bool on = (m == ToolMode.Drill && Type == BoosterType.Drill) || (m == ToolMode.Hammer && Type == BoosterType.Hammer);
		if (_bgLight != null) _bgLight.SetActive(on);
		transform.DOKill(true);
		if (on) transform.DOScale(1.1f, 0.15f);
		else transform.DOScale(1f, 0.15f);
	}

	void OnClick()
	{
		if (_gc == null || _gc.InputLocked) return;
		var inv = Managers.Inventory;
		if (inv == null) return;
		bool activeMode = (_gc.Mode == ToolMode.Drill && Type == BoosterType.Drill) || (_gc.Mode == ToolMode.Hammer && Type == BoosterType.Hammer);
		if (!activeMode && inv.GetBooster(Type) <= 0)
		{
			int price = Price;
			var ud = Managers.UserData;
			if (ud == null || !ud.TrySpendCoins(price, "tool_" + Key))
			{
				UIUtil.Deny(this);
				RewardToast.Show("Not enough coins!\nNeed " + price + " coins (you have " + (ud != null ? ud.Coins : 0) + ")");
				return;
			}
			inv.AddBooster(Type, 1);
			RewardToast.Show("Bought +1 " + RewardToast.BoosterName(Type) + "\n-" + price + " coins");
			if (CollectSound != null && Managers.Audio != null) Managers.Audio.PlayClip(CollectSound);
		}
		if (!_gc.UseTool(Type)) UIUtil.Deny(this);
		else if (Managers.Audio != null && Type != BoosterType.Broom) Managers.Audio.PlaySfx(Sfx.ToolShow);
	}
}
