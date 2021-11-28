using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RandEventSystem : MonoBehaviour
{
	private static RandEventSystem m_instance;

	public float m_eventIntervalMin = 1f;

	public float m_eventChance = 25f;

	public float m_randomEventRange = 200f;

	private float m_eventTimer;

	private float m_sendTimer;

	public List<RandomEvent> m_events = new List<RandomEvent>();

	private RandomEvent m_randomEvent;

	private float m_forcedEventUpdateTimer;

	private RandomEvent m_forcedEvent;

	private RandomEvent m_activeEvent;

	private float m_tempSaveEventTimer;

	private string m_tempSaveRandomEvent;

	private float m_tempSaveRandomEventTime;

	private Vector3 m_tempSaveRandomEventPos;

	public static RandEventSystem instance => m_instance;

	private void Awake()
	{
		m_instance = this;
	}

	private void OnDestroy()
	{
		m_instance = null;
	}

	private void Start()
	{
		ZRoutedRpc.instance.Register<string, float, Vector3>("SetEvent", RPC_SetEvent);
	}

	private void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		UpdateForcedEvents(fixedDeltaTime);
		UpdateRandomEvent(fixedDeltaTime);
		if (m_forcedEvent != null)
		{
			m_forcedEvent.Update(ZNet.instance.IsServer(), m_forcedEvent == m_activeEvent, playerInArea: true, fixedDeltaTime);
		}
		if (m_randomEvent != null && ZNet.instance.IsServer())
		{
			bool playerInArea = IsAnyPlayerInEventArea(m_randomEvent);
			if (m_randomEvent.Update(server: true, m_randomEvent == m_activeEvent, playerInArea, fixedDeltaTime))
			{
				SetRandomEvent(null, Vector3.zero);
			}
		}
		if (m_forcedEvent != null)
		{
			SetActiveEvent(m_forcedEvent);
		}
		else if (m_randomEvent != null && (bool)Player.m_localPlayer)
		{
			if (IsInsideRandomEventArea(m_randomEvent, Player.m_localPlayer.transform.position))
			{
				SetActiveEvent(m_randomEvent);
			}
			else
			{
				SetActiveEvent(null);
			}
		}
		else
		{
			SetActiveEvent(null);
		}
	}

	private bool IsInsideRandomEventArea(RandomEvent re, Vector3 position)
	{
		if (position.y > 3000f)
		{
			return false;
		}
		return Utils.DistanceXZ(position, re.m_pos) < m_randomEventRange;
	}

	private void UpdateRandomEvent(float dt)
	{
		if (!ZNet.instance.IsServer())
		{
			return;
		}
		m_eventTimer += dt;
		if (m_eventTimer > m_eventIntervalMin * 60f)
		{
			m_eventTimer = 0f;
			if (Random.Range(0f, 100f) <= m_eventChance)
			{
				StartRandomEvent();
			}
		}
		m_sendTimer += dt;
		if (m_sendTimer > 2f)
		{
			m_sendTimer = 0f;
			SendCurrentRandomEvent();
		}
	}

	private void UpdateForcedEvents(float dt)
	{
		m_forcedEventUpdateTimer += dt;
		if (m_forcedEventUpdateTimer > 2f)
		{
			m_forcedEventUpdateTimer = 0f;
			string forcedEvent = GetForcedEvent();
			SetForcedEvent(forcedEvent);
		}
	}

	private void SetForcedEvent(string name)
	{
		if (m_forcedEvent != null && name != null && m_forcedEvent.m_name == name)
		{
			return;
		}
		if (m_forcedEvent != null)
		{
			if (m_forcedEvent == m_activeEvent)
			{
				SetActiveEvent(null, end: true);
			}
			m_forcedEvent.OnStop();
			m_forcedEvent = null;
		}
		RandomEvent @event = GetEvent(name);
		if (@event != null)
		{
			m_forcedEvent = @event.Clone();
			m_forcedEvent.OnStart();
		}
	}

	private string GetForcedEvent()
	{
		if (EnemyHud.instance != null)
		{
			Character activeBoss = EnemyHud.instance.GetActiveBoss();
			if (activeBoss != null && activeBoss.m_bossEvent.Length > 0)
			{
				return activeBoss.m_bossEvent;
			}
			string @event = EventZone.GetEvent();
			if (@event != null)
			{
				return @event;
			}
		}
		return null;
	}

	private void SendCurrentRandomEvent()
	{
		if (m_randomEvent != null)
		{
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SetEvent", m_randomEvent.m_name, m_randomEvent.m_time, m_randomEvent.m_pos);
		}
		else
		{
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SetEvent", "", 0f, Vector3.zero);
		}
	}

	private void RPC_SetEvent(long sender, string eventName, float time, Vector3 pos)
	{
		if (!ZNet.instance.IsServer())
		{
			if (m_randomEvent == null || m_randomEvent.m_name != eventName)
			{
				SetRandomEventByName(eventName, pos);
			}
			if (m_randomEvent != null)
			{
				m_randomEvent.m_time = time;
				m_randomEvent.m_pos = pos;
			}
		}
	}

	public void StartRandomEvent()
	{
		if (!ZNet.instance.IsServer())
		{
			return;
		}
		List<KeyValuePair<RandomEvent, Vector3>> possibleRandomEvents = GetPossibleRandomEvents();
		ZLog.Log("Possible events:" + possibleRandomEvents.Count);
		if (possibleRandomEvents.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<RandomEvent, Vector3> item in possibleRandomEvents)
		{
			ZLog.DevLog("Event " + item.Key.m_name);
		}
		KeyValuePair<RandomEvent, Vector3> keyValuePair = possibleRandomEvents[Random.Range(0, possibleRandomEvents.Count)];
		SetRandomEvent(keyValuePair.Key, keyValuePair.Value);
	}

	private RandomEvent GetEvent(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		foreach (RandomEvent @event in m_events)
		{
			if (@event.m_name == name && @event.m_enabled)
			{
				return @event;
			}
		}
		return null;
	}

	public void SetRandomEventByName(string name, Vector3 pos)
	{
		RandomEvent @event = GetEvent(name);
		SetRandomEvent(@event, pos);
	}

	public void ResetRandomEvent()
	{
		SetRandomEvent(null, Vector3.zero);
	}

	public bool HaveEvent(string name)
	{
		return GetEvent(name) != null;
	}

	private void SetRandomEvent(RandomEvent ev, Vector3 pos)
	{
		if (m_randomEvent != null)
		{
			if (m_randomEvent == m_activeEvent)
			{
				SetActiveEvent(null, end: true);
			}
			m_randomEvent.OnStop();
			m_randomEvent = null;
		}
		if (ev != null)
		{
			m_randomEvent = ev.Clone();
			m_randomEvent.m_pos = pos;
			m_randomEvent.OnStart();
			ZLog.Log("Random event set:" + ev.m_name);
			if ((bool)Player.m_localPlayer)
			{
				Player.m_localPlayer.ShowTutorial("randomevent");
			}
		}
		if (ZNet.instance.IsServer())
		{
			SendCurrentRandomEvent();
		}
	}

	private bool IsAnyPlayerInEventArea(RandomEvent re)
	{
		foreach (ZDO allCharacterZDO in ZNet.instance.GetAllCharacterZDOS())
		{
			if (IsInsideRandomEventArea(re, allCharacterZDO.GetPosition()))
			{
				return true;
			}
		}
		return false;
	}

	private List<KeyValuePair<RandomEvent, Vector3>> GetPossibleRandomEvents()
	{
		List<KeyValuePair<RandomEvent, Vector3>> list = new List<KeyValuePair<RandomEvent, Vector3>>();
		List<ZDO> allCharacterZDOS = ZNet.instance.GetAllCharacterZDOS();
		foreach (RandomEvent @event in m_events)
		{
			if (@event.m_enabled && @event.m_random && HaveGlobalKeys(@event))
			{
				List<Vector3> validEventPoints = GetValidEventPoints(@event, allCharacterZDOS);
				if (validEventPoints.Count != 0)
				{
					Vector3 value = validEventPoints[Random.Range(0, validEventPoints.Count)];
					list.Add(new KeyValuePair<RandomEvent, Vector3>(@event, value));
				}
			}
		}
		return list;
	}

	private List<Vector3> GetValidEventPoints(RandomEvent ev, List<ZDO> characters)
	{
		List<Vector3> list = new List<Vector3>();
		foreach (ZDO character in characters)
		{
			if (InValidBiome(ev, character) && CheckBase(ev, character) && !(character.GetPosition().y > 3000f))
			{
				list.Add(character.GetPosition());
			}
		}
		return list;
	}

	private bool InValidBiome(RandomEvent ev, ZDO zdo)
	{
		if (ev.m_biome == Heightmap.Biome.None)
		{
			return true;
		}
		Vector3 position = zdo.GetPosition();
		if ((WorldGenerator.instance.GetBiome(position) & ev.m_biome) != 0)
		{
			return true;
		}
		return false;
	}

	private bool CheckBase(RandomEvent ev, ZDO zdo)
	{
		if (!ev.m_nearBaseOnly)
		{
			return true;
		}
		if (zdo.GetInt("baseValue") >= 3)
		{
			return true;
		}
		return false;
	}

	private bool HaveGlobalKeys(RandomEvent ev)
	{
		foreach (string requiredGlobalKey in ev.m_requiredGlobalKeys)
		{
			if (!ZoneSystem.instance.GetGlobalKey(requiredGlobalKey))
			{
				return false;
			}
		}
		foreach (string notRequiredGlobalKey in ev.m_notRequiredGlobalKeys)
		{
			if (ZoneSystem.instance.GetGlobalKey(notRequiredGlobalKey))
			{
				return false;
			}
		}
		return true;
	}

	public List<SpawnSystem.SpawnData> GetCurrentSpawners()
	{
		if (m_activeEvent != null)
		{
			return m_activeEvent.m_spawn;
		}
		return null;
	}

	public string GetEnvOverride()
	{
		if (m_activeEvent != null && !string.IsNullOrEmpty(m_activeEvent.m_forceEnvironment) && m_activeEvent.InEventBiome())
		{
			return m_activeEvent.m_forceEnvironment;
		}
		return null;
	}

	public string GetMusicOverride()
	{
		if (m_activeEvent != null && !string.IsNullOrEmpty(m_activeEvent.m_forceMusic))
		{
			return m_activeEvent.m_forceMusic;
		}
		return null;
	}

	private void SetActiveEvent(RandomEvent ev, bool end = false)
	{
		if (ev != null && m_activeEvent != null && ev.m_name == m_activeEvent.m_name)
		{
			return;
		}
		if (m_activeEvent != null)
		{
			m_activeEvent.OnDeactivate(end);
			m_activeEvent = null;
		}
		if (ev != null)
		{
			m_activeEvent = ev;
			if (m_activeEvent != null)
			{
				m_activeEvent.OnActivate();
			}
		}
	}

	public static bool InEvent()
	{
		if (m_instance == null)
		{
			return false;
		}
		return m_instance.m_activeEvent != null;
	}

	public static bool HaveActiveEvent()
	{
		if (m_instance == null)
		{
			return false;
		}
		if (m_instance.m_activeEvent != null)
		{
			return true;
		}
		if (m_instance.m_randomEvent == null)
		{
			return m_instance.m_activeEvent != null;
		}
		return true;
	}

	public RandomEvent GetCurrentRandomEvent()
	{
		return m_randomEvent;
	}

	public RandomEvent GetActiveEvent()
	{
		return m_activeEvent;
	}

	public void PrepareSave()
	{
		m_tempSaveEventTimer = m_eventTimer;
		if (m_randomEvent != null)
		{
			m_tempSaveRandomEvent = m_randomEvent.m_name;
			m_tempSaveRandomEventTime = m_randomEvent.m_time;
			m_tempSaveRandomEventPos = m_randomEvent.m_pos;
		}
		else
		{
			m_tempSaveRandomEvent = "";
			m_tempSaveRandomEventTime = 0f;
			m_tempSaveRandomEventPos = Vector3.zero;
		}
	}

	public void SaveAsync(BinaryWriter writer)
	{
		writer.Write(m_tempSaveEventTimer);
		writer.Write(m_tempSaveRandomEvent);
		writer.Write(m_tempSaveRandomEventTime);
		writer.Write(m_tempSaveRandomEventPos.x);
		writer.Write(m_tempSaveRandomEventPos.y);
		writer.Write(m_tempSaveRandomEventPos.z);
	}

	public void Load(BinaryReader reader, int version)
	{
		m_eventTimer = reader.ReadSingle();
		if (version < 25)
		{
			return;
		}
		string value = reader.ReadString();
		float time = reader.ReadSingle();
		Vector3 pos = default(Vector3);
		pos.x = reader.ReadSingle();
		pos.y = reader.ReadSingle();
		pos.z = reader.ReadSingle();
		if (!string.IsNullOrEmpty(value))
		{
			SetRandomEventByName(value, pos);
			if (m_randomEvent != null)
			{
				m_randomEvent.m_time = time;
				m_randomEvent.m_pos = pos;
			}
		}
	}
}
