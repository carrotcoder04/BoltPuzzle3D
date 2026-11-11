using Snui;
using UnityEngine;

/// <summary>Phóng to/thu nhỏ mô hình bằng 2 ngón (pinch) hoặc con lăn chuột. Scale trong [ZoomMin, ZoomMax] × scale gốc.</summary>
public class ZoomGesture : MonoBehaviour
{
	[SerializeField] Camera _camera;
	public float ZoomSpeed = 0.001f;
	public float ZoomMin = 0.8f;
	public float ZoomMax = 1.8f;
	public bool IsEnableWhenPanelShow;

	Vector3 _baseScale;
	float _zoom = 1f;
	float _lastDist = -1f;

	void Awake() => _baseScale = transform.localScale;

	void Update()
	{
		if (!IsEnableWhenPanelShow && DialogManager.Count > 0) { _lastDist = -1f; return; }
		float delta = 0f;
		if (Input.touchCount == 2)
		{
			float d = (Input.GetTouch(0).position - Input.GetTouch(1).position).magnitude;
			if (_lastDist > 0f) delta = (d - _lastDist) * ZoomSpeed * 2f;
			_lastDist = d;
		}
		else
		{
			_lastDist = -1f;
			float wheel = Input.mouseScrollDelta.y;
			if (Mathf.Abs(wheel) > 0.01f) delta = wheel * ZoomSpeed * 60f;
		}
		if (Mathf.Abs(delta) < 1e-5f) return;
		_zoom = Mathf.Clamp(_zoom + delta, ZoomMin, ZoomMax);
		transform.localScale = _baseScale * _zoom;
	}

	public void ResetZoom()
	{
		_zoom = 1f;
		transform.localScale = _baseScale;
	}
}
