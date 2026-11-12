using Snui;
using UnityEngine;

/// <summary>
/// Gốc DontDestroyOnLoad chứa toàn bộ manager (đặt trong SplashScene).
/// Khởi tạo theo thứ tự: UserData → Inventory → Vibration → Audio.
/// Nếu chạy thẳng Home/Game scene trong Editor, Managers.Ensure() sẽ tự tạo bản tối thiểu.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class Managers : MonoBehaviour
{
	public static Managers Instance { get; private set; }
	public static bool IsReady { get; private set; }

	public static UserDataManager UserData => UserDataManager.Instance;
	public static InventoryManager Inventory => InventoryManager.Instance;
	public static AudioManager Audio => AudioManager.Instance;
	public static VibrationManager Vibration => VibrationManager.Instance;

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);
		Application.targetFrameRate = 60;
		QualitySettings.vSyncCount = 0;
		// cấp sẵn bộ nhớ cho tween để không bị khựng khi nhiều ốc/hộp chạy hiệu ứng cùng lúc
		DG.Tweening.DOTween.Init(false, true, DG.Tweening.LogBehaviour.ErrorsOnly).SetCapacity(400, 100);
		Input.multiTouchEnabled = true;
		InitAll();
	}

	void InitAll()
	{
		Get<UserDataManager>().Init();
		Get<InventoryManager>().Init();
		Get<VibrationManager>().Init();
		Get<AudioManager>().Init();
		Get<FirebaseManager>().Init();
		Get<IapManager>().Init();
		IsReady = true;
	}

	T Get<T>() where T : Component => GetComponent<T>() ?? gameObject.AddComponent<T>();

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape)) DialogManager.HandleBack();
	}

	/// <summary>Đảm bảo có Managers khi Play trực tiếp từ HomeScene/GameScene.</summary>
	public static void Ensure()
	{
		if (Instance != null) return;
		var go = new GameObject("Managers");
		go.AddComponent<Managers>();
	}
}
