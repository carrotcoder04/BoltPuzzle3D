using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Nạp puzzle theo level vào GameRoot, cân số ốc chia hết cho 3,
/// mô phỏng thứ tự tháo được rồi tô màu ốc + sinh hàng đợi màu hộp.
/// </summary>
public class GameCreator : MonoBehaviour
{
	public bool IsAllLoadFinished;
	public UnityEvent InitLoadFinishEvent = new UnityEvent();

	public PuzzlePrefab Puzzle { get; private set; }
	public List<Board> Boards { get; } = new List<Board>();
	public List<Screw> Screws { get; } = new List<Screw>();
	/// <summary>Thứ tự màu các hộp sẽ xuất hiện (mỗi hộp 3 ốc).</summary>
	public List<int> BoxQueue { get; } = new List<int>();

	const float RayLength = 60f;

	public PuzzlePrefab Create(int level, Transform root)
	{
		IsAllLoadFinished = false;
		var cfg = GameConfig.Instance;
		var prefab = cfg != null ? cfg.GetLevelPrefab(level) : null;
		if (prefab == null)
		{
			Debug.LogError("[GameCreator] Không có level prefab. Kiểm tra Resources/GameConfig.asset");
			return null;
		}

		var go = Instantiate(prefab, root, false);
		go.name = prefab.name;
		Puzzle = go.GetComponent<PuzzlePrefab>() ?? go.AddComponent<PuzzlePrefab>();
		SetLayerRecursive(go, 0);
		CenterOnRoot(go.transform, root);

		Boards.Clear();
		Screws.Clear();
		foreach (var b in go.GetComponentsInChildren<Board>(false))
		{
			b.CollectScrews();
			Boards.Add(b);
		}

		UseExactBoardColliders(go);
		TrimToMultipleOfThree();
		foreach (var b in Boards) Screws.AddRange(b.Screws);

		Physics.SyncTransforms();
		var order = SimulateRemovalOrder();
		AssignColors(order, level, cfg);

		IsAllLoadFinished = true;
		InitLoadFinishEvent?.Invoke();
		return Puzzle;
	}

	static void SetLayerRecursive(GameObject go, int layer)
	{
		go.layer = layer;
		foreach (Transform c in go.transform) SetLayerRecursive(c.gameObject, layer);
	}

	/// <summary>Dời puzzle để tâm bounds trùng tâm GameRoot (xoay quanh đúng giữa mô hình).</summary>
	static void CenterOnRoot(Transform puzzle, Transform root)
	{
		Physics.SyncTransforms();
		var rs = puzzle.GetComponentsInChildren<Renderer>();
		if (rs.Length == 0) return;
		var b = rs[0].bounds;
		for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
		puzzle.position += root.position - b.center;
	}

	/// <summary>
	/// Prefab gốc để MeshCollider của tấm ở dạng convex (bao lồi), nên một tấm hình lồng/khung
	/// chặn cả khoảng trống bên trong: ốc nhìn thì thoáng nhưng tia kiểm tra vẫn trúng.
	/// Tấm luôn kinematic khi còn ốc nên dùng được collider lưới chính xác (non-convex).
	/// </summary>
	static void UseExactBoardColliders(GameObject puzzle)
	{
		// mặt lưới một phía (kính, tấm mỏng) vẫn phải chặn tia từ phía sau
		Physics.queriesHitBackfaces = true;
		foreach (var b in puzzle.GetComponentsInChildren<Board>(false))
		{
			var rb = b.GetComponent<Rigidbody>();
			if (rb != null && !rb.isKinematic) continue;
			foreach (var mc in b.GetComponents<MeshCollider>())
				if (mc.sharedMesh != null) mc.convex = false;
		}
	}

