using System.Collections.Generic;
using UnityEngine;

/// <summary>Gốc của một prefab màn chơi. Chứa các Stage (PuzzlePrefabItem) → Board → Screw.</summary>
public class PuzzlePrefab : MonoBehaviour
{
	public List<Board> GetBoards() => new List<Board>(GetComponentsInChildren<Board>(true));
	public List<Screw> GetScrews() => new List<Screw>(GetComponentsInChildren<Screw>(true));

	/// <summary>Tâm hình học của toàn bộ renderer (để căn giữa khi xoay).</summary>
	public Bounds GetBounds()
	{
		var rs = GetComponentsInChildren<Renderer>();
		if (rs.Length == 0) return new Bounds(transform.position, Vector3.one);
		var b = rs[0].bounds;
		for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
		return b;
	}
}
