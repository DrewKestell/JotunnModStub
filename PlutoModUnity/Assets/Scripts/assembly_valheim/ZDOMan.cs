using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

public class ZDOMan
{
	private class ZDOPeer
	{
		public struct PeerZDOInfo
		{
			public uint m_dataRevision;

			public long m_ownerRevision;

			public float m_syncTime;

			public PeerZDOInfo(uint dataRevision, uint ownerRevision, float syncTime)
			{
				m_dataRevision = dataRevision;
				m_ownerRevision = ownerRevision;
				m_syncTime = syncTime;
			}
		}

		public ZNetPeer m_peer;

		public Dictionary<ZDOID, PeerZDOInfo> m_zdos = new Dictionary<ZDOID, PeerZDOInfo>();

		public HashSet<ZDOID> m_forceSend = new HashSet<ZDOID>();

		public HashSet<ZDOID> m_invalidSector = new HashSet<ZDOID>();

		public int m_sendIndex;

		public void ZDOSectorInvalidated(ZDO zdo)
		{
			if (zdo.m_owner != m_peer.m_uid && m_zdos.ContainsKey(zdo.m_uid) && !ZNetScene.instance.InActiveArea(zdo.GetSector(), m_peer.GetRefPos()))
			{
				m_invalidSector.Add(zdo.m_uid);
				m_zdos.Remove(zdo.m_uid);
			}
		}

		public void ForceSendZDO(ZDOID id)
		{
			m_forceSend.Add(id);
		}

		public bool ShouldSend(ZDO zdo)
		{
			if (m_zdos.TryGetValue(zdo.m_uid, out var value))
			{
				if (zdo.m_ownerRevision <= value.m_ownerRevision)
				{
					return zdo.m_dataRevision > value.m_dataRevision;
				}
				return true;
			}
			return true;
		}
	}

	private class SaveData
	{
		public long m_myid;

		public uint m_nextUid = 1u;

		public List<ZDO> m_zdos;

		public Dictionary<ZDOID, long> m_deadZDOs;
	}

	private static long compareReceiver;

	public Action<ZDO> m_onZDODestroyed;

	private Dictionary<ZDOID, ZDO> m_objectsByID = new Dictionary<ZDOID, ZDO>();

	private List<ZDO>[] m_objectsBySector;

	private Dictionary<Vector2i, List<ZDO>> m_objectsByOutsideSector = new Dictionary<Vector2i, List<ZDO>>();

	private List<ZDOPeer> m_peers = new List<ZDOPeer>();

	private const int m_maxDeadZDOs = 100000;

	private Dictionary<ZDOID, long> m_deadZDOs = new Dictionary<ZDOID, long>();

	private List<ZDOID> m_destroySendList = new List<ZDOID>();

	private HashSet<ZDOID> m_clientChangeQueue = new HashSet<ZDOID>();

	private long m_myid;

	private uint m_nextUid = 1u;

	private int m_width;

	private int m_halfWidth;

	private float m_sendTimer;

	private const float m_sendFPS = 20f;

	private float m_releaseZDOTimer;

	private static ZDOMan m_instance;

	private int m_zdosSent;

	private int m_zdosRecv;

	private int m_zdosSentLastSec;

	private int m_zdosRecvLastSec;

	private float m_statTimer;

	private List<ZDO> m_tempToSync = new List<ZDO>();

	private List<ZDO> m_tempToSyncDistant = new List<ZDO>();

	private List<ZDO> m_tempNearObjects = new List<ZDO>();

	private List<ZDOID> m_tempRemoveList = new List<ZDOID>();

	private SaveData m_saveData;

	public static ZDOMan instance => m_instance;

	public ZDOMan(int width)
	{
		m_instance = this;
		m_myid = Utils.GenerateUID();
		ZRoutedRpc.instance.Register<ZPackage>("DestroyZDO", RPC_DestroyZDO);
		ZRoutedRpc.instance.Register<ZDOID>("RequestZDO", RPC_RequestZDO);
		m_width = width;
		m_halfWidth = m_width / 2;
		ResetSectorArray();
	}

	private void ResetSectorArray()
	{
		m_objectsBySector = new List<ZDO>[m_width * m_width];
		m_objectsByOutsideSector.Clear();
	}

