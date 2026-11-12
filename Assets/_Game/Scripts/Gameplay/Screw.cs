using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Con ốc trên một Board. Trục rút ốc là transform.right (đầu ốc hướng +X local, theo collider gốc).
/// </summary>
public class Screw : MonoBehaviour
{
	public int BoardIndex;
	public int ScrewColor;

	public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
	public const float PullDistance = 1.2f;

	public Board Board { get; set; }
	public bool IsRemoved { get; private set; }
	public Vector3 ExtractDir => transform.right;

	Renderer _renderer;
	Collider _col;
	MaterialPropertyBlock _mpb;

	public Renderer Renderer => _renderer != null ? _renderer : (_renderer = GetComponent<Renderer>());

	void Awake()
	{
		_col = GetComponent<Collider>();
	}

	public void SetColor(int colorIndex, Color c)
	{
		ScrewColor = colorIndex;
		if (Renderer == null) return;
		if (_mpb == null) _mpb = new MaterialPropertyBlock();
		Renderer.GetPropertyBlock(_mpb);
		_mpb.SetColor(BaseColorId, c);
		Renderer.SetPropertyBlock(_mpb);
	}

	/// <summary>Rung lắc khi ốc bị chặn: nhích ra theo trục rồi bật lại, kèm lắc xoay để người chơi thấy rõ là bị kẹt.</summary>
	public void ShakeBlocked()
	{
		transform.DOKill(true);
		var seq = DOTween.Sequence();
		seq.Append(transform.DOPunchPosition(ExtractDir * 0.28f, 0.32f, 8, 0.6f));
		seq.Join(transform.DOPunchRotation(new Vector3(25f, 0f, 0f), 0.32f, 10, 0.5f));
		seq.Join(transform.DOPunchScale(Vector3.one * 0.12f, 0.2f, 6, 0.5f));
		seq.SetLink(gameObject);
		if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.ScrewUnmoveable);
		if (VibrationManager.Instance != null) VibrationManager.Instance.Medium();
	}

	/// <summary>Tách khỏi tấm và xoáy ra theo trục; gọi onPulled khi xong.</summary>
	public void Extract(Action onPulled)
	{
		if (IsRemoved) return;
		IsRemoved = true;
		if (_col != null) _col.enabled = false;
		var dir = ExtractDir;
		var b = Board;
		transform.SetParent(null, true);
		if (b != null) b.RemoveScrew(this);
		if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.Screw);
		if (VibrationManager.Instance != null) VibrationManager.Instance.Light();
		transform.DOKill();
		var p0 = transform.position;
		var baseScale = transform.localScale;
		var seq = DOTween.Sequence();
		// 1. nảy nhẹ khi chạm
		seq.Append(transform.DOScale(baseScale * 1.18f, 0.06f).SetEase(Ease.OutQuad));
		// 2. xoáy ra: 2 vòng quanh trục ốc, trượt ra theo hướng rút
		seq.Append(transform.DOMove(p0 + dir * PullDistance * 1.4f, 0.26f).SetEase(Ease.OutCubic));
		seq.Join(transform.DOLocalRotate(new Vector3(720f, 0f, 0f), 0.26f, RotateMode.LocalAxisAdd).SetEase(Ease.OutCubic));
		seq.Join(transform.DOScale(baseScale, 0.2f).SetEase(Ease.OutBack));
		// 3. khựng lại một nhịp trước khi bay
		seq.AppendInterval(0.04f);
		seq.SetLink(gameObject);
		seq.OnComplete(() => onPulled?.Invoke());
	}

	/// <summary>Bay tới vị trí đích (world) trên lớp Top rồi gắn vào parent.</summary>
	/// <summary>Đường kính đầu ốc (đơn vị local của ốc), lấy từ BoxCollider gốc.</summary>
	public float HeadDiameter
	{
		get
		{
			var bc = (_col != null ? _col : GetComponent<Collider>()) as BoxCollider;
			return bc != null ? Mathf.Max(0.05f, Mathf.Max(bc.size.y, bc.size.z)) : 0.71f;
		}
	}

	/// <summary>
	/// Bay tới vị trí đích (world) trên lớp Top rồi gắn vào parent.
	/// Khi tới nơi: đầu ốc quay về phía camera (cắm thẳng vào lỗ) và co giãn để đầu ốc rộng headWorldSize.
	/// </summary>
	public void FlyTo(Transform parent, float duration, Action onArrive, float headWorldSize = 0f)
	{
		SetLayerRecursive(gameObject, GameController.TopLayer);
		transform.DOKill();
		var cam = Camera.main;
		var start = transform.position;
		Vector3 toCam = cam != null ? -cam.transform.forward : Vector3.back;
		Vector3 camUp = cam != null ? cam.transform.up : Vector3.up;
		var end = parent.position + toCam * 0.3f;
		var mid = (start + end) * 0.5f + camUp * 2f + toCam * 2f;
		// local +X (hướng đầu ốc) quay về phía camera
		var targetRot = Quaternion.LookRotation(toCam, camUp) * Quaternion.Euler(0f, -90f, 0f);
		var startScale = transform.lossyScale.x;
		if (headWorldSize <= 0f && GameController.Instance != null) headWorldSize = GameController.Instance.ScrewHeadWorldSize;
		float endScale = headWorldSize > 0f ? headWorldSize / HeadDiameter : startScale;
		// Đích được tính lại mỗi khung hình: hộp/lỗ có thể đang nảy, đang phóng to khi vừa xuất hiện
		// hoặc đang dời chỗ, nếu chốt vị trí lúc bắt đầu bay thì ốc sẽ cắm lệch.
		Func<Vector3> liveEnd = () => (parent != null ? parent.position : end) + toCam * 0.3f;
		var seq = DOTween.Sequence();
		// 1. bay theo đường cong tới phía trên lỗ, co dần về kích thước đích
		seq.Append(DOVirtual.Float(0f, 1f, duration, t =>
		{
			float u = 1f - t;
			var above = liveEnd() + toCam * 0.9f;
			transform.position = u * u * start + 2f * u * t * mid + t * t * above;
			SetWorldScale(Mathf.Lerp(startScale, endScale * 1.25f, t));
		}).SetEase(Ease.InOutSine));
		seq.Join(transform.DORotateQuaternion(targetRot, duration * 0.8f).SetEase(Ease.OutQuad));
		// 2. vặn vào lỗ: 1 vòng quanh trục ốc, hạ xuống và thu về đúng cỡ
		seq.Append(DOVirtual.Float(0f, 1f, 0.14f, t =>
		{
			var e = liveEnd();
			transform.position = Vector3.Lerp(e + toCam * 0.9f, e, t);
		}).SetEase(Ease.InQuad));
		seq.Join(transform.DORotateQuaternion(targetRot * Quaternion.Euler(-180f, 0f, 0f), 0.07f).SetEase(Ease.Linear));
		seq.Append(DOVirtual.Float(0f, 1f, 0.07f, t => { transform.position = liveEnd(); }));
		seq.Join(transform.DORotateQuaternion(targetRot, 0.07f).SetEase(Ease.Linear));
		seq.Join(DOVirtual.Float(endScale * 1.25f, endScale, 0.07f, v => SetWorldScale(v)));
		seq.SetLink(gameObject);
		seq.OnComplete(() =>
		{
			// Kết thúc ngay các tween nảy/co giãn của hộp/lỗ để tính đúng tỉ lệ khi gắn vào (tránh ốc bị nhỏ/lệch vĩnh viễn).
			for (var p = parent; p != null && p != parent.root; p = p.parent)
				if (p.GetComponent<BoxScript>() != null || p.GetComponent<Hole>() != null) { p.DOKill(true); break; }
			if (parent != null) parent.DOKill(true);
			var finalPos = liveEnd();
			transform.SetParent(parent, true);
			transform.position = finalPos;
			transform.rotation = targetRot;
			SetWorldScale(endScale);
			// 3. nảy khi cắm xong
			transform.DOPunchScale(transform.localScale * 0.22f, 0.22f, 6, 0.6f).SetLink(gameObject);
			if (VibrationManager.Instance != null) VibrationManager.Instance.Light();
			onArrive?.Invoke();
		});
	}

	void SetWorldScale(float world)
	{
		var p = transform.parent;
		float ps = p != null ? Mathf.Max(1e-4f, p.lossyScale.x) : 1f;
		transform.localScale = Vector3.one * (world / ps);
	}

	public static void SetLayerRecursive(GameObject go, int layer)
	{
		go.layer = layer;
		foreach (Transform c in go.transform) SetLayerRecursive(c.gameObject, layer);
	}

	void OnDestroy() => transform.DOKill();
}
