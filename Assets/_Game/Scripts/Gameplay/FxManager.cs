using UnityEngine;

/// <summary>
/// Hiệu ứng hạt dùng chung, tạo lúc chạy: mỗi loại là một ParticleSystem world-space (lớp Top) được tái sử dụng,
/// chỉ phát bằng Emit() nên không tốn chi phí tạo/hủy object trong lúc chơi.
/// </summary>
public static class FxManager
{
	enum Kind { Sparkle, Soft, Ring, Confetti, Smoke, Count }

	static readonly ParticleSystem[] _systems = new ParticleSystem[(int)Kind.Count];
	static Transform _root;

	static Vector3 ToCam
	{
		get { var c = Camera.main; return c != null ? -c.transform.forward : Vector3.back; }
	}

	/// <summary>Ốc vừa được rút ra: tia lấp lánh màu ốc + quầng sáng mềm.</summary>
	public static void ScrewPop(Vector3 pos, Color c)
	{
		pos += ToCam * 0.6f;
		Emit(Kind.Soft, pos, 1, Color.Lerp(c, Color.white, 0.35f), 0f, 0f, 1.6f, 2.0f, 0.28f, 0.32f);
		Emit(Kind.Sparkle, pos, 10, Color.Lerp(c, Color.white, 0.25f), 2.5f, 4.5f, 0.25f, 0.45f, 0.35f, 0.55f);
	}

	/// <summary>Ốc cắm vào lỗ: vòng sóng lan ra + vài tia nhỏ.</summary>
	public static void ScrewLand(Vector3 pos, Color c)
	{
		pos += ToCam * 0.4f;
		Emit(Kind.Ring, pos, 1, Color.Lerp(c, Color.white, 0.5f), 0f, 0f, 1.3f, 1.3f, 0.3f, 0.3f);
		Emit(Kind.Sparkle, pos, 6, Color.Lerp(c, Color.white, 0.5f), 1.2f, 2.4f, 0.15f, 0.28f, 0.25f, 0.4f);
	}

	/// <summary>Hộp đầy và đóng nắp: pháo giấy màu hộp, tia sáng và vòng sóng lớn.</summary>
	public static void BoxComplete(Vector3 pos, Color c)
	{
		pos += ToCam * 1.6f;
		Emit(Kind.Ring, pos, 1, Color.Lerp(c, Color.white, 0.4f), 0f, 0f, 3.2f, 3.2f, 0.4f, 0.4f);
		Emit(Kind.Soft, pos, 1, Color.Lerp(c, Color.white, 0.5f), 0f, 0f, 3.0f, 3.4f, 0.35f, 0.4f);
		Emit(Kind.Sparkle, pos, 16, Color.Lerp(c, Color.white, 0.3f), 3f, 6f, 0.3f, 0.55f, 0.45f, 0.7f);
		Emit(Kind.Confetti, pos, 26, c, 4f, 8f, 0.18f, 0.32f, 0.9f, 1.3f, true);
	}

	/// <summary>Tấm rơi khỏi mô hình: làn khói xám nhẹ.</summary>
	public static void BoardFall(Vector3 pos)
	{
		pos += ToCam * 0.5f;
		Emit(Kind.Smoke, pos, 8, new Color(0.9f, 0.9f, 0.95f, 0.55f), 0.8f, 1.8f, 0.8f, 1.4f, 0.5f, 0.8f);
	}

	/// <summary>Thắng màn: pháo giấy bắn từ hai góc dưới màn hình lên.</summary>
	public static void WinConfetti()
	{
		var cam = Camera.main;
		if (cam == null) return;
		float h = cam.orthographic ? cam.orthographicSize : 10f;
		float w = h * cam.aspect;
		var center = cam.transform.position + cam.transform.forward * 15f;
		var right = cam.transform.right;
		var up = cam.transform.up;
		var colors = GameConfig.Instance != null ? GameConfig.Instance.ScrewColors : null;
		for (int side = -1; side <= 1; side += 2)
		{
			var p = center + right * (w * 0.9f * side) - up * (h * 0.85f);
			var dir = (up * 2.2f - right * side).normalized;
			for (int i = 0; i < 45; i++)
			{
				var c = colors != null && colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Random.ColorHSV(0f, 1f, 0.6f, 0.9f, 0.9f, 1f);
				var v = Quaternion.AngleAxis(Random.Range(-18f, 18f), cam.transform.forward) * dir * Random.Range(14f, 24f);
				EmitOne(Kind.Confetti, p, v, c, Random.Range(0.3f, 0.5f), Random.Range(1.6f, 2.4f));
			}
		}
		Emit(Kind.Sparkle, center + up * (h * 0.2f), 30, Color.white, 4f, 10f, 0.4f, 0.8f, 0.6f, 1.0f);
	}

	// ---------------------------------------------------------------------------------------