	public void ShutDown()
	{
		if (!ZNet.instance.IsServer())
		{
			FlushClientObjects();
		}
		ZDOPool.Release(m_objectsByID);
		m_objectsByID.Clear();
		m_tempToSync.Clear();
		m_tempToSyncDistant.Clear();
		m_tempNearObjects.Clear();
		m_tempRemoveList.Clear();
		m_peers.Clear();
		ResetSectorArray();
		GC.Collect();
	}

	public void PrepareSave()
	{
		m_saveData = new SaveData();
		m_saveData.m_myid = m_myid;
		m_saveData.m_nextUid = m_nextUid;
		Stopwatch stopwatch = Stopwatch.StartNew();
		m_saveData.m_zdos = GetSaveClone();
		ZLog.Log("clone " + stopwatch.ElapsedMilliseconds);
		m_saveData.m_deadZDOs = new Dictionary<ZDOID, long>(m_deadZDOs);
	}

	public void SaveAsync(BinaryWriter writer)
	{
		writer.Write(m_saveData.m_myid);
		writer.Write(m_saveData.m_nextUid);
		ZPackage zPackage = new ZPackage();
		writer.Write(m_saveData.m_zdos.Count);
		foreach (ZDO zdo in m_saveData.m_zdos)
		{
			writer.Write(zdo.m_uid.userID);
			writer.Write(zdo.m_uid.id);
			zPackage.Clear();
			zdo.Save(zPackage);
			byte[] array = zPackage.GetArray();
			writer.Write(array.Length);
			writer.Write(array);
		}
		writer.Write(m_saveData.m_deadZDOs.Count);
		foreach (KeyValuePair<ZDOID, long> deadZDO in m_saveData.m_deadZDOs)
		{
			writer.Write(deadZDO.Key.userID);
			writer.Write(deadZDO.Key.id);
			writer.Write(deadZDO.Value);
		}
		ZLog.Log("Saved " + m_saveData.m_zdos.Count + " zdos");
		m_saveData = null;
	}

	public void Load(BinaryReader reader, int version)
	{
		reader.ReadInt64();
		uint num = reader.ReadUInt32();
		int num2 = reader.ReadInt32();
		ZDOPool.Release(m_objectsByID);
		m_objectsByID.Clear();
		ResetSectorArray();
		ZLog.Log("Loading " + num2 + " zdos , my id " + m_myid + " data version:" + version);
		ZPackage zPackage = new ZPackage();
		for (int i = 0; i < num2; i++)
		{
			ZDO zDO = ZDOPool.Create(this);
			zDO.m_uid = new ZDOID(reader);
			int count = reader.ReadInt32();
			byte[] data = reader.ReadBytes(count);
			zPackage.Load(data);
			zDO.Load(zPackage, version);
			zDO.SetOwner(0L);
			m_objectsByID.Add(zDO.m_uid, zDO);
			AddToSector(zDO, zDO.GetSector());
			if (zDO.m_uid.userID == m_myid && zDO.m_uid.id >= num)
			{
				num = zDO.m_uid.id + 1;
			}
		}
		m_deadZDOs.Clear();
		int num3 = reader.ReadInt32();
		for (int j = 0; j < num3; j++)
		{
			ZDOID key = new ZDOID(reader.ReadInt64(), reader.ReadUInt32());
			long value = reader.ReadInt64();
			m_deadZDOs.Add(key, value);
			if (key.userID == m_myid && key.id >= num)
			{
				num = key.id + 1;
			}
		}
		CapDeadZDOList();
		ZLog.Log("Loaded " + m_deadZDOs.Count + " dead zdos");
		RemoveOldGeneratedZDOS();
		m_nextUid = num;
	}

	private void RemoveOldGeneratedZDOS()
	{
		List<ZDOID> list = new List<ZDOID>();
		foreach (KeyValuePair<ZDOID, ZDO> item in m_objectsByID)
		{
			int pGWVersion = item.Value.GetPGWVersion();
			if (pGWVersion != 0 && pGWVersion != ZoneSystem.instance.m_pgwVersion)
			{
				list.Add(item.Key);
				RemoveFromSector(item.Value, item.Value.GetSector());
				ZDOPool.Release(item.Value);
			}
		}
		foreach (ZDOID item2 in list)
		{
			m_objectsByID.Remove(item2);
		}
		ZLog.Log("Removed " + list.Count + " OLD generated ZDOS");
	}

