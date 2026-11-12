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

	/// <summary>Rung lắc khi ốc bị chặn.</summary>
	public void ShakeBlocked()
	{
		transform.DOKill(true);
		transform.DOPunchPosition(ExtractDir * 0.15f, 0.25f, 10, 0.5f);
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
		var seq = DOTween.Sequence();
		seq.Join(transform.DOMove(transform.position + dir * PullDistance, 0.18f).SetEase(Ease.OutQuad));
		seq.Join(transform.DOLocalRotate(new Vector3(720f, 0f, 0f), 0.18f, RotateMode.LocalAxisAdd));
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
		var seq = DOTween.Sequence();
		seq.Append(DOVirtual.Float(0f, 1f, duration, t =>
		{
			float u = 1f - t;
			transform.position = u * u * start + 2f * u * t * mid + t * t * end;
			SetWorldScale(Mathf.Lerp(startScale, endScale, t));
		}).SetEase(Ease.InOutQuad));
		seq.Join(transform.DORotateQuaternion(targetRot, duration));
		seq.OnComplete(() =>
		{
			transform.SetParent(parent, true);
			transform.position = end;
			transform.rotation = targetRot;
			SetWorldScale(endScale);
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
