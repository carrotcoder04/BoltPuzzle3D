using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Phát hiệu ứng âm thanh (SFX). Clip được nạp theo tên từ Resources/Sfx
/// (Assets/_Game/Audio/Resources/Sfx/*.ogg) và cache lại.
/// </summary>
public class AudioManager : MonoBehaviour
{
	public static AudioManager Instance { get; private set; }

	const int PoolSize = 8;
	const string SfxFolder = "Sfx/";

	readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();
	readonly Dictionary<string, float> _lastPlay = new Dictionary<string, float>();
	AudioSource[] _pool;
	int _next;

	public bool SoundOn => UserDataManager.Instance == null || UserDataManager.Instance.SoundOn;

	public void Init()
	{
		Instance = this;
		_pool = new AudioSource[PoolSize];
		for (int i = 0; i < PoolSize; i++)
		{
			var s = gameObject.AddComponent<AudioSource>();
			s.playOnAwake = false;
			s.spatialBlend = 0f;
			_pool[i] = s;
		}
	}

	public AudioClip GetClip(string name)
	{
		if (string.IsNullOrEmpty(name)) return null;
		if (_cache.TryGetValue(name, out var c)) return c;
		c = Resources.Load<AudioClip>(SfxFolder + name);
		if (c == null) Debug.LogWarning("[Audio] Missing sfx: " + name);
		_cache[name] = c;
		return c;
	}

	/// <summary>Phát SFX theo tên file (không đuôi). minInterval chống phát chồng quá dày.</summary>
	public void PlaySfx(string name, float volume = 1f, float pitch = 1f, float minInterval = 0.03f)
	{
		if (!SoundOn) return;
		if (_lastPlay.TryGetValue(name, out var t) && Time.unscaledTime - t < minInterval) return;
		var clip = GetClip(name);
		if (clip == null) return;
		_lastPlay[name] = Time.unscaledTime;
		PlayClip(clip, volume, pitch);
	}

	public void PlayClip(AudioClip clip, float volume = 1f, float pitch = 1f)
	{
		if (!SoundOn || clip == null || _pool == null) return;
		var s = _pool[_next];
		_next = (_next + 1) % _pool.Length;
		s.pitch = pitch;
		s.PlayOneShot(clip, volume);
	}

	public void StopAll()
	{
		if (_pool == null) return;
		foreach (var s in _pool) s.Stop();
	}
}

/// <summary>Tên các SFX có sẵn trong Resources/Sfx.</summary>
public static class Sfx
{
	public const string Click = "click";
	public const string Screw = "screw";
	public const string ScrewPick = "screw_pick";
	public const string ScrewInPlace = "screw_in_place";
	public const string ScrewUnmoveable = "screw_unmoveable";
	public const string BoardOpen = "board_open";
	public const string BoardClose = "board_close";
	public const string Match = "match";
	public const string Win = "win";
	public const string Fail = "fail";
	public const string Revive = "revive";
	public const string ToolHammer = "tool_hammer";
	public const string ToolBroom = "tool_broom";
	public const string ToolDrill = "tool_drill";
	public const string ToolShow = "tool_show";
	public const string BigCoinGet = "big_coin_get";
	public const string Award = "award";
	public const string Whoosh = "whoosh";
	public const string HomeIn = "home_in";
}
