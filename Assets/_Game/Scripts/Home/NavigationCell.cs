using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Snui
{
	/// <summary>Nút tab ở thanh điều hướng dưới.</summary>
	public class NavigationCell : MonoBehaviour
	{
		public float IconNormalPosition = -65.8f;
		public float IconSelectPosition = -19.6f;
		public float SelectedScale = 0.8f;
		public float UnselectedScale = 0.6f;
		[SerializeField] Sprite _normalSprite;
		[SerializeField] Sprite _selectedSprite;
		[SerializeField] Component _icon;
		[SerializeField] Component _text;
		[SerializeField] Component _line;

		RectTransform _iconRt;
		Image _iconImg;
		GameObject _textGo;

		public Button Button { get; private set; }

		void Awake()
		{
			Button = GetComponent<Button>();
			var icon = _icon != null ? _icon.transform : transform.Find("Icon");
			_iconRt = icon as RectTransform;
			_iconImg = icon != null ? icon.GetComponent<Image>() : null;
			var text = _text != null ? _text.transform : transform.Find("Text");
			_textGo = text != null ? text.gameObject : null;
		}

		public void SetSelected(bool selected, bool animate = true)
		{
			float d = animate ? 0.2f : 0f;
			if (_iconRt != null)
			{
				_iconRt.DOKill();
				_iconRt.DOAnchorPosY(selected ? IconSelectPosition : IconNormalPosition, d);
				_iconRt.DOScale(selected ? SelectedScale : UnselectedScale, d);
			}
			if (_iconImg != null)
			{
				var s = selected ? _selectedSprite : _normalSprite;
				if (s != null) _iconImg.sprite = s;
			}
			if (_textGo != null) _textGo.SetActive(selected);
		}
	}
}
