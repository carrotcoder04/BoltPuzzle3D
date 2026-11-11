using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Widget tiến độ ở góc Home – bản rebuild dùng làm thanh "Level Chest":
/// thắng đủ ChestEveryLevels màn thì bấm để mở rương.
/// </summary>
public class SpecialScrewWidget : MonoBehaviour
{
	public AudioClip CollectSound;
	public Sprite ProgressBgNor;
	public Sprite ProgressBgLock;

	ProgressBar _bar;
	TMP_Text _text;

	void Awake()
	{
		foreach (var n in new[] { "LockSpine", "UnlockLightSpine", "Timer", "Tutorial" }) transform.SetActive(n, false);
		_bar = transform.FindComp<ProgressBar>("ProgressBg");
		_text = transform.FindComp<TMP_Text>("ProgressBg/Text");
		UIUtil.SetText(transform.FindComp<TMP_Text>("RewardCell/Count"), LevelChest.Coins.ToString());
		transform.BindButton(name, OnClick);
	}

	void OnEnable()
	{
		Refresh();
		if (UserDataManager.Instance != null) UserDataManager.Instance.OnCoinsChanged += OnCoins;
	}

	void OnDisable()
	{
		if (UserDataManager.Instance != null) UserDataManager.Instance.OnCoinsChanged -= OnCoins;
	}

	void OnCoins(int _) => Refresh();

	void Refresh()
	{
		int cur = LevelChest.Progress, max = LevelChest.Target;
		if (_bar != null) _bar.SetProgress(cur, max);
		UIUtil.SetText(_text, cur + "/" + max);
		transform.DOKill(true);
		if (LevelChest.IsReady) transform.DOScale(1.06f, 0.5f).SetLoops(-1, LoopType.Yoyo);
		else transform.localScale = Vector3.one;
	}

	void OnClick()
	{
		if (!LevelChest.IsReady) { UIUtil.Deny(this); return; }
		if (HomeController.Instance != null) HomeController.Instance.OpenChest();
	}
}
