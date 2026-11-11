using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Thanh tiến trình dạng Mask + Bar.
/// Style 0: Bar trượt trong Mask (Mask giữ nguyên). Style 1: Mask co giãn theo chiều ngang (Bar giữ nguyên).
/// Text (RectTransform) hiển thị "current/max" nếu có TMP_Text.
/// </summary>
public class ProgressBar : MonoBehaviour
{
	public RectTransform Mask;
	public RectTransform Bar;
	public RectTransform Text;
	public int Style;

	float _fullWidth = -1f;
	Vector2 _barBasePos;
	TMP_Text _tmp;
	float _value;

	public float Value => _value;

	void Awake() => Cache();

	void Cache()
	{
		if (_fullWidth >= 0f) return;
		var target = Style == 1 ? Mask : (Mask != null ? Mask : Bar);
		_fullWidth = target != null ? target.rect.width : 0f;
		if (Bar != null) _barBasePos = Bar.anchoredPosition;
		if (Text != null) _tmp = Text.GetComponent<TMP_Text>() ?? Text.GetComponentInChildren<TMP_Text>(true);
	}

	/// <summary>Đặt tiến trình 0..1 (tùy chọn chạy tween).</summary>
	public void SetProgress(float value, float duration = 0f)
	{
		Cache();
		value = Mathf.Clamp01(value);
		if (duration > 0f)
		{
			DOTween.Kill(this);
			DOTween.To(() => _value, Apply, value, duration).SetTarget(this).SetUpdate(true);
		}
		else Apply(value);
	}

	public void SetProgress(int current, int max, float duration = 0f)
	{
		SetText($"{current}/{max}");
		SetProgress(max > 0 ? (float)current / max : 0f, duration);
	}

	public void SetText(string s)
	{
		Cache();
		if (_tmp != null) _tmp.text = s;
	}

	void Apply(float v)
	{
		_value = v;
		if (Style == 1 && Mask != null)
		{
			Mask.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _fullWidth * v);
		}
		else if (Bar != null)
		{
			Bar.anchoredPosition = _barBasePos + Vector2.left * (_fullWidth * (1f - v));
		}
	}

	void OnDestroy() => DOTween.Kill(this);
}
