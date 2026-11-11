using Snui;
using UnityEngine;

/// <summary>Xoay mô hình quanh trục nhìn của camera bằng cách vặn 2 ngón.</summary>
public class TwoFingerRotateGesture : MonoBehaviour
{
	[SerializeField] Camera _camera;
	public float RotateSpeed = 1.5f;
	public bool IsEnableWhenPanelShow;

	float _lastAngle;
	bool _active;

	void Update()
	{
		if ((!IsEnableWhenPanelShow && DialogManager.Count > 0) || Input.touchCount != 2) { _active = false; return; }
		var v = Input.GetTouch(1).position - Input.GetTouch(0).position;
		float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
		if (_active)
		{
			float d = Mathf.DeltaAngle(_lastAngle, angle);
			var cam = _camera != null ? _camera : Camera.main;
			var axis = cam != null ? cam.transform.forward : Vector3.forward;
			transform.Rotate(axis, -d * RotateSpeed, Space.World);
		}
		_lastAngle = angle;
		_active = true;
	}
}
