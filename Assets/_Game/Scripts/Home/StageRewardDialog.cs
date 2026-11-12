using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Popup nhận rương mốc level: hiện số coin, chạm để nhận.</summary>
public class StageRewardDialog : MonoBehaviour
{
	public GameObject RewardCellPrefab;
	public AudioClip RewardSound;
	public AudioClip EnterSound;
	public AudioClip ChestSound;
	public AudioClip FireworksClip;
	public GameObject Fireworks;

	bool _claimed;

	void Awake()
	{
		var rt = transform as RectTransform;
		if (rt != null) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
		UIUtil.SetText(transform.FindComp<TMP_Text>("SafeArea/Top/Ribbon/Count"), "+" + LevelChest.Coins);
		UIUtil.SetText(transform.FindComp<TMP_Text>("SafeArea/Top/Desc"), "Reward for completing " + (LevelChest.Earned * LevelChest.Target) + " levels!");
		var btn = gameObject.GetOrAdd<Button>();
		btn.onClick.AddListener(Claim);
		Play(EnterSound);
		transform.localScale = Vector3.one * 0.8f;
		transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
	}

	void Play(AudioClip c)
	{
		if (c != null && AudioManager.Instance != null) AudioManager.Instance.PlayClip(c);
	}

	void Claim()
	{
		if (_claimed) return;
		_claimed = true;
		LevelChest.Claim();
		Play(ChestSound != null ? ChestSound : RewardSound);
		transform.DOScale(0.8f, 0.2f).SetEase(Ease.InBack).OnComplete(() => Destroy(gameObject));
	}

	void OnDestroy() => transform.DOKill();
}