	private void CapDeadZDOList()
	{
		if (m_deadZDOs.Count > 100000)
		{
			List<KeyValuePair<ZDOID, long>> list = m_deadZDOs.ToList();
			list.Sort((KeyValuePair<ZDOID, long> a, KeyValuePair<ZDOID, long> b) => a.Value.CompareTo(b.Value));
			int num = list.Count - 100000;
			for (int i = 0; i < num; i++)
			{
				m_deadZDOs.Remove(list[i].Key);
			}
		}
	}

	public ZDO CreateNewZDO(Vector3 position)
	{
		ZDOID zDOID = new ZDOID(m_myid, m_nextUid++);
		while (GetZDO(zDOID) != null)
		{
			zDOID = new ZDOID(m_myid, m_nextUid++);
		}
		return CreateNewZDO(zDOID, position);
	}

	public ZDO CreateNewZDO(ZDOID uid, Vector3 position)
	{
		ZDO zDO = ZDOPool.Create(this, uid, position);
		zDO.m_owner = m_myid;
		zDO.m_timeCreated = ZNet.instance.GetTime().Ticks;
		m_objectsByID.Add(uid, zDO);
		return zDO;
	}

	public void AddToSector(ZDO zdo, Vector2i sector)
	{
		int num = SectorToIndex(sector);
		List<ZDO> value;
		if (num >= 0)
		{
			if (m_objectsBySector[num] != null)
			{
				m_objectsBySector[num].Add(zdo);
				return;
			}
			List<ZDO> list = new List<ZDO>();
			list.Add(zdo);
			m_objectsBySector[num] = list;
		}
		else if (m_objectsByOutsideSector.TryGetValue(sector, out value))
		{
			value.Add(zdo);
		}
		else
		{
			value = new List<ZDO>();
			value.Add(zdo);
			m_objectsByOutsideSector.Add(sector, value);
		}
	}

	public void ZDOSectorInvalidated(ZDO zdo)
	{
		foreach (ZDOPeer peer in m_peers)
		{
			peer.ZDOSectorInvalidated(zdo);
		}
	}

	public void RemoveFromSector(ZDO zdo, Vector2i sector)
	{
		int num = SectorToIndex(sector);
		List<ZDO> value;
		if (num >= 0)
		{
			if (m_objectsBySector[num] != null)
			{
				m_objectsBySector[num].Remove(zdo);
			}
		}
		else if (m_objectsByOutsideSector.TryGetValue(sector, out value))
		{
			value.Remove(zdo);
		}
	}

