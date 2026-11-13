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

	/// <summary>
	/// Cho tấm rơi: tắt collider (không cản tia kiểm tra / không vướng tấm khác) rồi rơi theo quỹ đạo tự viết
	/// tính theo khung camera, nên tốc độ trông giống nhau dù mô hình to hay nhỏ:
	/// bật nhẹ ra ngoài + về phía camera, nghiêng dần theo hướng văng, gia tốc rơi đều rồi tự hủy khi ra khỏi màn.
	/// </summary>
	public void Fall(Transform dropRoot = null)
	{
		if (IsFallen) return;
		IsFallen = true;
		transform.DOKill();
		if (_col != null) _col.enabled = false;
		var gc = GameController.Instance;
		if (dropRoot == null && gc != null) dropRoot = gc.DropRoot;
		transform.SetParent(dropRoot, true);
		if (_rb != null) { _rb.isKinematic = true; _rb.useGravity = false; }

		var rend = GetComponent<Renderer>();
		var pos = rend != null ? rend.bounds.center : transform.position;
		if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.BoardOpen);
		FxManager.BoardFall(pos);
		if (gc != null) gc.OnBoardFallen(this);
		StartCoroutine(FallRoutine(gc, pos));
	}

	System.Collections.IEnumerator FallRoutine(GameController gc, Vector3 pivot)
	{
		var cam = gc != null ? gc.MainCamera : Camera.main;
		var camT = cam != null ? cam.transform : null;
		Vector3 right = camT != null ? camT.right : Vector3.right;
		Vector3 up = camT != null ? camT.up : Vector3.up;
		Vector3 fwd = camT != null ? camT.forward : Vector3.forward;
		// đơn vị = nửa chiều cao khung nhìn, để chuyển động ăn theo kích thước màn chứ không theo scale mô hình
		float unit = cam != null && cam.orthographic ? cam.orthographicSize
			: cam != null ? Mathf.Max(1f, Vector3.Distance(camT.position, pivot) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad)) : 5f;

		// hướng văng: từ tâm mô hình ra chỗ tấm, chiếu lên mặt phẳng màn hình
		var center = gc != null && gc.GameRoot != null ? gc.GameRoot.position : Vector3.zero;
		var d = pivot - center;
		var side = new Vector2(Vector3.Dot(d, right), Vector3.Dot(d, up));
		if (side.sqrMagnitude < 1e-4f) side = new Vector2(Random.value < 0.5f ? -1f : 1f, 0.3f);
		side.Normalize();
		float sx = Mathf.Sign(side.x == 0f ? Random.value - 0.5f : side.x);

		// vận tốc đầu: bật lên nhẹ, dạt sang ngang theo phía của tấm, tiến về camera để không xuyên qua mô hình
		Vector3 vel = right * (sx * Random.Range(0.25f, 0.45f) * unit)
			+ up * (Random.Range(0.45f, 0.65f) * unit)
			- fwd * (0.5f * unit);
		Vector3 gravity = -up * (4.2f * unit);
		// xoay: lật ra phía ngoài (quanh trục dọc màn hình) + nghiêng nhẹ, chậm dần nhờ cản không khí
		Vector3 axis = (up * -sx * 0.6f + fwd * sx * 0.8f + Random.insideUnitSphere * 0.25f).normalized;
		float spin = Random.Range(140f, 220f);

		var t0 = Time.time;
		var startScale = transform.localScale;
		while (true)
		{
			float dt = Time.deltaTime;
			vel += gravity * dt;
			vel *= 1f - 0.15f * dt;                 // cản nhẹ cho đường rơi mềm
			transform.position += vel * dt;
			transform.Rotate(axis, spin * dt, Space.World);
			spin = Mathf.Lerp(spin, spin * 0.6f, dt);

			float age = Time.time - t0;
			bool offscreen = cam != null && cam.WorldToViewportPoint(transform.position).y < -0.35f;
			if (offscreen || age > 3f) break;
			if (age > 1.6f) transform.localScale = startScale * Mathf.Lerp(1f, 0.7f, (age - 1.6f) / 1.4f);
			yield return null;
		}
		Destroy(gameObject);
	}

	/// <summary>Búa: đập vỡ tấm ngay lập tức.</summary>
	public void Smash()
	{
		transform.DOKill();
		transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 8, 0.5f);
	}
}
