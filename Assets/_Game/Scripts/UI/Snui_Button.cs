using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Snui
{
	public enum PreventClickType
	{
		None = 0,
		Self = 1,
		Global = 2
	}

	/// <summary>
	/// Hiệu ứng & âm thanh/rung cho nút bấm. Đặt cùng GameObject với UnityEngine.UI.Button.
	/// Chống bấm liên tục (PreventClickType) bằng cách tạm khóa Button.interactable.
	/// </summary>
	[DisallowMultipleComponent]
	public class Snui_Button : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
	{
		public UnityEvent OnPointerDownEvent = new UnityEvent();
		public UnityEvent OnPointerUpEvent = new UnityEvent();

		public bool EnableClickAudio = true;
		public bool EnableVibration = true;
		public PreventClickType PreventClickType = PreventClickType.None;
		public float PreventDuration = 0.2f;

		public bool UsePressDownEffect;
		public float DownOffset = 4f;
		public float ClickScale = 1f;
		public float ClickAlpha = 1f;
		public float DisableAlpha = 1f;

		public static string ClickSound = "click";

		static float _globalBlockUntil;

		Button _button;
		CanvasGroup _canvasGroup;
		RectTransform _content;
		Vector3 _baseScale = Vector3.one;
		Vector2 _contentBasePos;
		float _selfBlockUntil;
		bool _pressed;

		public Button Button => _button;

		void Awake()
		{
			_button = GetComponent<Button>();
			_baseScale = transform.localScale;
			// Một số nút có con "Content" để dịch xuống khi nhấn
			var c = transform.Find("Content");
			_content = c != null ? c as RectTransform : null;
			if (_content != null) _contentBasePos = _content.anchoredPosition;
			if (_button != null) _button.onClick.AddListener(HandleClick);
		}

		void OnDestroy()
		{
			if (_button != null) _button.onClick.RemoveListener(HandleClick);
			transform.DOKill();
		}

		void OnEnable()
		{
			_pressed = false;
			transform.localScale = _baseScale;
			if (_content != null) _content.anchoredPosition = _contentBasePos;
			RefreshInteractableAlpha();
		}

		bool IsInteractable => _button == null || _button.IsInteractable();

		public void OnPointerDown(PointerEventData eventData)
		{
			if (!IsInteractable || IsBlocked()) return;
			_pressed = true;
			if (!Mathf.Approximately(ClickScale, 1f))
			{
				transform.DOKill();
				transform.DOScale(_baseScale * ClickScale, 0.08f).SetUpdate(true);
			}
			if (UsePressDownEffect && _content != null)
				_content.anchoredPosition = _contentBasePos + Vector2.down * DownOffset;
			SetAlpha(ClickAlpha);
			OnPointerDownEvent?.Invoke();
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if (!_pressed) return;
			Release();
			OnPointerUpEvent?.Invoke();
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (_pressed) Release();
		}

		void Release()
		{
			_pressed = false;
			if (!Mathf.Approximately(ClickScale, 1f))
			{
				transform.DOKill();
				transform.DOScale(_baseScale, 0.12f).SetEase(Ease.OutBack).SetUpdate(true);
			}
			if (_content != null) _content.anchoredPosition = _contentBasePos;
			RefreshInteractableAlpha();
		}

		void HandleClick()
		{
			if (EnableClickAudio && AudioManager.Instance != null) AudioManager.Instance.PlaySfx(ClickSound);
			if (EnableVibration && VibrationManager.Instance != null) VibrationManager.Instance.Light();
			StartBlock();
		}

		bool IsBlocked()
		{
			float now = Time.unscaledTime;
			return now < _selfBlockUntil || (PreventClickType == PreventClickType.Global && now < _globalBlockUntil);
		}

		void StartBlock()
		{
			if (PreventClickType == PreventClickType.None || PreventDuration <= 0f || _button == null) return;
			float until = Time.unscaledTime + PreventDuration;
			_selfBlockUntil = until;
			if (PreventClickType == PreventClickType.Global) _globalBlockUntil = until;
			_button.interactable = false;
			DOVirtual.DelayedCall(PreventDuration, () => { if (_button != null) _button.interactable = true; }, true).SetLink(gameObject);
		}

		/// <summary>Bật/tắt nút kèm alpha DisableAlpha.</summary>
		public void SetInteractable(bool value)
		{
			if (_button != null) _button.interactable = value;
			RefreshInteractableAlpha();
		}

		void RefreshInteractableAlpha() => SetAlpha(IsInteractable ? 1f : DisableAlpha);

		void SetAlpha(float a)
		{
			if (Mathf.Approximately(a, 1f) && _canvasGroup == null) return;
			if (_canvasGroup == null) _canvasGroup = gameObject.GetOrAdd<CanvasGroup>();
			_canvasGroup.alpha = a;
		}

		/// <summary>Đăng ký click ngắn gọn từ code.</summary>
		public void AddClick(Action action)
		{
			if (_button == null) _button = GetComponent<Button>();
			if (_button != null) _button.onClick.AddListener(() => action());
		}
	}
}
