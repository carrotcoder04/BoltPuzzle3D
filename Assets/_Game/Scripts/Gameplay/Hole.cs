using DG.Tweening;
using UnityEngine;

/// <summary>Lỗ chờ tạm trên khay HoleRoot (Hole1..Hole7). Mỗi lỗ chứa tối đa 1 ốc.</summary>
public class Hole : MonoBehaviour
{
	public Screw Screw { get; private set; }
	public bool Reserved { get; private set; }
	public bool IsFree => Screw == null && !Reserved;
	public Transform Pos { get; private set; }

	GameObject _failNotice;

	bool _inited;
	Vector3 _baseScale = Vector3.one;

	void Awake() => Init();

	/// <summary>Khởi tạo (gọi được cả khi GameObject đang tắt, ví dụ Hole6/7).</summary>
	public void Init()
	{
		if (_inited) return;
		_inited = true;
		_baseScale = transform.localScale;
		Pos = transform.Find("Pos") ?? transform;
		var f = transform.Find("FailNotice");
		_failNotice = f != null ? f.gameObject : null;
		ShowFailNotice(false);
	}

	public void Reserve() => Reserved = true;

	public void Place(Screw s)
	{
		Reserved = false;
		Screw = s;
		if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.ScrewInPlace);
		Pos.DOKill(true);
		Pos.DOPunchScale(Vector3.one * 0.25f, 0.25f, 6, 0.6f);
	}

	/// <summary>Lấy ốc ra khỏi lỗ (để bay vào hộp hoặc vào kho dự trữ).</summary>
	public Screw Take()
	{
		var s = Screw;
		Screw = null;
		return s;
	}

	public void ShowFailNotice(bool show)
	{
		if (_failNotice != null) _failNotice.SetActive(show);
	}

	public bool IsActive => gameObject.activeSelf;

	public void Unlock()
	{
		Init();
		gameObject.SetActive(true);
		transform.DOKill();
		transform.localScale = Vector3.zero;
		// lỗ mới bật ra to hơn rồi thu về, xoay một vòng, kèm hiệu ứng mở khoá
		var seq = DOTween.Sequence();
		seq.Append(transform.DOScale(_baseScale * 1.35f, 0.25f).SetEase(Ease.OutBack));
		seq.Join(transform.DOLocalRotate(new Vector3(0f, 0f, 360f), 0.45f, RotateMode.LocalAxisAdd).SetEase(Ease.OutCubic));
		seq.Append(transform.DOScale(_baseScale, 0.2f).SetEase(Ease.InOutSine));
		seq.Append(transform.DOPunchScale(_baseScale * 0.12f, 0.25f, 6, 0.6f));
		seq.SetLink(gameObject);
		DOVirtual.DelayedCall(0.05f, () => { if (this != null) FxManager.Unlock(Pos != null ? Pos.position : transform.position, 0.8f); }).SetLink(gameObject);
		if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.Revive);
		if (VibrationManager.Instance != null) VibrationManager.Instance.Medium();
	}
}
