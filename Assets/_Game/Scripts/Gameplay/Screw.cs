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
	public void FlyTo(Transform parent, float duration, Action onArrive)
	{
		SetLayerRecursive(gameObject, GameController.TopLayer);
		transform.DOKill();
		var start = transform.position;
		var end = parent.position;
		var mid = (start + end) * 0.5f + Vector3.up * 2f;
		var targetRot = parent.rotation * Quaternion.Euler(0f, 0f, 90f);
		var startScale = transform.lossyScale;
		var seq = DOTween.Sequence();
		seq.Append(DOVirtual.Float(0f, 1f, duration, t =>
		{
			float u = 1f - t;
			transform.position = u * u * start + 2f * u * t * mid + t * t * end;
		}).SetEase(Ease.InOutQuad));
		seq.Join(transform.DORotateQuaternion(targetRot, duration));
		seq.OnComplete(() =>
		{
			transform.SetParent(parent, true);
			transform.localPosition = Vector3.zero;
			transform.rotation = targetRot;
			onArrive?.Invoke();
		});
	}

	public static void SetLayerRecursive(GameObject go, int layer)
	{
		go.layer = layer;
		foreach (Transform c in go.transform) SetLayerRecursive(c.gameObject, layer);
	}

	void OnDestroy() => transform.DOKill();
}
