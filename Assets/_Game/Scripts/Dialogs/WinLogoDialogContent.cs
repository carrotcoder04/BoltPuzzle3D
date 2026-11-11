using Snui;

/// <summary>Màn logo "Level Complete" + pháo hoa.</summary>
public class WinLogoDialogContent : DialogContent
{
	protected override void OnShown()
	{
		if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.Award);
	}

	// Không cho đóng sớm bằng nền
	protected override void OnCloseClicked() { }
}