	public ZDO GetZDO(ZDOID id)
	{
		if (id == ZDOID.None)
		{
			return null;
		}
		if (m_objectsByID.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	public void AddPeer(ZNetPeer netPeer)
	{
		ZDOPeer zDOPeer = new ZDOPeer();
		zDOPeer.m_peer = netPeer;
		m_peers.Add(zDOPeer);
		zDOPeer.m_peer.m_rpc.Register<ZPackage>("ZDOData", RPC_ZDOData);
	}

	public void RemovePeer(ZNetPeer netPeer)
	{
		ZDOPeer zDOPeer = FindPeer(netPeer);
		if (zDOPeer != null)
		{
			m_peers.Remove(zDOPeer);
			if (ZNet.instance.IsServer())
			{
				RemoveOrphanNonPersistentZDOS();
			}
		}
	}

	private ZDOPeer FindPeer(ZNetPeer netPeer)
	{
		foreach (ZDOPeer peer in m_peers)
		{
			if (peer.m_peer == netPeer)
			{
				return peer;
			}
		}
		return null;
	}

	private ZDOPeer FindPeer(ZRpc rpc)
	{
		foreach (ZDOPeer peer in m_peers)
		{
			if (peer.m_peer.m_rpc == rpc)
			{
				return peer;
			}
		}
		return null;
	}

	public void Update(float dt)
	{
		if (ZNet.instance.IsServer())
		{
			ReleaseZDOS(dt);
		}
		SendZDOToPeers(dt);
		SendDestroyed();
		UpdateStats(dt);
	}

	private void UpdateStats(float dt)
	{
		m_statTimer += dt;
		if (m_statTimer >= 1f)
		{
			m_statTimer = 0f;
			m_zdosSentLastSec = m_zdosSent;
			m_zdosRecvLastSec = m_zdosRecv;
			m_zdosRecv = 0;
			m_zdosSent = 0;
		}
	}

	private void SendZDOToPeers(float dt)
	{
		m_sendTimer += dt;
		if (!(m_sendTimer > 0.05f))
		{
			return;
		}
		m_sendTimer = 0f;
		foreach (ZDOPeer peer in m_peers)
		{
			SendZDOs(peer, flush: false);
		}
	}

	private void FlushClientObjects()
	{
		foreach (ZDOPeer peer in m_peers)
		{
			SendAllZDOs(peer);
		}
	}

	private void ReleaseZDOS(float dt)
	{
		m_releaseZDOTimer += dt;
		if (!(m_releaseZDOTimer > 2f))
		{
			return;
		}
		m_releaseZDOTimer = 0f;
		ReleaseNearbyZDOS(ZNet.instance.GetReferencePosition(), m_myid);
		foreach (ZDOPeer peer in m_peers)
		{
			ReleaseNearbyZDOS(peer.m_peer.m_refPos, peer.m_peer.m_uid);
		}
	}

	private bool IsInPeerActiveArea(Vector2i sector, long uid)
	{
		if (uid == m_myid)
		{
			return ZNetScene.instance.InActiveArea(sector, ZNet.instance.GetReferencePosition());
		}
		ZNetPeer peer = ZNet.instance.GetPeer(uid);
		if (peer == null)
		{
			return false;
		}
		return ZNetScene.instance.InActiveArea(sector, peer.GetRefPos());
	}

	private void ReleaseNearbyZDOS(Vector3 refPosition, long uid)
	{
		Vector2i zone = ZoneSystem.instance.GetZone(refPosition);
		m_tempNearObjects.Clear();
		FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, 0, m_tempNearObjects);
		foreach (ZDO tempNearObject in m_tempNearObjects)
		{
			if (!tempNearObject.m_persistent)
			{
				continue;
			}
			if (tempNearObject.m_owner == uid)
			{
				if (!ZNetScene.instance.InActiveArea(tempNearObject.GetSector(), zone))
				{
					tempNearObject.SetOwner(0L);
				}
			}
			else if ((tempNearObject.m_owner == 0L || !IsInPeerActiveArea(tempNearObject.GetSector(), tempNearObject.m_owner)) && ZNetScene.instance.InActiveArea(tempNearObject.GetSector(), zone))
			{
				tempNearObject.SetOwner(uid);
			}
		}
	}

	public void DestroyZDO(ZDO zdo)
	{
		if (zdo.IsOwner())
		{
			m_destroySendList.Add(zdo.m_uid);
		}
	}

	private void SendDestroyed()
	{
		if (m_destroySendList.Count == 0)
		{
			return;
		}
		ZPackage zPackage = new ZPackage();
		zPackage.Write(m_destroySendList.Count);
		foreach (ZDOID destroySend in m_destroySendList)
		{
			zPackage.Write(destroySend);
		}
		m_destroySendList.Clear();
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "DestroyZDO", zPackage);
	}

	private void RPC_DestroyZDO(long sender, ZPackage pkg)
	{
		int num = pkg.ReadInt();
		for (int i = 0; i < num; i++)
		{
			ZDOID uid = pkg.ReadZDOID();
			HandleDestroyedZDO(uid);
		}
	}

	private void HandleDestroyedZDO(ZDOID uid)
	{
		if (uid.userID == m_myid && uid.id >= m_nextUid)
		{
			m_nextUid = uid.id + 1;
		}
		ZDO zDO = GetZDO(uid);
		if (zDO == null)
		{
			return;
		}
		if (m_onZDODestroyed != null)
		{
			m_onZDODestroyed(zDO);
		}
		RemoveFromSector(zDO, zDO.GetSector());
		m_objectsByID.Remove(zDO.m_uid);
		ZDOPool.Release(zDO);
		foreach (ZDOPeer peer in m_peers)
		{
			peer.m_zdos.Remove(uid);
		}
		if (ZNet.instance.IsServer())
		{
			long ticks = ZNet.instance.GetTime().Ticks;
			m_deadZDOs[uid] = ticks;
		}
	}

	private void SendAllZDOs(ZDOPeer peer)
	{
		while (SendZDOs(peer, flush: true))
		{
		}
	}

