using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Snui
{
	/// <summary>
	/// Khung Dialog toàn màn hình: nền tối (Image trên root) + SafeArea + một DialogContent.
	/// Mở bằng DialogManager.Show(prefab). Đóng khi content trả kết quả (CloseOnContentResult) hoặc gọi Close().
	/// </summary>
	public class Dialog : MonoBehaviour
	{
		public bool EnterAnim = true;
		public bool ExitAnim = true;
		public float FadeInOutDuration = 0.03f;
		public bool CloseOnContentResult = true;

		/// <summary>Gọi khi dialog đã đóng, kèm kết quả cuối của content.</summary>
		public event Action<Dialog, int> Closed;

		public DialogContent Content { get; private set; }
		public int Result { get; private set; } = -1;
		public bool IsClosing { get; private set; }

		Image _dim;
		float _dimAlpha;
		Button _bgBtn;

		protected virtual void Awake()
		{
			_dim = GetComponent<Image>();
			if (_dim != null) _dimAlpha = _dim.color.a;
			Content = GetComponentInChildren<DialogContent>(true);
			if (Content != null) Content.Dialog = this;
			var bg = transform.Find("SafeArea/BgBtn");
			if (bg != null) _bgBtn = bg.GetComponent<Button>();
			if (_bgBtn != null) _bgBtn.onClick.AddListener(OnBackgroundClicked);
		}

		protected virtual void OnEnable()
		{
			if (EnterAnim && _dim != null)
			{
				SetDimAlpha(0f);
				_dim.DOKill();
				_dim.DOFade(_dimAlpha, Mathf.Max(FadeInOutDuration, 0.15f)).SetUpdate(true);
			}
			DialogManager.Register(this);
		}

		protected virtual void OnDisable() => DialogManager.Unregister(this);

		protected virtual void OnDestroy()
		{
			if (_dim != null) _dim.DOKill();
		}

		/// <summary>Bấm nền: mặc định đóng như nút Close.</summary>
		protected virtual void OnBackgroundClicked()
		{
			if (Content != null && Content.IsShowing) Content.Close(-1);
			else Close(-1);
		}

		/// <summary>Nút Back của Android.</summary>
		public virtual void OnBackPressed() => OnBackgroundClicked();

		internal void OnContentResult(DialogContent content, int result)
		{
			Result = result;
			if (CloseOnContentResult) Close(result);
		}

		public void Close(int result = -1)
		{
			if (IsClosing) return;
			IsClosing = true;
			Result = result;
			if (ExitAnim && _dim != null)
			{
				_dim.DOKill();
				_dim.DOFade(0f, Mathf.Max(FadeInOutDuration, 0.15f)).SetUpdate(true).OnComplete(Finish);
			}
			else Finish();
		}

		void Finish()
		{
			Closed?.Invoke(this, Result);
			Destroy(gameObject);
		}

		void SetDimAlpha(float a)
		{
			var c = _dim.color;
			c.a = a;
			_dim.color = c;
		}
	}
}
