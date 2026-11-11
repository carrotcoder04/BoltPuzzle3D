using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Nhạc nền dùng chung (DontDestroyOnLoad). Tự đổi bài theo scene:
/// HomeScene → home_bg, GameScene → game_bg. Tôn trọng cài đặt Music.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BgmSource : MonoBehaviour
{
	public static BgmSource Instance { get; private set; }

	const string HomeBgm = "home_bg";
	const string GameBgm = "game_bg";
	const float Volume = 0.6f;

	AudioSource _source;
	string _current;

	void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
		DontDestroyOnLoad(gameObject);
		_source = GetComponent<AudioSource>();
		_source.loop = true;
		_source.playOnAwake = false;
		SceneManager.activeSceneChanged += OnSceneChanged;
	}

	void Start()
	{
		if (UserDataManager.Instance != null) UserDataManager.Instance.OnSettingsChanged += Refresh;
		OnSceneChanged(default, SceneManager.GetActiveScene());
	}

	void OnDestroy()
	{
		if (Instance != this) return;
		SceneManager.activeSceneChanged -= OnSceneChanged;
		if (UserDataManager.Instance != null) UserDataManager.Instance.OnSettingsChanged -= Refresh;
	}

	void OnSceneChanged(Scene _, Scene next)
	{
		if (next.name == "GameScene") Play(GameBgm);
		else if (next.name == "HomeScene") Play(HomeBgm);
	}

	bool MusicOn => UserDataManager.Instance == null || UserDataManager.Instance.MusicOn;

	public void Play(string clipName)
	{
		if (_current == clipName && _source.isPlaying) return;
		_current = clipName;
		var clip = AudioManager.Instance != null ? AudioManager.Instance.GetClip(clipName) : Resources.Load<AudioClip>("Sfx/" + clipName);
		if (clip == null) return;
		_source.DOKill();
		_source.clip = clip;
		_source.volume = 0f;
		if (!MusicOn) return;
		_source.Play();
		_source.DOFade(Volume, 0.6f).SetUpdate(true);
	}

	void Refresh()
	{
		if (MusicOn)
		{
			if (!_source.isPlaying && _source.clip != null)
			{
				_source.Play();
				_source.DOKill();
				_source.DOFade(Volume, 0.4f).SetUpdate(true);
			}
		}
		else
		{
			_source.DOKill();
			_source.Stop();
		}
	}
}