	private bool SendZDOs(ZDOPeer peer, bool flush)
	{
		int sendQueueSize = peer.m_peer.m_socket.GetSendQueueSize();
		if (!flush && sendQueueSize > 10240)
		{
			return false;
		}
		int num = 10240 - sendQueueSize;
		if (num < 2048)
		{
			return false;
		}
		m_tempToSync.Clear();
		CreateSyncList(peer, m_tempToSync);
		if (m_tempToSync.Count == 0 && peer.m_invalidSector.Count == 0)
		{
			return false;
		}
		ZPackage zPackage = new ZPackage();
		bool flag = false;
		if (peer.m_invalidSector.Count > 0)
		{
			flag = true;
			zPackage.Write(peer.m_invalidSector.Count);
			foreach (ZDOID item in peer.m_invalidSector)
			{
				zPackage.Write(item);
			}
			peer.m_invalidSector.Clear();
		}
		else
		{
			zPackage.Write(0);
		}
		float time = Time.time;
		ZPackage zPackage2 = new ZPackage();
		bool flag2 = false;
		for (int i = 0; i < m_tempToSync.Count; i++)
		{
			if (zPackage.Size() > num)
			{
				break;
			}
			ZDO zDO = m_tempToSync[i];
			peer.m_forceSend.Remove(zDO.m_uid);
			if (!ZNet.instance.IsServer())
			{
				m_clientChangeQueue.Remove(zDO.m_uid);
			}
			if (peer.ShouldSend(zDO))
			{
				zPackage.Write(zDO.m_uid);
				zPackage.Write(zDO.m_ownerRevision);
				zPackage.Write(zDO.m_dataRevision);
				zPackage.Write(zDO.m_owner);
				zPackage.Write(zDO.GetPosition());
				zPackage2.Clear();
				zDO.Serialize(zPackage2);
				zPackage.Write(zPackage2);
				peer.m_zdos[zDO.m_uid] = new ZDOPeer.PeerZDOInfo(zDO.m_dataRevision, zDO.m_ownerRevision, time);
				flag2 = true;
				m_zdosSent++;
			}
		}
		zPackage.Write(ZDOID.None);
		if (flag2 || flag)
		{
			peer.m_peer.m_rpc.Invoke("ZDOData", zPackage);
		}
		return flag2 || flag;
	}

	private void RPC_ZDOData(ZRpc rpc, ZPackage pkg)
	{
		ZDOPeer zDOPeer = FindPeer(rpc);
		if (zDOPeer == null)
		{
			ZLog.Log("ZDO data from unkown host, ignoring");
			return;
		}
		float time = Time.time;
		int num = 0;
		ZPackage pkg2 = new ZPackage();
		int num2 = pkg.ReadInt();
		for (int i = 0; i < num2; i++)
		{
			ZDOID id = pkg.ReadZDOID();
			GetZDO(id)?.InvalidateSector();
		}
		while (true)
		{
			ZDOID zDOID = pkg.ReadZDOID();
			if (zDOID.IsNone())
			{
				break;
			}
			num++;
			uint num3 = pkg.ReadUInt();
			uint num4 = pkg.ReadUInt();
			long owner = pkg.ReadLong();
			Vector3 vector = pkg.ReadVector3();
			pkg.ReadPackage(ref pkg2);
			ZDO zDO = GetZDO(zDOID);
			bool flag = false;
			if (zDO != null)
			{
				if (num4 <= zDO.m_dataRevision)
				{
					if (num3 > zDO.m_ownerRevision)
					{
						zDO.m_owner = owner;
						zDO.m_ownerRevision = num3;
						zDOPeer.m_zdos[zDOID] = new ZDOPeer.PeerZDOInfo(num4, num3, time);
					}
					continue;
				}
			}
			else
			{
				zDO = CreateNewZDO(zDOID, vector);
				flag = true;
			}
			zDO.m_ownerRevision = num3;
			zDO.m_dataRevision = num4;
			zDO.m_owner = owner;
			zDO.InternalSetPosition(vector);
			zDOPeer.m_zdos[zDOID] = new ZDOPeer.PeerZDOInfo(zDO.m_dataRevision, zDO.m_ownerRevision, time);
			zDO.Deserialize(pkg2);
			if (ZNet.instance.IsServer() && flag && m_deadZDOs.ContainsKey(zDOID))
			{
				zDO.SetOwner(m_myid);
				DestroyZDO(zDO);
			}
		}
		m_zdosRecv += num;
	}