	void TrimToMultipleOfThree()
	{
		int total = 0;
		foreach (var b in Boards) total += b.Screws.Count;
		int extra = total % 3;
		if (extra == 0) return;
		// Bỏ ốc ở các tấm còn >= 2 ốc (tấm nhiều ốc nhất trước) để tấm vẫn được giữ.
		var list = new List<Board>(Boards);
		list.Sort((a, c) => c.Screws.Count.CompareTo(a.Screws.Count));
		foreach (var b in list)
		{
			while (extra > 0 && b.Screws.Count >= 2)
			{
				b.DiscardScrew(b.Screws[b.Screws.Count - 1]);
				extra--;
			}
			if (extra == 0) break;
		}
	}

	/// <summary>Ốc có bị tấm khác (chưa rơi) chặn theo hướng rút không.</summary>
	public static bool IsBlocked(Screw s, HashSet<Board> ignore = null)
	{
		var dir = s.ExtractDir;
		var hits = Physics.RaycastAll(s.transform.position, dir, RayLength, ~0, QueryTriggerInteraction.Ignore);
		foreach (var h in hits)
		{
			var b = h.collider.GetComponentInParent<Board>();
			if (b == null || b == s.Board || b.IsFallen) continue;
			if (ignore != null && ignore.Contains(b)) continue;
			return true;
		}
		return false;
	}

	/// <summary>Mô phỏng tháo ốc: lần lượt chọn ngẫu nhiên một ốc không bị chặn.</summary>
	List<Screw> SimulateRemovalOrder()
	{
		var remaining = new List<Screw>(Screws);
		var left = new Dictionary<Board, int>();
		foreach (var b in Boards) left[b] = b.Screws.Count;
		var gone = new HashSet<Board>();
		var order = new List<Screw>(remaining.Count);
		var free = new List<Screw>();
		while (remaining.Count > 0)
		{
			free.Clear();
			foreach (var s in remaining) if (!IsBlocked(s, gone)) free.Add(s);
			Screw pick;
			if (free.Count > 0) pick = free[Random.Range(0, free.Count)];
			else pick = remaining[0]; // kẹt do hình học: lấy tạm để không treo
			remaining.Remove(pick);
			order.Add(pick);
			if (pick.Board != null && --left[pick.Board] <= 0) gone.Add(pick.Board);
		}
		return order;
	}

	void AssignColors(List<Screw> order, int level, GameConfig cfg)
	{
		BoxQueue.Clear();
		int n = order.Count;
		int boxes = n / 3;
		int k = cfg.ColorCountFor(n, level);

		// Dãy màu hộp: xoay vòng một hoán vị ngẫu nhiên, tránh 2 hộp liền nhau trùng màu.
		var palette = new List<int>();
		for (int i = 0; i < cfg.ScrewColors.Length; i++) palette.Add(i);
		Shuffle(palette);
		palette.RemoveRange(k, palette.Count - k);
		var bag = new List<int>();
		int last = -1;
		for (int i = 0; i < boxes; i++)
		{
			if (bag.Count == 0) { bag.AddRange(palette); Shuffle(bag); }
			int idx = 0;
			if (bag.Count > 1 && bag[0] == last) idx = 1;
			last = bag[idx];
			bag.RemoveAt(idx);
			BoxQueue.Add(last);
		}

		// Ốc thứ i theo thứ tự tháo mang màu hộp i/3, rồi trộn trong cửa sổ 9 ốc để tạo độ khó.
		var colors = new int[n];
		for (int i = 0; i < n; i++) colors[i] = BoxQueue[i / 3];
		const int window = 9;
		for (int start = 0; start < n; start += window)
		{
			int end = Mathf.Min(n, start + window);
			for (int i = end - 1; i > start; i--)
			{
				int j = Random.Range(start, i + 1);
				(colors[i], colors[j]) = (colors[j], colors[i]);
			}
		}
		for (int i = 0; i < n; i++) order[i].SetColor(colors[i], cfg.ScrewColors[colors[i]]);
	}

	static void Shuffle<T>(IList<T> list)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = Random.Range(0, i + 1);
			(list[i], list[j]) = (list[j], list[i]);
		}
	}
}
