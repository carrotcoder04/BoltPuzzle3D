using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Hiển thị tim: số tim, đồng hồ hồi tim, trạng thái Full / vô hạn.</summary>
public class HeartHud : MonoBehaviour
{
	public string InventoryKey;
	public UnityEvent AutoUpdateEvent = new UnityEvent();

	TMP_Text _count, _time, _full;
	GameObject _infinite, _countGo;
	float _tick;

	void Awake()
	{
		_count = transform.FindComp<TMP_Text>("Text");
		_time = transform.FindComp<TMP_Text>("TimeText");
		_full = transform.FindComp<TMP_Text>("FullText");
		var inf = transform.FindDeep("InfiniteIcon");
		_infinite = inf != null ? inf.gameObject : null;
		_countGo = _count != null ? _count.gameObject : null;
		transform.SetActive("FreeHeart", false);
	}

	void OnEnable()
	{
		Managers.Ensure();
		if (Managers.Inventory != null) Managers.Inventory.OnHeartsChanged += Refresh;
		Refresh();
	}

	void OnDisable()
	{
		if (Managers.Inventory != null) Managers.Inventory.OnHeartsChanged -= Refresh;
	}

	void Update()
	{
		_tick += Time.unscaledDeltaTime;
		if (_tick < 0.5f) return;
		_tick = 0f;
		Refresh();
		AutoUpdateEvent?.Invoke();
	}

	void Refresh()
	{
		var inv = Managers.Inventory;
		if (inv == null) return;
		bool unlimited = inv.IsUnlimitedHeart;
		bool full = !unlimited && inv.Hearts >= InventoryManager.MaxHearts;
		if (_infinite != null) _infinite.SetActive(unlimited);
		if (_countGo != null) _countGo.SetActive(!unlimited);
		UIUtil.SetText(_count, inv.Hearts.ToString());
		if (_full != null) _full.gameObject.SetActive(full);
		if (_time != null)
		{
			_time.gameObject.SetActive(!full);
			var t = unlimited ? inv.UnlimitedHeartRemaining : inv.NextHeartRemaining;
			_time.text = UIUtil.ShortDuration(t);
		}
	}
}
