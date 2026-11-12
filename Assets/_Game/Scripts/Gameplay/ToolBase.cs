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
	GameObject _countBg, _add, _bgLight;

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
		if (_add != null) _add.SetActive(n <= 0);
		UIUtil.SetText(_count, n.ToString());
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
			// hết lượt: mở popup mua bằng coin; mua xong cộng 1 lượt (bấm lại để dùng)
			var kind = Type == BoosterType.Hammer ? BuyItemDialogContent.Kind.Hammer : Type == BoosterType.Broom ? BuyItemDialogContent.Kind.Broom : BuyItemDialogContent.Kind.Drill;
			BuyItemDialogContent.Open(kind, Price, () =>
			{
				inv.AddBooster(Type, 1);
				if (CollectSound != null && Managers.Audio != null) Managers.Audio.PlayClip(CollectSound);
				RewardToast.Show("+1 " + RewardToast.BoosterName(Type) + "\n-" + Price + " coins");
				transform.DOPunchScale(Vector3.one * 0.2f, 0.4f, 6, 0.5f);
			});
			return;
		}
		if (!_gc.UseTool(Type)) UIUtil.Deny(this);
		else if (Managers.Audio != null && Type != BoosterType.Broom) Managers.Audio.PlaySfx(Sfx.ToolShow);
	}
}
