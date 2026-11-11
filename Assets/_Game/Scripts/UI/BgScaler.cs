using UnityEngine;

/// <summary>Phóng ảnh nền để luôn phủ kín Canvas cha (giữ tỉ lệ, kiểu "cover").</summary>
[RequireComponent(typeof(RectTransform))]
public class BgScaler : MonoBehaviour
{
	RectTransform _rect;
	RectTransform _parent;
	Vector2 _baseSize;
	Vector2 _lastParentSize;

	void Awake()
	{
		_rect = (RectTransform)transform;
		_parent = _rect.parent as RectTransform;
		_baseSize = _rect.rect.size;
	}

	void Start() => Fit();

	void LateUpdate()
	{
		if (_parent != null && _parent.rect.size != _lastParentSize) Fit();
	}

	void Fit()
	{
		if (_parent == null || _baseSize.x <= 0f || _baseSize.y <= 0f) return;
		_lastParentSize = _parent.rect.size;
		float s = Mathf.Max(_lastParentSize.x / _baseSize.x, _lastParentSize.y / _baseSize.y);
		_rect.localScale = new Vector3(s, s, 1f);
	}
}
