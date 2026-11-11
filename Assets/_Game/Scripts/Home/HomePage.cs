using UnityEngine;

/// <summary>Trang Home: nút Settings, HUD, nút Play, widget rương level. Ẩn các phần online (avatar, sự kiện).</summary>
public class HomePage : MonoBehaviour
{
	public bool IsHomeAnimationPlaying;
	public bool IsHomePlayBtnReady = true;
	public Component AnimationController;

	void Awake()
	{
		var safe = transform.Find("SafeArea") ?? transform;
		safe.SetActive("MyAvatar", false);
		safe.SetActive("HomeEvents", false);
		safe.BindButton("Setting", () => { if (HomeController.Instance != null) HomeController.Instance.OpenSettings(); });
		IsHomePlayBtnReady = true;
	}
}
