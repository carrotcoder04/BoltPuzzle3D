using DG.Tweening;
using UnityEngine;

/// <summary>Trình tự thắng: logo + pháo hoa → bảng nhận thưởng.</summary>
public class WinDialog : MonoBehaviour
{
	public float LogoDuration = 1.3f;

	void Start()
	{
		var logo = GetComponentInChildren<WinLogoDialogContent>(true);
		var content = GetComponentInChildren<WinDialogContent>(true);
		transform.SetActive("WinRvDialogContent", false);
		if (content != null) content.gameObject.SetActive(false);
		if (logo == null) { if (content != null) content.Show(); return; }
		logo.Show();
		DOVirtual.DelayedCall(LogoDuration, () =>
		{
			logo.Hide(() => { if (content != null) content.Show(); });
		}).SetLink(gameObject);
	}
}
