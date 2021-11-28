using System;
using System.Collections.Generic;
using UnityEngine;

public class MasterClient
{
	private const int statVersion = 2;

	public Action<List<ServerData>> m_onServerList;

	private string m_msHost = "dvoid.noip.me";

	private int m_msPort = 9983;

	private long m_sessionUID;

	private ZConnector2 m_connector;

	private ZSocket2 m_socket;

	private ZRpc m_rpc;

	private bool m_haveServerlist;

	private List<ServerData> m_servers = new List<ServerData>();

	private ZPackage m_registerPkg;

	private float m_sendStatsTimer;

	private int m_serverListRevision;

	private string m_nameFilter = "";

	private static MasterClient m_instance;

	public static MasterClient instance => m_instance;

	public static void Initialize()
	{
		if (m_instance == null)
		{
			m_instance = new MasterClient();
		}
	}

	public MasterClient()
	{
		m_sessionUID = Utils.GenerateUID();
	}

	public void Dispose()
	{
		if (m_socket != null)
		{
			m_socket.Dispose();
		}
		if (m_connector != null)
		{
			m_connector.Dispose();
		}
		if (m_rpc != null)
		{
			m_rpc.Dispose();
		}
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	public void Update(float dt)
	{
	}

	private void SendStats(float duration)
	{
		ZPackage zPackage = new ZPackage();
		zPackage.Write(2);
		zPackage.Write(m_sessionUID);
		zPackage.Write(Time.time);
		bool flag = Player.m_localPlayer != null;
		zPackage.Write(flag ? duration : 0f);
		bool flag2 = (bool)ZNet.instance && !ZNet.instance.IsServer();
		zPackage.Write(flag2 ? duration : 0f);
		zPackage.Write(Version.GetVersionString());
		bool flag3 = (bool)ZNet.instance && ZNet.instance.IsServer();
		zPackage.Write(flag3);
		if (flag3)
		{
			zPackage.Write(ZNet.instance.GetWorldUID());
			zPackage.Write(duration);
			int num = ZNet.instance.GetPeerConnections();
			if (Player.m_localPlayer != null)
			{
				num++;
			}
			zPackage.Write(num);
			bool data = ZNet.instance.GetZNat() != null && ZNet.instance.GetZNat().GetStatus();
			zPackage.Write(data);
		}
		PlayerProfile playerProfile = ((Game.instance != null) ? Game.instance.GetPlayerProfile() : null);
		if (playerProfile != null)
		{
			zPackage.Write(data: true);
			zPackage.Write(playerProfile.GetPlayerID());
			zPackage.Write(playerProfile.m_playerStats.m_kills);
			zPackage.Write(playerProfile.m_playerStats.m_deaths);
			zPackage.Write(playerProfile.m_playerStats.m_crafts);
			zPackage.Write(playerProfile.m_playerStats.m_builds);
		}
		else
		{
			zPackage.Write(data: false);
		}
		m_rpc.Invoke("Stats", zPackage);
	}

	public void RegisterServer(string name, string host, int port, bool password, bool upnp, long worldUID, string version)
	{
		m_registerPkg = new ZPackage();
		m_registerPkg.Write(1);
		m_registerPkg.Write(name);
		m_registerPkg.Write(host);
		m_registerPkg.Write(port);
		m_registerPkg.Write(password);
		m_registerPkg.Write(upnp);
		m_registerPkg.Write(worldUID);
		m_registerPkg.Write(version);
		if (m_rpc != null)
		{
			m_rpc.Invoke("RegisterServer2", m_registerPkg);
		}
		ZLog.Log("Registering server " + name + "  " + host + ":" + port);
	}

	public void UnregisterServer()
	{
		if (m_registerPkg != null)
		{
			if (m_rpc != null)
			{
				m_rpc.Invoke("UnregisterServer");
			}
			m_registerPkg = null;
		}
	}

	public List<ServerData> GetServers()
	{
		return m_servers;
	}

	public bool GetServers(List<ServerData> servers)
	{
		if (!m_haveServerlist)
		{
			return false;
		}
		servers.Clear();
		servers.AddRange(m_servers);
		return true;
	}

	public void RequestServerlist()
	{
		if (m_rpc != null)
		{
			m_rpc.Invoke("RequestServerlist2");
		}
	}

	private void RPC_ServerList(ZRpc rpc, ZPackage pkg)
	{
		m_haveServerlist = true;
		m_serverListRevision++;
		pkg.ReadInt();
		int num = pkg.ReadInt();
		m_servers.Clear();
		for (int i = 0; i < num; i++)
		{
			ServerData serverData = new ServerData();
			serverData.m_name = pkg.ReadString();
			serverData.m_host = pkg.ReadString();
			serverData.m_port = pkg.ReadInt();
			serverData.m_password = pkg.ReadBool();
			serverData.m_upnp = pkg.ReadBool();
			pkg.ReadLong();
			serverData.m_version = pkg.ReadString();
			serverData.m_players = pkg.ReadInt();
			if (m_nameFilter.Length <= 0 || serverData.m_name.Contains(m_nameFilter))
			{
				m_servers.Add(serverData);
			}
		}
		if (m_onServerList != null)
		{
			m_onServerList(m_servers);
		}
	}

	public int GetServerListRevision()
	{
		return m_serverListRevision;
	}

	public bool IsConnected()
	{
		return m_rpc != null;
	}

	public void SetNameFilter(string filter)
	{
		m_nameFilter = filter;
		ZLog.Log("filter is " + filter);
	}
}
