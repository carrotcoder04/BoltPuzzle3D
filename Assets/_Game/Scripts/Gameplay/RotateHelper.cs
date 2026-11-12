using System;
using Snui;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Xoay mô hình (GameRoot) bằng 1 ngón kéo, có quán tính. Chạm nhẹ (không vượt DeadZone) → sự kiện OnTap.
/// </summary>
public class RotateHelper : MonoBehaviour
{
	public bool Enable = true;
	public UnityEvent OnRotate = new UnityEvent();
	public float RotationSpeed = 3f;
	public float Damping = 10f;
	public float MinSpeed = 0.1f;
	public float AutoRotateSpeed = 1f;
	public float DeadZone = 10f;
	public float AxisAdsorbAngle = 10f;
	/// <summary>Độ "bám" khi kéo: càng lớn càng sát ngón tay, càng nhỏ càng mượt (lọc rung từ input).</summary>
	public float FollowSharpness = 22f;

	/// <summary>Chạm (tap) tại toạ độ màn hình.</summary>
	public event Action<Vector2> OnTap;

	bool _down;
	bool _dragging;
	bool _overUI;
	Vector2 _startPos;
	Vector2 _lastPos;
	Vector2 _velocity; // độ/giây quanh (trục dọc, trục ngang)
	Vector2 _pending;  // góc đã kéo nhưng chưa áp dụng (làm mượt)
	float _lastMoveTime;
	int _fingerId = -1;

	float DegPerPixel => 180f / Mathf.Max(1f, Screen.width) * (RotationSpeed / 3f) * 2f;

	void Update()
	{
		float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
		if (!Enable || DialogManager.Count > 0)
		{
			_down = _dragging = false;
			_velocity = _pending = Vector2.zero;
			return;
		}

		if (Input.touchCount >= 2)
		{
			// Hai ngón: nhường cho Zoom/TwoFingerRotate, huỷ tap
			_down = false;
			_dragging = false;
			_velocity = _pending = Vector2.zero;
			return;
		}

		bool pressed, held, released;
		Vector2 pos;
		ReadPointer(out pressed, out held, out released, out pos);

		if (pressed)
		{
			_down = true;
			_dragging = false;
			_startPos = _lastPos = pos;
			_velocity = Vector2.zero;
			_pending = Vector2.zero;
			_overUI = IsOverUI();
		}
		else if (held && _down && !_overUI)
		{
			if (!_dragging && (pos - _startPos).magnitude > DeadZone * Mathf.Max(1f, Screen.dpi / 160f) * 0.6f)
			{
				_dragging = true;
				_lastPos = pos; // không giật một phát bằng cả vùng chết
			}
			if (_dragging)
			{
				var deg = (pos - _lastPos) * DegPerPixel;
				_pending += deg;
				if (deg.sqrMagnitude > 0f)
				{
					// vận tốc lọc theo thời gian để quán tính khi thả tay ổn định
					var inst = deg / Mathf.Max(dt, 0.001f);
					_velocity = Vector2.Lerp(_velocity, inst, 1f - Mathf.Exp(-18f * dt));
					_lastMoveTime = Time.unscaledTime;
				}
				else if (Time.unscaledTime - _lastMoveTime > 0.08f)
				{
					_velocity = Vector2.Lerp(_velocity, Vector2.zero, 1f - Mathf.Exp(-20f * dt));
				}
			}
			_lastPos = pos;
		}
		else if (released && _down)
		{
			_down = false;
			if (!_overUI && !_dragging) OnTap?.Invoke(pos);
#if UNITY_EDITOR
			else if (_overUI) Debug.Log("[Tap] blocked by UI: " + _uiHitName);
#endif
			// ngón đứng yên trước khi nhấc thì không văng
			if (Time.unscaledTime - _lastMoveTime > 0.1f) _velocity = Vector2.zero;
			_dragging = false;
		}

		// Áp dụng phần góc còn chờ theo hàm mũ: bám ngón tay nhưng lọc rung khung hình.
		if (_pending.sqrMagnitude > 1e-6f)
		{
			var step = _pending * (1f - Mathf.Exp(-FollowSharpness * dt));
			_pending -= step;
			ApplyRotation(step);
		}

		// Quán tính sau khi thả tay
		if (!_down && _velocity.sqrMagnitude > 0f)
		{
			ApplyRotation(_velocity * dt);
			_velocity *= Mathf.Exp(-Damping * 0.45f * dt);
			if (_velocity.magnitude < MinSpeed * 10f) _velocity = Vector2.zero;
		}
	}

	void ApplyRotation(Vector2 deg)
	{
		if (deg.sqrMagnitude < 1e-6f) return;
		var cam = Camera.main;
		Vector3 up = cam != null ? cam.transform.up : Vector3.up;
		Vector3 right = cam != null ? cam.transform.right : Vector3.right;
		transform.Rotate(up, -deg.x, Space.World);
		transform.Rotate(right, deg.y, Space.World);
		OnRotate?.Invoke();
	}

	void ReadPointer(out bool pressed, out bool held, out bool released, out Vector2 pos)
	{
		pressed = held = released = false;
		pos = _lastPos;
		if (Input.touchCount == 1)
		{
			var t = Input.GetTouch(0);
			pos = t.position;
			_fingerId = t.fingerId;
			pressed = t.phase == TouchPhase.Began;
			held = t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary;
			released = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
			return;
		}
		_fingerId = -1;
		pos = Input.mousePosition;
		pressed = Input.GetMouseButtonDown(0);
		held = Input.GetMouseButton(0) && !pressed;
		released = Input.GetMouseButtonUp(0);
	}

	static readonly System.Collections.Generic.List<RaycastResult> _uiHits = new System.Collections.Generic.List<RaycastResult>();
	static PointerEventData _ped;
	string _uiHitName;

	/// <summary>Chỉ coi là "trên UI" khi phần tử UI trên cùng tại điểm chạm nhận được click (nút, dialog...).</summary>
	bool IsOverUI()
	{
		var es = EventSystem.current;
		if (es == null) return false;
		if (_ped == null) _ped = new PointerEventData(es);
		_ped.position = _startPos;
		_uiHits.Clear();
		es.RaycastAll(_ped, _uiHits);
		foreach (var r in _uiHits)
		{
			if (r.gameObject == null) continue;
			_uiHitName = r.gameObject.name;
			if (r.module is UnityEngine.UI.GraphicRaycaster)
				return ExecuteEvents.GetEventHandler<IPointerClickHandler>(r.gameObject) != null
					|| ExecuteEvents.GetEventHandler<IPointerDownHandler>(r.gameObject) != null;
		}
		return false;
	}
}
