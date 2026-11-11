using UnityEngine;
using UnityEngine.UI;

namespace Snui
{
	/// <summary>Tiện ích nhỏ cho Image: đổi sprite giữ nguyên kích thước gốc hoặc native size.</summary>
	[DisallowMultipleComponent]
	public class Snui_Image : MonoBehaviour
	{
		Image _image;

		public Image Image => _image != null ? _image : (_image = GetComponent<Image>());

		public void SetSprite(Sprite sprite, bool nativeSize = false)
		{
			if (Image == null) return;
			Image.sprite = sprite;
			if (nativeSize) Image.SetNativeSize();
		}

		public void SetGray(bool gray)
		{
			if (Image == null) return;
			Image.color = gray ? new Color(0.6f, 0.6f, 0.6f, Image.color.a) : new Color(1f, 1f, 1f, Image.color.a);
		}
	}
}
