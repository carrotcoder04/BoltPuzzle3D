using TMPro;
using UnityEngine;

/// <summary>
/// Áp viền / bóng / glow lên TextMeshPro bằng một material instance riêng,
/// để nhiều text dùng chung font asset nhưng có hiệu ứng khác nhau.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TMPSetRealtime : MonoBehaviour
{
	public Color OutlineColor = Color.black;
	public float OutlineWidth;
	public float FaceDilate;

	public bool EnableUnderlay;
	public Color UnderlayColor = new Color(0f, 0f, 0f, 0.5f);
	public float UnderlayOffsetX;
	public float UnderlayOffsetY;
	public float UnderlaySoftness;
	public float UnderlayDilate;

	public bool EnableGlow;
	public Color GlowColor = Color.cyan;
	public float GlowOffset;
	public float GlowInner = 0.05f;
	public float GlowOuter = 0.05f;
	public float GlowPower = 0.75f;

	TMP_Text _text;

	void Awake() => Apply();

	public void Apply()
	{
		if (_text == null) _text = GetComponent<TMP_Text>();
		if (_text == null || _text.font == null) return;

		// fontMaterial trả về instance riêng cho text này.
		var mat = _text.fontMaterial;
		if (mat == null) return;

		mat.SetColor(ShaderUtilities.ID_OutlineColor, OutlineColor);
		mat.SetFloat(ShaderUtilities.ID_OutlineWidth, OutlineWidth);
		mat.SetFloat(ShaderUtilities.ID_FaceDilate, FaceDilate);

		if (EnableUnderlay)
		{
			mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
			mat.SetColor(ShaderUtilities.ID_UnderlayColor, UnderlayColor);
			mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, UnderlayOffsetX);
			mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, UnderlayOffsetY);
			mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, UnderlaySoftness);
			mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, UnderlayDilate);
		}
		else mat.DisableKeyword(ShaderUtilities.Keyword_Underlay);

		if (EnableGlow)
		{
			mat.EnableKeyword(ShaderUtilities.Keyword_Glow);
			mat.SetColor(ShaderUtilities.ID_GlowColor, GlowColor);
			mat.SetFloat(ShaderUtilities.ID_GlowOffset, GlowOffset);
			mat.SetFloat(ShaderUtilities.ID_GlowInner, GlowInner);
			mat.SetFloat(ShaderUtilities.ID_GlowOuter, GlowOuter);
			mat.SetFloat(ShaderUtilities.ID_GlowPower, GlowPower);
		}
		else mat.DisableKeyword(ShaderUtilities.Keyword_Glow);

		_text.UpdateMeshPadding();
		_text.SetVerticesDirty();
	}
}
