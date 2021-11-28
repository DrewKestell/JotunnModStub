using System;
using UnityEngine;

public class Beehive : MonoBehaviour, Hoverable, Interactable
{
	public string m_name = "";

	public Transform m_coverPoint;

	public Transform m_spawnPoint;

	public GameObject m_beeEffect;

	public float m_maxCover = 0.25f;

	[BitMask(typeof(Heightmap.Biome))]
	public Heightmap.Biome m_biome;

	public float m_secPerUnit = 10f;

	public int m_maxHoney = 4;

	public ItemDrop m_honeyItem;

	public EffectList m_spawnEffect = new EffectList();

	private ZNetView m_nview;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (m_nview.GetZDO() != null)
		{
			if (m_nview.IsOwner() && m_nview.GetZDO().GetLong("lastTime", 0L) == 0L)
			{
				m_nview.GetZDO().Set("lastTime", ZNet.instance.GetTime().Ticks);
			}
			m_nview.Register("Extract", RPC_Extract);
			InvokeRepeating("UpdateBees", 0f, 10f);
		}
	}

	public string GetHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
		{
			return Localization.instance.Localize(m_name + "\n$piece_noaccess");
		}
		int honeyLevel = GetHoneyLevel();
		if (honeyLevel > 0)
		{
			return Localization.instance.Localize(m_name + " ( " + m_honeyItem.m_itemData.m_shared.m_name + " x " + honeyLevel + " )\n[<color=yellow><b>$KEY_Use</b></color>] $piece_beehive_extract");
		}
		return Localization.instance.Localize(m_name + " ( $piece_container_empty )\n[<color=yellow><b>$KEY_Use</b></color>] $piece_beehive_check");
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position))
		{
			return true;
		}
		if (GetHoneyLevel() > 0)
		{
			Extract();
		}
		else
		{
			if (!CheckBiome())
			{
				character.Message(MessageHud.MessageType.Center, "$piece_beehive_area");
				return true;
			}
			if (!HaveFreeSpace())
			{
				character.Message(MessageHud.MessageType.Center, "$piece_beehive_freespace");
				return true;
			}
			if (!EnvMan.instance.IsDaylight())
			{
				character.Message(MessageHud.MessageType.Center, "$piece_beehive_sleep");
				return true;
			}
			character.Message(MessageHud.MessageType.Center, "$piece_beehive_happy");
		}
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void Extract()
	{
		m_nview.InvokeRPC("Extract");
	}

	private void RPC_Extract(long caller)
	{
		int honeyLevel = GetHoneyLevel();
		if (honeyLevel > 0)
		{
			m_spawnEffect.Create(m_spawnPoint.position, Quaternion.identity);
			for (int i = 0; i < honeyLevel; i++)
			{
				Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.5f;
				Vector3 position = m_spawnPoint.position + new Vector3(vector.x, 0.25f * (float)i, vector.y);
				UnityEngine.Object.Instantiate(m_honeyItem, position, Quaternion.identity);
			}
			ResetLevel();
		}
	}

	private float GetTimeSinceLastUpdate()
	{
		DateTime dateTime = new DateTime(m_nview.GetZDO().GetLong("lastTime", ZNet.instance.GetTime().Ticks));
		DateTime time = ZNet.instance.GetTime();
		TimeSpan timeSpan = time - dateTime;
		m_nview.GetZDO().Set("lastTime", time.Ticks);
		double num = timeSpan.TotalSeconds;
		if (num < 0.0)
		{
			num = 0.0;
		}
		return (float)num;
	}

	private void ResetLevel()
	{
		m_nview.GetZDO().Set("level", 0);
	}

	private void IncreseLevel(int i)
	{
		int honeyLevel = GetHoneyLevel();
		honeyLevel += i;
		honeyLevel = Mathf.Clamp(honeyLevel, 0, m_maxHoney);
		m_nview.GetZDO().Set("level", honeyLevel);
	}

	private int GetHoneyLevel()
	{
		return m_nview.GetZDO().GetInt("level");
	}

	private void UpdateBees()
	{
		bool flag = CheckBiome() && HaveFreeSpace();
		bool active = flag && EnvMan.instance.IsDaylight();
		m_beeEffect.SetActive(active);
		if (m_nview.IsOwner() && flag)
		{
			float timeSinceLastUpdate = GetTimeSinceLastUpdate();
			float @float = m_nview.GetZDO().GetFloat("product");
			@float += timeSinceLastUpdate;
			if (@float > m_secPerUnit)
			{
				int i = (int)(@float / m_secPerUnit);
				IncreseLevel(i);
				@float = 0f;
			}
			m_nview.GetZDO().Set("product", @float);
		}
	}

	private bool HaveFreeSpace()
	{
		Cover.GetCoverForPoint(m_coverPoint.position, out var coverPercentage, out var _);
		return coverPercentage < m_maxCover;
	}

	private bool CheckBiome()
	{
		return (Heightmap.FindBiome(base.transform.position) & m_biome) != 0;
	}
}