	public void FindSectorObjects(Vector2i sector, int area, int distantArea, List<ZDO> sectorObjects, List<ZDO> distantSectorObjects = null)
	{
		FindObjects(sector, sectorObjects);
		for (int i = 1; i <= area; i++)
		{
			for (int j = sector.x - i; j <= sector.x + i; j++)
			{
				FindObjects(new Vector2i(j, sector.y - i), sectorObjects);
				FindObjects(new Vector2i(j, sector.y + i), sectorObjects);
			}
			for (int k = sector.y - i + 1; k <= sector.y + i - 1; k++)
			{
				FindObjects(new Vector2i(sector.x - i, k), sectorObjects);
				FindObjects(new Vector2i(sector.x + i, k), sectorObjects);
			}
		}
		List<ZDO> objects = ((distantSectorObjects != null) ? distantSectorObjects : sectorObjects);
		for (int l = area + 1; l <= area + distantArea; l++)
		{
			for (int m = sector.x - l; m <= sector.x + l; m++)
			{
				FindDistantObjects(new Vector2i(m, sector.y - l), objects);
				FindDistantObjects(new Vector2i(m, sector.y + l), objects);
			}
			for (int n = sector.y - l + 1; n <= sector.y + l - 1; n++)
			{
				FindDistantObjects(new Vector2i(sector.x - l, n), objects);
				FindDistantObjects(new Vector2i(sector.x + l, n), objects);
			}
		}
	}

	public void FindSectorObjects(Vector2i sector, int area, List<ZDO> sectorObjects)
	{
		for (int i = sector.y - area; i <= sector.y + area; i++)
		{
			for (int j = sector.x - area; j <= sector.x + area; j++)
			{
				Vector2i sector2 = new Vector2i(j, i);
				FindObjects(sector2, sectorObjects);
			}
		}
	}

