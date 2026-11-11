using TMPro;
using UnityEngine;

namespace Snui
{
	public enum SnuiFontType
	{
		Default = 0,
		Title = 1,
		Number = 2
	}

	/// <summary>
	/// Wrapper cho TextMeshProUGUI. Bản gốc có xử lý RTL (Ả Rập/Do Thái);
	/// bản rebuild chỉ hỗ trợ ngôn ngữ LTR nên AutoRTL được giữ lại cho dữ liệu nhưng không dùng.
	/// </summary>
	[DisallowMultipleComponent]
	public class Snui_Text : MonoBehaviour
	{
		public SnuiFontType FontType;
		public bool AutoRTL = true;
		public bool AutoAlignRTL;

		TMP_Text _tmp;

		public TMP_Text Tmp => _tmp != null ? _tmp : (_tmp = GetComponent<TMP_Text>());

		public string text
		{
			get => Tmp != null ? Tmp.text : string.Empty;
			set { if (Tmp != null) Tmp.text = value; }
		}

		public void SetText(string value) => text = value;

		public void SetText(int value) => text = value.ToString();

		public Color color
		{
			get => Tmp != null ? Tmp.color : Color.white;
			set { if (Tmp != null) Tmp.color = value; }
		}
	}
}
