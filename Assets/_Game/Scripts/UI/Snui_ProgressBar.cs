using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Thanh tiến trình của bộ Snui. BarType 0: bar trượt trong mask; 1: co giãn chiều rộng bar.
/// TextType 0: "cur/max"; 1: phần trăm; 2: ẩn chữ.
/// </summary>
public class Snui_ProgressBar : MonoBehaviour
{
	public int BarType;
	public int TextType;
	[SerializeField] RectTransform _bar;
	[SerializeField] TMP_Text _text;
	[SerializeField] RectTransform _mask;

	float _full = -1f;
	Vector2 _barBase;
	float _value;

	void Cache()
	{
		if (_full >= 0f) return;
		var r = _mask != null ? _mask : _bar;
		_full = r != null ? r.rect.width : 0f;
		if (_bar != null) _barBase = _bar.anchoredPosition;
	}

	public void SetValue(int current, int max, float duration = 0f)
	{
		Cache();
		float target = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
		if (_text != null)
		{
			_text.gameObject.SetActive(TextType != 2);
			_text.text = TextType == 1 ? Mathf.RoundToInt(target * 100f) + "%" : $"{current}/{max}";
		}
		DOTween.Kill(this);
		if (duration > 0f) DOTween.To(() => _value, Apply, target, duration).SetTarget(this).SetUpdate(true);
		else Apply(target);
	}

	void Apply(float v)
	{
		_value = v;
		if (_bar == null) return;
		if (BarType == 1) _bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _full * v);
		else _bar.anchoredPosition = _barBase + Vector2.left * (_full * (1f - v));
	}

	void OnDestroy() => DOTween.Kill(this);
}