	static void Emit(Kind k, Vector3 pos, int count, Color c, float speedMin, float speedMax,
		float sizeMin, float sizeMax, float lifeMin, float lifeMax, bool upward = false)
	{
		var ps = Get(k);
		if (ps == null) return;
		var cam = Camera.main;
		var up = cam != null ? cam.transform.up : Vector3.up;
		for (int i = 0; i < count; i++)
		{
			var dir = Random.onUnitSphere;
			dir -= ToCam * Vector3.Dot(dir, ToCam) * 0.7f; // tỏa chủ yếu trên mặt phẳng màn hình
			if (upward) dir = (dir + up * 1.2f).normalized;
			var p = new ParticleSystem.EmitParams
			{
				position = pos,
				velocity = dir.normalized * Random.Range(speedMin, speedMax),
				startColor = c,
				startSize = Random.Range(sizeMin, sizeMax),
				startLifetime = Random.Range(lifeMin, lifeMax),
				rotation = Random.Range(0f, 360f),
				angularVelocity = k == Kind.Confetti ? Random.Range(-540f, 540f) : 0f,
				applyShapeToPosition = false
			};
			ps.Emit(p, 1);
		}
	}

	static void EmitOne(Kind k, Vector3 pos, Vector3 vel, Color c, float size, float life)
	{
		var ps = Get(k);
		if (ps == null) return;
		ps.Emit(new ParticleSystem.EmitParams
		{
			position = pos, velocity = vel, startColor = c, startSize = size, startLifetime = life,
			rotation = Random.Range(0f, 360f), angularVelocity = Random.Range(-540f, 540f)
		}, 1);
	}

	static ParticleSystem Get(Kind k)
	{
		var ps = _systems[(int)k];
		if (ps != null) return ps;
		if (_root == null)
		{
			var go = new GameObject("[FX]");
			go.layer = GameController.TopLayer;
			_root = go.transform;
		}
		string mat;
		switch (k)
		{
			case Kind.Sparkle: mat = "FX/FX_Sparkle"; break;
			case Kind.Ring: mat = "FX/FX_Ring"; break;
			case Kind.Confetti: mat = "FX/FX_Confetti"; break;
			default: mat = "FX/FX_Soft"; break;
		}
		var m = Resources.Load<Material>(mat);
		if (m == null) return null;
		var obj = new GameObject("FX_" + k);
		obj.layer = GameController.TopLayer;
		obj.transform.SetParent(_root, false);
		ps = obj.AddComponent<ParticleSystem>();
		ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

		var main = ps.main;
		main.playOnAwake = false;
		main.loop = false;
		main.simulationSpace = ParticleSystemSimulationSpace.World;
		main.maxParticles = k == Kind.Confetti ? 300 : 150;
		main.scalingMode = ParticleSystemScalingMode.Hierarchy;
		main.gravityModifier = k == Kind.Confetti ? 1.6f : (k == Kind.Smoke ? -0.08f : 0f);

		var em = ps.emission; em.enabled = false;
		var shape = ps.shape; shape.enabled = false;

		var size = ps.sizeOverLifetime;
		size.enabled = true;
		switch (k)
		{
			case Kind.Ring: size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.25f, 1f, 1f)); break;
			case Kind.Soft: size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.8f))); break;
			case Kind.Smoke: size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.4f)); break;
			case Kind.Confetti: size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0f))); break;
			default: size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0f))); break;
		}

		var col = ps.colorOverLifetime;
		col.enabled = true;
		var g = new Gradient();
		g.SetKeys(
			new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
			k == Kind.Confetti
				? new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) }
				: new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
		col.color = g;

		if (k == Kind.Sparkle || k == Kind.Confetti || k == Kind.Smoke)
		{
			var lv = ps.limitVelocityOverLifetime;
			lv.enabled = true;
			lv.limit = k == Kind.Confetti ? 40f : 100f;
			lv.drag = k == Kind.Confetti ? 1.2f : 3.5f;
			lv.multiplyDragByParticleSize = false;
			lv.multiplyDragByParticleVelocity = k == Kind.Confetti;
		}
		if (k == Kind.Confetti)
		{
			// lá giấy lật lật khi rơi
			var rot = ps.rotationOverLifetime; rot.enabled = true;
			rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
			var noise = ps.noise; noise.enabled = true;
			noise.strength = 1.2f; noise.frequency = 0.6f; noise.quality = ParticleSystemNoiseQuality.Low;
		}

		var r = obj.GetComponent<ParticleSystemRenderer>();
		r.renderMode = ParticleSystemRenderMode.Billboard;
		r.sharedMaterial = m;
		r.sortingOrder = 100;
		r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		r.receiveShadows = false;
		r.maxParticleSize = 2f;

		ps.Play();
		_systems[(int)k] = ps;
		return ps;
	}
}
