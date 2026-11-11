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
		transform.DOPunchRotation(new Vector3(0f, 0f, 6f), 0.4f, 6, 0.6f);
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
			_rb.linearVelocity = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(0.5f, 2f), Random.Range(-1f, 1f));
			_rb.angularVelocity = Random.insideUnitSphere * 4f;
		}
		if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.BoardOpen);
		Destroy(gameObject, 3f);
		if (gc != null) gc.OnBoardFallen(this);
	}

	/// <summary>Búa: đập vỡ tấm ngay lập tức.</summary>
	public void Smash()
	{
		transform.DOKill();
		transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 8, 0.5f);
	}
}
