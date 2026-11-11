using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Snui
{
	public enum DialogAnimType
	{
		None = 0,
		Scale = 1,
		Fade = 2,
		SlideUp = 3
	}

	/// <summary>
	/// Phần nội dung (bảng) của một Dialog. Các content cụ thể (PauseDialogContent, WinDialogContent...)
	/// kế thừa lớp này. Dialog cha điều khiển nền mờ; content tự chạy hiệu ứng vào/ra.
	/// </summary>
	public class DialogContent : MonoBehaviour
	{
		public bool PlayOnStart = true;
		public DialogAnimType EnterAnim = DialogAnimType.Scale;
		public DialogAnimType ExitAnim = DialogAnimType.Scale;

		public UnityEvent OnShow = new UnityEvent();
		public UnityEvent OnShowAfterHide = new UnityEvent();
		public UnityEvent OnHide = new UnityEvent();
		public UnityEvent OnDismiss = new UnityEvent();

		[SerializeField] protected Image _bg;
		[SerializeField] protected Snui_Text _title;
		[SerializeField] protected Button _closeBtn;

		public const float AnimDuration = 0.25f;

		/// <summary>Kết quả trả về khi content đóng (ví dụ: Continue / Home / Quit).</summary>
		public int Result { get; protected set; } = -1;

		public Dialog Dialog { get; internal set; }
		public bool IsShowing { get; private set; }

		CanvasGroup _group;
		RectTransform _rect;
		Vector2 _basePos;
		bool _started;

		protected CanvasGroup Group => _group != null ? _group : (_group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>());

		protected virtual void Awake()
		{
			_rect = transform as RectTransform;
			if (_rect != null) _basePos = _rect.anchoredPosition;
			if (Dialog == null) Dialog = GetComponentInParent<Dialog>(true);
			if (_closeBtn == null)
			{
				var t = transform.Find("CloseBtn");
				if (t != null) _closeBtn = t.GetComponent<Button>();
			}
			if (_closeBtn != null) _closeBtn.onClick.AddListener(OnCloseClicked);
		}

		protected virtual void Start()
		{
			_started = true;
			if (PlayOnStart) Show();
		}

		protected virtual void OnDestroy()
		{
			transform.DOKill();
			if (_group != null) _group.DOKill();
		}

		protected virtual void OnCloseClicked() => Close(-1);

		/// <summary>Hiện content kèm hiệu ứng vào.</summary>
		public virtual void Show(Action onComplete = null)
		{
			gameObject.SetActive(true);
			IsShowing = true;
			Group.interactable = false;
			OnShow?.Invoke();
			PlayAnim(EnterAnim, true, () =>
			{
				Group.interactable = true;
				OnShown();
				onComplete?.Invoke();
			});
		}

		/// <summary>Gọi sau khi hiệu ứng vào kết thúc.</summary>
		protected virtual void OnShown() { }

		/// <summary>Ẩn content (không đóng dialog) – dùng khi chuyển giữa các content.</summary>
		public virtual void Hide(Action onComplete = null)
		{
			IsShowing = false;
			Group.interactable = false;
			OnHide?.Invoke();
			PlayAnim(ExitAnim, false, () =>
			{
				gameObject.SetActive(false);
				onComplete?.Invoke();
			});
		}

		/// <summary>Đóng content với kết quả. Nếu Dialog cha bật CloseOnContentResult thì đóng luôn dialog.</summary>
		public virtual void Close(int result)
		{
			Result = result;
			Hide(() =>
			{
				OnDismiss?.Invoke();
				if (Dialog != null) Dialog.OnContentResult(this, result);
			});
		}

		protected void PlayAnim(DialogAnimType type, bool enter, Action done)
		{
			transform.DOKill();
			Group.DOKill();
			float d = AnimDuration;
			switch (type)
			{
				case DialogAnimType.Scale:
					if (enter)
					{
						transform.localScale = Vector3.one * 0.6f;
						Group.alpha = 0f;
						Group.DOFade(1f, d * 0.6f).SetUpdate(true);
						transform.DOScale(1f, d).SetEase(Ease.OutBack).SetUpdate(true).OnComplete(() => done?.Invoke());
					}
					else
					{
						Group.DOFade(0f, d * 0.8f).SetUpdate(true);
						transform.DOScale(0.7f, d * 0.8f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() => done?.Invoke());
					}
					break;
				case DialogAnimType.Fade:
					Group.alpha = enter ? 0f : Group.alpha;
					Group.DOFade(enter ? 1f : 0f, d).SetUpdate(true).OnComplete(() => done?.Invoke());
					break;
				case DialogAnimType.SlideUp:
					if (_rect == null) goto default;
					float h = Screen.height;
					if (enter)
					{
						Group.alpha = 1f;
						_rect.anchoredPosition = _basePos + Vector2.down * h;
						_rect.DOAnchorPos(_basePos, d * 1.4f).SetEase(Ease.OutCubic).SetUpdate(true).OnComplete(() => done?.Invoke());
					}
					else
					{
						_rect.DOAnchorPos(_basePos + Vector2.down * h, d).SetEase(Ease.InCubic).SetUpdate(true).OnComplete(() => done?.Invoke());
					}
					break;
				default:
					Group.alpha = enter ? 1f : 0f;
					transform.localScale = Vector3.one;
					done?.Invoke();
					break;
			}
		}
	}
}