	private void CreateSyncList(ZDOPeer peer, List<ZDO> toSync)
	{
		if (ZNet.instance.IsServer())
		{
			Vector3 refPos = peer.m_peer.GetRefPos();
			Vector2i zone = ZoneSystem.instance.GetZone(refPos);
			m_tempToSyncDistant.Clear();
			FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, ZoneSystem.instance.m_activeDistantArea, toSync, m_tempToSyncDistant);
			ServerSortSendZDOS(toSync, refPos, peer);
			toSync.AddRange(m_tempToSyncDistant);
			AddForceSendZdos(peer, toSync);
			return;
		}
		m_tempRemoveList.Clear();
		foreach (ZDOID item in m_clientChangeQueue)
		{
			ZDO zDO = GetZDO(item);
			if (zDO != null)
			{
				toSync.Add(zDO);
			}
			else
			{
				m_tempRemoveList.Add(item);
			}
		}
		foreach (ZDOID tempRemove in m_tempRemoveList)
		{
			m_clientChangeQueue.Remove(tempRemove);
		}
		ClientSortSendZDOS(toSync, peer);
		AddForceSendZdos(peer, toSync);
	}

	private void AddForceSendZdos(ZDOPeer peer, List<ZDO> syncList)
	{
		if (peer.m_forceSend.Count <= 0)
		{
			return;
		}
		m_tempRemoveList.Clear();
		foreach (ZDOID item in peer.m_forceSend)
		{
			ZDO zDO = GetZDO(item);
			if (zDO != null)
			{
				syncList.Insert(0, zDO);
			}
			else
			{
				m_tempRemoveList.Add(item);
			}
		}
		foreach (ZDOID tempRemove in m_tempRemoveList)
		{
			peer.m_forceSend.Remove(tempRemove);
		}
	}

	private static int ServerSendCompare(ZDO x, ZDO y)
	{
		bool flag = x.m_type == ZDO.ObjectType.Prioritized && x.HasOwner() && x.m_owner != compareReceiver;
		bool flag2 = y.m_type == ZDO.ObjectType.Prioritized && y.HasOwner() && y.m_owner != compareReceiver;
		if (flag && flag2)
		{
			return x.m_tempSortValue.CompareTo(y.m_tempSortValue);
		}
		if (flag != flag2)
		{
			if (!flag)
			{
				return 1;
			}
			return -1;
		}
		if (x.m_type == y.m_type)
		{
			return x.m_tempSortValue.CompareTo(y.m_tempSortValue);
		}
		return y.m_type.CompareTo(x.m_type);
	}

	private void ServerSortSendZDOS(List<ZDO> objects, Vector3 refPos, ZDOPeer peer)
	{
		float time = Time.time;
		for (int i = 0; i < objects.Count; i++)
		{
			ZDO zDO = objects[i];
			Vector3 position = zDO.GetPosition();
			zDO.m_tempSortValue = Vector3.Distance(position, refPos);
			float num = 100f;
			if (peer.m_zdos.TryGetValue(zDO.m_uid, out var value))
			{
				num = Mathf.Clamp(time - value.m_syncTime, 0f, 100f);
				zDO.m_tempHaveRevision = true;
			}
			else
			{
				zDO.m_tempHaveRevision = false;
			}
			zDO.m_tempSortValue -= num * 1.5f;
		}
		compareReceiver = peer.m_peer.m_uid;
		objects.Sort(ServerSendCompare);
	}

	private static int ClientSendCompare(ZDO x, ZDO y)
	{
		if (x.m_type == y.m_type)
		{
			return x.m_tempSortValue.CompareTo(y.m_tempSortValue);
		}
		if (x.m_type == ZDO.ObjectType.Prioritized)
		{
			return -1;
		}
		if (y.m_type == ZDO.ObjectType.Prioritized)
		{
			return 1;
		}
		return x.m_tempSortValue.CompareTo(y.m_tempSortValue);
	}

	private void ClientSortSendZDOS(List<ZDO> objects, ZDOPeer peer)
	{
		float time = Time.time;
		for (int i = 0; i < objects.Count; i++)
		{
			ZDO zDO = objects[i];
			zDO.m_tempSortValue = 0f;
			float num = 100f;
			if (peer.m_zdos.TryGetValue(zDO.m_uid, out var value))
			{
				num = Mathf.Clamp(time - value.m_syncTime, 0f, 100f);
			}
			zDO.m_tempSortValue -= num * 1.5f;
		}
		objects.Sort(ClientSendCompare);
	}

	private void PrintZdoList(List<ZDO> zdos)
	{
		ZLog.Log("Sync list " + zdos.Count);
		foreach (ZDO zdo in zdos)
		{
			string text = "";
			int prefab = zdo.GetPrefab();
			if (prefab != 0)
			{
				GameObject prefab2 = ZNetScene.instance.GetPrefab(prefab);
				if ((bool)prefab2)
				{
					text = prefab2.name;
				}
			}
			ZLog.Log("  " + zdo.m_uid.ToString() + "  " + zdo.m_ownerRevision + " prefab:" + text);
		}
	}

	private void AddDistantObjects(ZDOPeer peer, int maxItems, List<ZDO> toSync)
	{
		if (peer.m_sendIndex >= m_objectsByID.Count)
		{
			peer.m_sendIndex = 0;
		}
		IEnumerable<KeyValuePair<ZDOID, ZDO>> enumerable = m_objectsByID.Skip(peer.m_sendIndex).Take(maxItems);
		peer.m_sendIndex += maxItems;
		foreach (KeyValuePair<ZDOID, ZDO> item in enumerable)
		{
			toSync.Add(item.Value);
		}
	}

	public long GetMyID()
	{
		return m_myid;
	}

	private int SectorToIndex(Vector2i s)
	{
		int num = s.x + m_halfWidth;
		int num2 = s.y + m_halfWidth;
		if (num < 0 || num2 < 0 || num >= m_width || num2 >= m_width)
		{
			return -1;
		}
		return num2 * m_width + num;
	}

	private void FindObjects(Vector2i sector, List<ZDO> objects)
	{
		int num = SectorToIndex(sector);
		List<ZDO> value;
		if (num >= 0)
		{
			if (m_objectsBySector[num] != null)
			{
				objects.AddRange(m_objectsBySector[num]);
			}
		}
		else if (m_objectsByOutsideSector.TryGetValue(sector, out value))
		{
			objects.AddRange(value);
		}
	}

	private void FindDistantObjects(Vector2i sector, List<ZDO> objects)
	{
		int num = SectorToIndex(sector);
		if (num >= 0)
		{
			List<ZDO> list = m_objectsBySector[num];
			if (list == null)
			{
				return;
			}
			for (int i = 0; i < list.Count; i++)
			{
				ZDO zDO = list[i];
				if (zDO.m_distant)
				{
					objects.Add(zDO);
				}
			}
		}
		else
		{
			if (!m_objectsByOutsideSector.TryGetValue(sector, out var value))
			{
				return;
			}
			for (int j = 0; j < value.Count; j++)
			{
				ZDO zDO2 = value[j];
				if (zDO2.m_distant)
				{
					objects.Add(zDO2);
				}
			}
		}
	}

	private void RemoveOrphanNonPersistentZDOS()
	{
		foreach (KeyValuePair<ZDOID, ZDO> item in m_objectsByID)
		{
			ZDO value = item.Value;
			if (!value.m_persistent && (value.m_owner == 0L || !IsPeerConnected(value.m_owner)))
			{
				ZLog.Log(string.Concat("Destroying abandoned non persistent zdo ", value.m_uid, " owner ", value.m_owner));
				value.SetOwner(m_myid);
				DestroyZDO(value);
			}
		}
	}

	private bool IsPeerConnected(long uid)
	{
		if (m_myid == uid)
		{
			return true;
		}
		foreach (ZDOPeer peer in m_peers)
		{
			if (peer.m_peer.m_uid == uid)
			{
				return true;
			}
		}
		return false;
	}

	public void GetAllZDOsWithPrefab(string prefab, List<ZDO> zdos)
	{
		int stableHashCode = prefab.GetStableHashCode();
		foreach (ZDO value in m_objectsByID.Values)
		{
			if (value.GetPrefab() == stableHashCode)
			{
				zdos.Add(value);
			}
		}
	}

	private static bool InvalidZDO(ZDO zdo)
	{
		return !zdo.IsValid();
	}

	public bool GetAllZDOsWithPrefabIterative(string prefab, List<ZDO> zdos, ref int index)
	{
		int stableHashCode = prefab.GetStableHashCode();
		if (index >= m_objectsBySector.Length)
		{
			foreach (List<ZDO> value in m_objectsByOutsideSector.Values)
			{
				foreach (ZDO item in value)
				{
					if (item.GetPrefab() == stableHashCode)
					{
						zdos.Add(item);
					}
				}
			}
			zdos.RemoveAll(InvalidZDO);
			return true;
		}
		int num = 0;
		while (index < m_objectsBySector.Length)
		{
			List<ZDO> list = m_objectsBySector[index];
			if (list != null)
			{
				foreach (ZDO item2 in list)
				{
					if (item2.GetPrefab() == stableHashCode)
					{
						zdos.Add(item2);
					}
				}
				num++;
				if (num > 400)
				{
					break;
				}
			}
			index++;
		}
		return false;
	}

	private List<ZDO> GetSaveClone()
	{
		List<ZDO> list = new List<ZDO>();
		for (int i = 0; i < m_objectsBySector.Length; i++)
		{
			if (m_objectsBySector[i] == null)
			{
				continue;
			}
			foreach (ZDO item in m_objectsBySector[i])
			{
				if (item.m_persistent)
				{
					list.Add(item.Clone());
				}
			}
		}
		foreach (List<ZDO> value in m_objectsByOutsideSector.Values)
		{
			foreach (ZDO item2 in value)
			{
				if (item2.m_persistent)
				{
					list.Add(item2.Clone());
				}
			}
		}
		return list;
	}

	public int NrOfObjects()
	{
		return m_objectsByID.Count;
	}

	public int GetSentZDOs()
	{
		return m_zdosSentLastSec;
	}

	public int GetRecvZDOs()
	{
		return m_zdosRecvLastSec;
	}

	public void GetAverageStats(out float sentZdos, out float recvZdos)
	{
		sentZdos = (float)m_zdosSentLastSec / 20f;
		recvZdos = (float)m_zdosRecvLastSec / 20f;
	}

	public int GetClientChangeQueue()
	{
		return m_clientChangeQueue.Count;
	}

	public void RequestZDO(ZDOID id)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC("RequestZDO", id);
	}

	private void RPC_RequestZDO(long sender, ZDOID id)
	{
		GetPeer(sender)?.ForceSendZDO(id);
	}

	private ZDOPeer GetPeer(long uid)
	{
		foreach (ZDOPeer peer in m_peers)
		{
			if (peer.m_peer.m_uid == uid)
			{
				return peer;
			}
		}
		return null;
	}

	public void ForceSendZDO(ZDOID id)
	{
		foreach (ZDOPeer peer in m_peers)
		{
			peer.ForceSendZDO(id);
		}
	}

	public void ForceSendZDO(long peerID, ZDOID id)
	{
		if (ZNet.instance.IsServer())
		{
			GetPeer(peerID)?.ForceSendZDO(id);
			return;
		}
		foreach (ZDOPeer peer in m_peers)
		{
			peer.ForceSendZDO(id);
		}
	}

	public void ClientChanged(ZDOID id)
	{
		m_clientChangeQueue.Add(id);
	}
}
