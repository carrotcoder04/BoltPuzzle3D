using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Hộp đựng ốc 1 màu, 3 ô (ScrewPos1..3). Đủ ốc thì đóng nắp, bay đi và báo GameController.
/// </summary>
public class BoxScript : MonoBehaviour
{
	public int MaxScrewCount = 3;
	public List<Sprite> BoxSpriteList = new List<Sprite>();
	public List<Sprite> LidSpriteList = new List<Sprite>();
	public int Color;
	public bool OnMoving;

	public int Slot { get; set; }
	public int Filled { get; private set; }
	public int Reserved { get; private set; }
	public bool IsFull => Filled + Reserved >= MaxScrewCount;
	public bool IsClosing { get; private set; }

	readonly List<Transform> _pos = new List<Transform>();
	readonly List<Screw> _screws = new List<Screw>();
	SpriteRenderer _box;
	SpriteRenderer _lid;
	Transform _lidRoot;

	public event Action<BoxScript> Completed;

	void Awake()
	{
		var boxNode = transform.Find("Box");
		if (boxNode != null)
		{
			var s = boxNode.Find("Box");
			_box = s != null ? s.GetComponent<SpriteRenderer>() : null;
			for (int i = 1; i <= MaxScrewCount; i++)
			{
				var p = boxNode.Find("ScrewPos" + i);
				if (p != null) _pos.Add(p);
			}
		}
		_lidRoot = transform.Find("Lid");
		if (_lidRoot != null)
		{
			var l = _lidRoot.Find("Lid");
			_lid = l != null ? l.GetComponent<SpriteRenderer>() : null;
			_lidRoot.gameObject.SetActive(false);
		}
		var anim = GetComponent<Animation>();
		if (anim != null) anim.playAutomatically = false;
	}

	public void Setup(int color, int slot)
	{
		Color = color;
		Slot = slot;
		if (_box != null && color < BoxSpriteList.Count) _box.sprite = BoxSpriteList[color];
		if (_lid != null && color < LidSpriteList.Count) _lid.sprite = LidSpriteList[color];
	}

	/// <summary>Giữ chỗ 1 ô và trả về transform của ô đó.</summary>
	public Transform ReserveSlot()
	{
		if (IsFull || IsClosing) return null;
		int i = Filled + Reserved;
		Reserved++;
		return i < _pos.Count ? _pos[i] : transform;
	}

	public void OnScrewArrived(Screw s)
	{
		Reserved = Mathf.Max(0, Reserved - 1);
		Filled++;
		_screws.Add(s);
		if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.ScrewInPlace);
		transform.DOKill(true);
		transform.DOPunchScale(Vector3.one * 0.14f, 0.22f, 6, 0.6f);
		transform.DOPunchRotation(new Vector3(0f, 0f, Filled % 2 == 0 ? 6f : -6f), 0.25f, 6, 0.6f);
		if (Filled >= MaxScrewCount) Close();
	}

	void Close()
	{
		IsClosing = true;
		OnMoving = true;
		if (_lidRoot != null)
		{
			_lidRoot.gameObject.SetActive(true);
			// Ốc được vẽ bằng TopCamera (lớp Top) nên luôn nằm trên sprite hộp; cho nắp lên cùng lớp và nhích về phía camera để che ốc.
			Screw.SetLayerRecursive(_lidRoot.gameObject, GameController.TopLayer);
			var cam = Camera.main;
			var toCam = cam != null ? -cam.transform.forward : Vector3.back;
			var target = _lidRoot.parent != null
				? _lidRoot.localPosition + _lidRoot.parent.InverseTransformVector(toCam * 1.2f)
				: _lidRoot.localPosition + toCam * 1.2f;
			_lidRoot.localPosition = target + Vector3.up * 2f;
			var baseScale = transform.localScale;
			var seq = DOTween.Sequence();
			seq.AppendInterval(0.12f);
			// nắp rơi xuống, nảy nhẹ
			seq.Append(_lidRoot.DOLocalMove(target, 0.22f).SetEase(Ease.OutBounce));
			seq.AppendCallback(() => { HideScrews(); if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.Match); if (VibrationManager.Instance != null) VibrationManager.Instance.Medium(); });
			// dẹt xuống rồi bật lên
			seq.Append(transform.DOScale(new Vector3(baseScale.x * 1.2f, baseScale.y * 0.82f, baseScale.z), 0.08f).SetEase(Ease.OutQuad));
			seq.Append(transform.DOScale(new Vector3(baseScale.x * 0.9f, baseScale.y * 1.15f, baseScale.z), 0.1f).SetEase(Ease.OutQuad));
			seq.Append(transform.DOScale(baseScale, 0.1f).SetEase(Ease.OutBack));
			seq.AppendInterval(0.12f);
			// bay vút lên, xoay nhẹ, thu nhỏ
			seq.Append(transform.DOMoveY(transform.position.y + 8f, 0.4f).SetEase(Ease.InBack));
			seq.Join(transform.DORotate(new Vector3(0f, 0f, 20f), 0.4f).SetEase(Ease.InQuad));
			seq.Join(transform.DOScale(baseScale * 0.7f, 0.4f).SetEase(Ease.InQuad));
			seq.SetLink(gameObject);
			seq.OnComplete(Finish);
		}
		else Finish();
	}

	void HideScrews()
	{
		foreach (var sc in _screws) if (sc != null) sc.gameObject.SetActive(false);
	}

	void Finish()
	{
		Completed?.Invoke(this);
		Destroy(gameObject);
	}

	void OnDestroy() => transform.DOKill();
}
