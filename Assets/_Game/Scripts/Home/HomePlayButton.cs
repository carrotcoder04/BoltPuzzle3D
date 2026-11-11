using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Nút Play/Continue ở Home: hiện "Level N", nhãn Hard (mỗi 5 màn), tim vô hạn; hết tim → mở Shop.</summary>
public class HomePlayButton : MonoBehaviour
{
	[SerializeField] Sprite _hardPlayBtnBgNormal;
	[SerializeField] Sprite _hardPlayBtnBgClick;
	[SerializeField] Sprite _hardPlayBtnUnlimitedTipBg;

	public static bool IsHardLevel(int level) => level > 5 && level % 5 == 0;

	TMP_Text _countdown;
	GameObject _unlimited;

	void Awake()
	{
		int level = UserDataManager.Instance != null ? UserDataManager.Instance.CurrentLevel : 1;
		bool hard = IsHardLevel(level);
		UIUtil.SetText(transform.FindComp<TMP_Text>("Content/LevelText"), "Level " + level);
		transform.SetActive("Content/HardTag", hard);
		transform.SetActive("DoubleTip", false);
		if (hard)
		{
			var img = GetComponent<Image>();
			if (img != null && _hardPlayBtnBgNormal != null) img.sprite = _hardPlayBtnBgNormal;
		}
		var u = transform.Find("UnlimitedTip");
		_unlimited = u != null ? u.gameObject : null;
		_countdown = transform.FindComp<TMP_Text>("UnlimitedTip/Countdown");
		var btn = GetComponent<Button>();
		if (btn != null) btn.onClick.AddListener(OnPlay);
	}

	void Update()
	{
		var inv = InventoryManager.Instance;
		bool on = inv != null && inv.IsUnlimitedHeart;
		if (_unlimited != null && _unlimited.activeSelf != on) _unlimited.SetActive(on);
		if (on && _countdown != null)
		{
			var t = inv.UnlimitedHeartRemaining;
			_countdown.text = $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}";
		}
	}

	void OnPlay()
	{
		var inv = InventoryManager.Instance;
		if (inv != null && !inv.HasHeart)
		{
			UIUtil.Deny(this);
			if (NavigationController.Instance != null) NavigationController.Instance.GoTo("ShopPage");
			return;
		}
		SceneManager.LoadScene(GameController.GameSceneName);
	}
}
