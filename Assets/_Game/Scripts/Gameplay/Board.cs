using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Tấm ván 3D. Được giữ bởi các Screw con. Khi hết ốc thì rơi tự do rồi tự hủy.
/// Các field serialized (BoardColor, Stage, Index, Type, UseJoint, IsLoose, Screws) giữ nguyên từ dữ liệu gốc.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Board : MonoBehaviour
{
	public Color BoardColor = Color.white;
	public int Stage;
	public int Index;
	public string Type;
	public bool UseJoint;
	public bool IsLoose;
	public List<Screw> Screws = new List<Screw>();

	public bool IsFallen { get; private set; }

	Rigidbody _rb;
	Collider _col;

	public Collider Collider => _col != null ? _col : (_col = GetComponent<Collider>());

	void Awake()
	{
		_rb = GetComponent<Rigidbody>();
		_col = GetComponent<Collider>();
		CollectScrews();
	}

	public void CollectScrews()
	{
		Screws.Clear();
		foreach (Transform t in transform)
		{
			var s = t.GetComponent<Screw>();
			if (s != null && s.gameObject.activeSelf) { Screws.Add(s); s.Board = this; }
		}
	}

	public int ScrewCount => Screws.Count;

	public void RemoveScrew(Screw s)
	{
		if (!Screws.Remove(s)) return;
		if (Screws.Count == 0) Fall();
		else if (Screws.Count == 1 && UseJoint) Wobble();
	}

	/// <summary>Bỏ ốc khỏi tấm mà không làm tấm rơi (dùng khi cân số ốc chia hết cho 3).</summary>
	public void DiscardScrew(Screw s)
	{
		if (!Screws.Remove(s)) return;
		s.gameObject.SetActive(false);
		Destroy(s.gameObject);
	}

	void Wobble()
	{
		transform.DOKill();
		transform.DOPunchRotation(new Vector3(4f, 0f, 10f), 0.5f, 7, 0.5f);
	}

	/// <summary>Cho tấm rơi: tắt collider để không cản tia kiểm tra / không vướng tấm khác.</summary>
	public void Fall(Transform dropRoot = null)
	{
		if (IsFallen) return;
		IsFallen = true;
		transform.DOKill();
		if (_col != null) _col.enabled = false;
		var gc = GameController.Instance;
		if (dropRoot == null && gc != null) dropRoot = gc.DropRoot;
		transform.SetParent(dropRoot, true);
		if (_rb != null)
		{
			_rb.isKinematic = false;
			_rb.useGravity = true;
			// bật lên và văng ra khỏi khối mô hình rồi mới rơi, để thấy rõ tấm vừa được tháo
			var center = gc != null && gc.GameRoot != null ? gc.GameRoot.position : Vector3.zero;
			var rend = GetComponent<Renderer>();
			var outDir = (rend != null ? rend.bounds.center : transform.position) - center;
			outDir.z = Mathf.Min(outDir.z, 0f) - 0.5f;
			outDir = outDir.sqrMagnitude > 1e-4f ? outDir.normalized : Vector3.back;
			_rb.linearVelocity = outDir * 4f + Vector3.up * Random.Range(4f, 6f);
			_rb.angularVelocity = Random.insideUnitSphere * 6f;
		}
		if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.BoardOpen);
		{
			var rb = GetComponent<Renderer>();
			FxManager.BoardFall(rb != null ? rb.bounds.center : transform.position);
		}
		transform.DOScale(transform.localScale * 0.6f, 0.6f).SetDelay(1.2f).SetEase(Ease.InQuad).SetLink(gameObject);
		Destroy(gameObject, 2.5f);
		if (gc != null) gc.OnBoardFallen(this);
	}

	/// <summary>Búa: đập vỡ tấm ngay lập tức.</summary>
	public void Smash()
	{
		transform.DOKill();
		transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 8, 0.5f);
	}
}
