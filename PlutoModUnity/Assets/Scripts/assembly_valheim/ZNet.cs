using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Steamworks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ZNet : MonoBehaviour
{
	public enum ConnectionStatus
	{
		None,
		Connecting,
		Connected,
		ErrorVersion,
		ErrorDisconnected,
		ErrorConnectFailed,
		ErrorPassword,
		ErrorAlreadyConnected,
		ErrorBanned,
		ErrorFull
	}

	public struct PlayerInfo
	{
		public string m_name;

		public string m_host;

		public ZDOID m_characterID;

		public bool m_publicPosition;

		public Vector3 m_position;
	}

	private float m_banlistTimer;

	private static ZNet m_instance;

	public int m_hostPort = 2456;

	public RectTransform m_passwordDialog;

	public RectTransform m_connectingDialog;

	public float m_badConnectionPing = 5f;

	public int m_zdoSectorsWidth = 512;

	public int m_serverPlayerLimit = 10;

	private ZConnector2 m_serverConnector;

	private ISocket m_hostSocket;

	private List<ZNetPeer> m_peers = new List<ZNetPeer>();

	private Thread m_saveThread;

	private float m_saveStartTime;

	private float m_saveThreadStartTime;

	private bool m_loadError;

	private ZDOMan m_zdoMan;

	private ZRoutedRpc m_routedRpc;

	private ZNat m_nat;

	private double m_netTime = 2040.0;

	private ZDOID m_characterID = ZDOID.None;

	private Vector3 m_referencePosition = Vector3.zero;

	private bool m_publicReferencePosition;

	private float m_periodicSendTimer;

	private bool m_haveStoped;

	private static bool m_isServer = true;

	private static World m_world = null;

	private static ulong m_serverSteamID = 0uL;

	private static SteamNetworkingIPAddr m_serverIPAddr;

	private static bool m_openServer = true;

	private static bool m_publicServer = true;

	private static string m_serverPassword = "";

	private static string m_ServerName = "";

	private static ConnectionStatus m_connectionStatus = ConnectionStatus.None;

	private SyncedList m_adminList;

	private SyncedList m_bannedList;

	private SyncedList m_permittedList;

	private List<PlayerInfo> m_players = new List<PlayerInfo>();

	private ZRpc m_tempPasswordRPC;

	public static ZNet instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_routedRpc = new ZRoutedRpc(m_isServer);
		m_zdoMan = new ZDOMan(m_zdoSectorsWidth);
		m_passwordDialog.gameObject.SetActive(value: false);
		m_connectingDialog.gameObject.SetActive(value: false);
		WorldGenerator.Deitialize();
		if (!SteamManager.Initialize())
		{
			return;
		}
		string personaName = SteamFriends.GetPersonaName();
		ZLog.Log("Steam initialized, persona:" + personaName);
		ZSteamMatchmaking.Initialize();
		if (m_isServer)
		{
			m_adminList = new SyncedList(Utils.GetSaveDataPath() + "/adminlist.txt", "List admin players ID  ONE per line");
			m_bannedList = new SyncedList(Utils.GetSaveDataPath() + "/bannedlist.txt", "List banned players ID  ONE per line");
			m_permittedList = new SyncedList(Utils.GetSaveDataPath() + "/permittedlist.txt", "List permitted players ID ONE per line");
			if (m_world == null)
			{
				m_publicServer = false;
				m_world = World.GetDevWorld();
			}
			if (m_openServer)
			{
				ZSteamSocket zSteamSocket = new ZSteamSocket();
				zSteamSocket.StartHost();
				m_hostSocket = zSteamSocket;
				bool password = m_serverPassword != "";
				string versionString = Version.GetVersionString();
				ZSteamMatchmaking.instance.RegisterServer(m_ServerName, password, versionString, m_publicServer, m_world.m_seedName);
			}
			WorldGenerator.Initialize(m_world);
			m_connectionStatus = ConnectionStatus.Connected;
		}
		m_routedRpc.SetUID(m_zdoMan.GetMyID());
		if (IsServer())
		{
			SendPlayerList();
		}
	}

	private void Start()
	{
		if (m_isServer)
		{
			LoadWorld();
			ZoneSystem.instance.GenerateLocationsIfNeeded();
		}
		else if (m_serverSteamID != 0L)
		{
			ZLog.Log("Connecting to server " + m_serverSteamID);
			Connect(new CSteamID(m_serverSteamID));
		}
		else
		{
			m_serverIPAddr.ToString(out var buf, bWithPort: true);
			ZLog.Log("Connecting to server " + buf);
			Connect(m_serverIPAddr);
		}
	}

	private string GetPublicIP()
	{
		try
		{
			string input = Utils.DownloadString("http://checkip.dyndns.org/");
			input = new Regex("\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}").Matches(input)[0].ToString();
			ZLog.Log("Got public ip respons:" + input);
			return input;
		}
		catch (Exception ex)
		{
			ZLog.Log("Failed to get public ip:" + ex.ToString());
			return "";
		}
	}

	public void Shutdown()
	{
		ZLog.Log("ZNet Shutdown");
		Save(sync: true);
		StopAll();
		base.enabled = false;
	}

	private void StopAll()
	{
		if (m_haveStoped)
		{
			return;
		}
		m_haveStoped = true;
		if (m_saveThread != null && m_saveThread.IsAlive)
		{
			m_saveThread.Join();
			m_saveThread = null;
		}
		m_zdoMan.ShutDown();
		SendDisconnect();
		ZSteamMatchmaking.instance.ReleaseSessionTicket();
		ZSteamMatchmaking.instance.UnregisterServer();
		if (m_hostSocket != null)
		{
			m_hostSocket.Dispose();
		}
		if (m_serverConnector != null)
		{
			m_serverConnector.Dispose();
		}
		foreach (ZNetPeer peer in m_peers)
		{
			peer.Dispose();
		}
		m_peers.Clear();
	}

	private void OnDestroy()
	{
		ZLog.Log("ZNet OnDestroy");
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	public void Connect(CSteamID hostID)
	{
		ZNetPeer peer = new ZNetPeer(new ZSteamSocket(hostID), server: true);
		OnNewConnection(peer);
		m_connectionStatus = ConnectionStatus.Connecting;
		m_connectingDialog.gameObject.SetActive(value: true);
	}

	public void Connect(SteamNetworkingIPAddr host)
	{
		ZNetPeer peer = new ZNetPeer(new ZSteamSocket(host), server: true);
		OnNewConnection(peer);
		m_connectionStatus = ConnectionStatus.Connecting;
		m_connectingDialog.gameObject.SetActive(value: true);
	}

	private void UpdateClientConnector(float dt)
	{
		if (m_serverConnector != null && m_serverConnector.UpdateStatus(dt, logErrors: true))
		{
			ZSocket2 zSocket = m_serverConnector.Complete();
			if (zSocket != null)
			{
				ZLog.Log("Connection established to " + m_serverConnector.GetEndPointString());
				ZNetPeer peer = new ZNetPeer(zSocket, server: true);
				OnNewConnection(peer);
			}
			else
			{
				m_connectionStatus = ConnectionStatus.ErrorConnectFailed;
				ZLog.Log("Failed to connect to server");
			}
			m_serverConnector.Dispose();
			m_serverConnector = null;
		}
	}

	private void OnNewConnection(ZNetPeer peer)
	{
		m_peers.Add(peer);
		peer.m_rpc.Register<ZPackage>("PeerInfo", RPC_PeerInfo);
		peer.m_rpc.Register("Disconnect", RPC_Disconnect);
		if (m_isServer)
		{
			peer.m_rpc.Register("ServerHandshake", RPC_ServerHandshake);
			return;
		}
		peer.m_rpc.Register<int>("Error", RPC_Error);
		peer.m_rpc.Register<bool>("ClientHandshake", RPC_ClientHandshake);
		peer.m_rpc.Invoke("ServerHandshake");
	}

	private void RPC_ServerHandshake(ZRpc rpc)
	{
		ZNetPeer peer = GetPeer(rpc);
		if (peer != null)
		{
			ZLog.Log("Got handshake from client " + peer.m_socket.GetEndPointString());
			ClearPlayerData(peer);
			bool flag = !string.IsNullOrEmpty(m_serverPassword);
			peer.m_rpc.Invoke("ClientHandshake", flag);
		}
	}

	private void UpdatePassword()
	{
		if (m_passwordDialog.gameObject.activeSelf)
		{
			m_passwordDialog.GetComponentInChildren<InputField>().ActivateInputField();
		}
	}

	public bool InPasswordDialog()
	{
		return m_passwordDialog.gameObject.activeSelf;
	}

	private void RPC_ClientHandshake(ZRpc rpc, bool needPassword)
	{
		m_connectingDialog.gameObject.SetActive(value: false);
		if (needPassword)
		{
			m_passwordDialog.gameObject.SetActive(value: true);
			InputField componentInChildren = m_passwordDialog.GetComponentInChildren<InputField>();
			componentInChildren.text = "";
			componentInChildren.ActivateInputField();
			m_passwordDialog.GetComponentInChildren<InputFieldSubmit>().m_onSubmit = OnPasswordEnter;
			m_tempPasswordRPC = rpc;
		}
		else
		{
			SendPeerInfo(rpc);
		}
	}

	private void OnPasswordEnter(string pwd)
	{
		if (m_tempPasswordRPC.IsConnected())
		{
			m_passwordDialog.gameObject.SetActive(value: false);
			SendPeerInfo(m_tempPasswordRPC, pwd);
			m_tempPasswordRPC = null;
		}
	}

	public void OnPasswordOk()
	{
		InputFieldSubmit componentInChildren = m_passwordDialog.GetComponentInChildren<InputFieldSubmit>();
		OnPasswordEnter(componentInChildren.GetComponentInChildren<Text>().text);
	}

	private void SendPeerInfo(ZRpc rpc, string password = "")
	{
		ZPackage zPackage = new ZPackage();
		zPackage.Write(GetUID());
		zPackage.Write(Version.GetVersionString());
		zPackage.Write(m_referencePosition);
		zPackage.Write(Game.instance.GetPlayerProfile().GetName());
		if (IsServer())
		{
			zPackage.Write(m_world.m_name);
			zPackage.Write(m_world.m_seed);
			zPackage.Write(m_world.m_seedName);
			zPackage.Write(m_world.m_uid);
			zPackage.Write(m_world.m_worldGenVersion);
			zPackage.Write(m_netTime);
		}
		else
		{
			string data = (string.IsNullOrEmpty(password) ? "" : HashPassword(password));
			zPackage.Write(data);
			byte[] array = ZSteamMatchmaking.instance.RequestSessionTicket();
			if (array == null)
			{
				m_connectionStatus = ConnectionStatus.ErrorConnectFailed;
				return;
			}
			zPackage.Write(array);
		}
		rpc.Invoke("PeerInfo", zPackage);
	}

	private void RPC_PeerInfo(ZRpc rpc, ZPackage pkg)
	{
		ZNetPeer peer = GetPeer(rpc);
		if (peer == null)
		{
			return;
		}
		long num = pkg.ReadLong();
		string text = pkg.ReadString();
		string endPointString = peer.m_socket.GetEndPointString();
		string hostName = peer.m_socket.GetHostName();
		ZLog.Log("VERSION check their:" + text + "  mine:" + Version.GetVersionString());
		if (text != Version.GetVersionString())
		{
			if (m_isServer)
			{
				rpc.Invoke("Error", 3);
			}
			else
			{
				m_connectionStatus = ConnectionStatus.ErrorVersion;
			}
			ZLog.Log("Peer " + endPointString + " has incompatible version, mine:" + Version.GetVersionString() + " remote " + text);
			return;
		}
		Vector3 refPos = pkg.ReadVector3();
		string text2 = pkg.ReadString();
		if (m_isServer)
		{
			if (!IsAllowed(hostName, text2))
			{
				rpc.Invoke("Error", 8);
				ZLog.Log("Player " + text2 + " : " + hostName + " is blacklisted or not in whitelist.");
				return;
			}
			string text3 = pkg.ReadString();
			ZSteamSocket zSteamSocket = peer.m_socket as ZSteamSocket;
			byte[] ticket = pkg.ReadByteArray();
			if (!ZSteamMatchmaking.instance.VerifySessionTicket(ticket, zSteamSocket.GetPeerID()))
			{
				ZLog.Log("Peer " + endPointString + " has invalid session ticket");
				rpc.Invoke("Error", 8);
				return;
			}
			if (GetNrOfPlayers() >= m_serverPlayerLimit)
			{
				rpc.Invoke("Error", 9);
				ZLog.Log("Peer " + endPointString + " disconnected due to server is full");
				return;
			}
			if (m_serverPassword != text3)
			{
				rpc.Invoke("Error", 6);
				ZLog.Log("Peer " + endPointString + " has wrong password");
				return;
			}
			if (IsConnected(num))
			{
				rpc.Invoke("Error", 7);
				ZLog.Log("Already connected to peer with UID:" + num + "  " + endPointString);
				return;
			}
		}
		else
		{
			m_world = new World();
			m_world.m_name = pkg.ReadString();
			m_world.m_seed = pkg.ReadInt();
			m_world.m_seedName = pkg.ReadString();
			m_world.m_uid = pkg.ReadLong();
			m_world.m_worldGenVersion = pkg.ReadInt();
			WorldGenerator.Initialize(m_world);
			m_netTime = pkg.ReadDouble();
		}
		peer.m_refPos = refPos;
		peer.m_uid = num;
		peer.m_playerName = text2;
		rpc.Register<Vector3, bool>("RefPos", RPC_RefPos);
		rpc.Register<ZPackage>("PlayerList", RPC_PlayerList);
		rpc.Register<string>("RemotePrint", RPC_RemotePrint);
		if (m_isServer)
		{
			rpc.Register<ZDOID>("CharacterID", RPC_CharacterID);
			rpc.Register<string>("Kick", RPC_Kick);
			rpc.Register<string>("Ban", RPC_Ban);
			rpc.Register<string>("Unban", RPC_Unban);
			rpc.Register("Save", RPC_Save);
			rpc.Register("PrintBanned", RPC_PrintBanned);
		}
		else
		{
			rpc.Register<double>("NetTime", RPC_NetTime);
		}
		if (m_isServer)
		{
			SendPeerInfo(rpc);
			SendPlayerList();
		}
		else
		{
			m_connectionStatus = ConnectionStatus.Connected;
		}
		m_zdoMan.AddPeer(peer);
		m_routedRpc.AddPeer(peer);
	}

	private void SendDisconnect()
	{
		ZLog.Log("Sending disconnect msg");
		foreach (ZNetPeer peer in m_peers)
		{
			SendDisconnect(peer);
		}
	}

	private void SendDisconnect(ZNetPeer peer)
	{
		if (peer.m_rpc != null)
		{
			ZLog.Log("Sent to " + peer.m_socket.GetEndPointString());
			peer.m_rpc.Invoke("Disconnect");
		}
	}

	private void RPC_Disconnect(ZRpc rpc)
	{
		ZLog.Log("RPC_Disconnect ");
		ZNetPeer peer = GetPeer(rpc);
		if (peer != null)
		{
			if (peer.m_server)
			{
				m_connectionStatus = ConnectionStatus.ErrorDisconnected;
			}
			Disconnect(peer);
		}
	}

	private void RPC_Error(ZRpc rpc, int error)
	{
		ZLog.Log("Got connectoin error msg " + (m_connectionStatus = (ConnectionStatus)error));
	}

	public bool IsConnected(long uid)
	{
		if (uid == GetUID())
		{
			return true;
		}
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.m_uid == uid)
			{
				return true;
			}
		}
		return false;
	}

	private void ClearPlayerData(ZNetPeer peer)
	{
		m_routedRpc.RemovePeer(peer);
		m_zdoMan.RemovePeer(peer);
	}

	public void Disconnect(ZNetPeer peer)
	{
		ClearPlayerData(peer);
		m_peers.Remove(peer);
		peer.Dispose();
		if (m_isServer)
		{
			SendPlayerList();
		}
	}

	private void FixedUpdate()
	{
		UpdateNetTime(Time.fixedDeltaTime);
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		ZSteamSocket.UpdateAllSockets(deltaTime);
		if (IsServer())
		{
			UpdateBanList(deltaTime);
		}
		CheckForIncommingServerConnections();
		UpdatePeers(deltaTime);
		SendPeriodicData(deltaTime);
		m_zdoMan.Update(deltaTime);
		UpdateSave();
		UpdatePassword();
	}

	private void UpdateNetTime(float dt)
	{
		if (IsServer())
		{
			if (GetNrOfPlayers() > 0)
			{
				m_netTime += dt;
			}
		}
		else
		{
			m_netTime += dt;
		}
	}

	private void UpdateBanList(float dt)
	{
		m_banlistTimer += dt;
		if (!(m_banlistTimer > 5f))
		{
			return;
		}
		m_banlistTimer = 0f;
		CheckWhiteList();
		foreach (string item in m_bannedList.GetList())
		{
			InternalKick(item);
		}
	}

	private void CheckWhiteList()
	{
		if (m_permittedList.Count() == 0)
		{
			return;
		}
		bool flag = false;
		while (!flag)
		{
			flag = true;
			foreach (ZNetPeer peer in m_peers)
			{
				if (peer.IsReady())
				{
					string hostName = peer.m_socket.GetHostName();
					if (!m_permittedList.Contains(hostName))
					{
						ZLog.Log("Kicking player not in permitted list " + peer.m_playerName + " host: " + hostName);
						InternalKick(peer);
						flag = false;
						break;
					}
				}
			}
		}
	}

	public bool IsSaving()
	{
		return m_saveThread != null;
	}

	public void ConsoleSave()
	{
		if (IsServer())
		{
			RPC_Save(null);
		}
		else
		{
			GetServerRPC()?.Invoke("Save");
		}
	}

	private void RPC_Save(ZRpc rpc)
	{
		if (base.enabled)
		{
			if (rpc != null && !m_adminList.Contains(rpc.GetSocket().GetHostName()))
			{
				RemotePrint(rpc, "You are not admin");
				return;
			}
			RemotePrint(rpc, "Saving..");
			Save(sync: false);
		}
	}

	public void Save(bool sync)
	{
		if (m_loadError || ZoneSystem.instance.SkipSaving() || DungeonDB.instance.SkipSaving())
		{
			ZLog.LogWarning("Skipping world save");
		}
		else if (m_isServer && m_world != null)
		{
			SaveWorld(sync);
		}
	}

	private void SendPeriodicData(float dt)
	{
		m_periodicSendTimer += dt;
		if (!(m_periodicSendTimer >= 2f))
		{
			return;
		}
		m_periodicSendTimer = 0f;
		if (IsServer())
		{
			SendNetTime();
			SendPlayerList();
			return;
		}
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.IsReady())
			{
				peer.m_rpc.Invoke("RefPos", m_referencePosition, m_publicReferencePosition);
			}
		}
	}

	private void SendNetTime()
	{
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.IsReady())
			{
				peer.m_rpc.Invoke("NetTime", m_netTime);
			}
		}
	}

	private void RPC_NetTime(ZRpc rpc, double time)
	{
		m_netTime = time;
	}

	private void RPC_RefPos(ZRpc rpc, Vector3 pos, bool publicRefPos)
	{
		ZNetPeer peer = GetPeer(rpc);
		if (peer != null)
		{
			peer.m_refPos = pos;
			peer.m_publicRefPos = publicRefPos;
		}
	}

	private void UpdatePeers(float dt)
	{
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.m_rpc.IsConnected())
			{
				continue;
			}
			if (peer.m_server)
			{
				if (m_connectionStatus == ConnectionStatus.Connecting)
				{
					m_connectionStatus = ConnectionStatus.ErrorConnectFailed;
				}
				else
				{
					m_connectionStatus = ConnectionStatus.ErrorDisconnected;
				}
			}
			Disconnect(peer);
			break;
		}
		ZNetPeer[] array = m_peers.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].m_rpc.Update(dt);
		}
	}

	private void CheckForIncommingServerConnections()
	{
		if (m_hostSocket == null)
		{
			return;
		}
		ISocket socket = m_hostSocket.Accept();
		if (socket != null)
		{
			if (!socket.IsConnected())
			{
				socket.Dispose();
				return;
			}
			ZNetPeer peer = new ZNetPeer(socket, server: false);
			OnNewConnection(peer);
		}
	}

	public ZNetPeer GetPeerByPlayerName(string name)
	{
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.IsReady() && peer.m_playerName == name)
			{
				return peer;
			}
		}
		return null;
	}

	public ZNetPeer GetPeerByHostName(string endpoint)
	{
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.IsReady() && peer.m_socket.GetHostName() == endpoint)
			{
				return peer;
			}
		}
		return null;
	}

	public ZNetPeer GetPeer(long uid)
	{
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.m_uid == uid)
			{
				return peer;
			}
		}
		return null;
	}

	private ZNetPeer GetPeer(ZRpc rpc)
	{
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.m_rpc == rpc)
			{
				return peer;
			}
		}
		return null;
	}

	public List<ZNetPeer> GetConnectedPeers()
	{
		return new List<ZNetPeer>(m_peers);
	}

	private void SaveWorld(bool sync)
	{
		if (m_saveThread != null && m_saveThread.IsAlive)
		{
			m_saveThread.Join();
			m_saveThread = null;
		}
		m_saveStartTime = Time.realtimeSinceStartup;
		m_zdoMan.PrepareSave();
		ZoneSystem.instance.PrepareSave();
		RandEventSystem.instance.PrepareSave();
		m_saveThreadStartTime = Time.realtimeSinceStartup;
		m_saveThread = new Thread(SaveWorldThread);
		m_saveThread.Start();
		if (sync)
		{
			m_saveThread.Join();
			m_saveThread = null;
		}
	}

	private void UpdateSave()
	{
		if (m_saveThread != null && !m_saveThread.IsAlive)
		{
			m_saveThread = null;
			float num = m_saveThreadStartTime - m_saveStartTime;
			float num2 = Time.realtimeSinceStartup - m_saveThreadStartTime;
			MessageHud.instance.MessageAll(MessageHud.MessageType.TopLeft, "$msg_worldsaved ( " + num.ToString("0.00") + "+" + num2.ToString("0.00") + "s )");
		}
	}

	private void SaveWorldThread()
	{
		DateTime now = DateTime.Now;
		string dBPath = m_world.GetDBPath();
		string text = dBPath + ".new";
		string text2 = dBPath + ".old";
		FileStream fileStream = File.Create(text);
		BinaryWriter binaryWriter = new BinaryWriter(fileStream);
		binaryWriter.Write(Version.m_worldVersion);
		binaryWriter.Write(m_netTime);
		m_zdoMan.SaveAsync(binaryWriter);
		ZoneSystem.instance.SaveASync(binaryWriter);
		RandEventSystem.instance.SaveAsync(binaryWriter);
		binaryWriter.Flush();
		fileStream.Flush(flushToDisk: true);
		fileStream.Close();
		fileStream.Dispose();
		m_world.SaveWorldMetaData();
		if (File.Exists(dBPath))
		{
			if (File.Exists(text2))
			{
				File.Delete(text2);
			}
			File.Move(dBPath, text2);
		}
		File.Move(text, dBPath);
		ZLog.Log("World saved ( " + (DateTime.Now - now).TotalMilliseconds + "ms )");
	}

	private void LoadWorld()
	{
		ZLog.Log("Load world " + m_world.m_name);
		string dBPath = m_world.GetDBPath();
		FileStream fileStream;
		try
		{
			fileStream = File.OpenRead(dBPath);
		}
		catch
		{
			ZLog.Log("  missing " + dBPath);
			return;
		}
		BinaryReader binaryReader = new BinaryReader(fileStream);
		try
		{
			if (!CheckDataVersion(binaryReader, out var version))
			{
				ZLog.Log("  incompatible data version " + version);
				binaryReader.Close();
				fileStream.Dispose();
				return;
			}
			if (version >= 4)
			{
				m_netTime = binaryReader.ReadDouble();
			}
			m_zdoMan.Load(binaryReader, version);
			if (version >= 12)
			{
				ZoneSystem.instance.Load(binaryReader, version);
			}
			if (version >= 15)
			{
				RandEventSystem.instance.Load(binaryReader, version);
			}
			binaryReader.Close();
			fileStream.Dispose();
		}
		catch (Exception ex)
		{
			ZLog.LogError("Exception while loading world " + dBPath + ":" + ex.ToString());
			m_loadError = true;
			Application.Quit();
		}
		GC.Collect();
	}

	private bool CheckDataVersion(BinaryReader reader, out int version)
	{
		version = reader.ReadInt32();
		if (!Version.IsWorldVersionCompatible(version))
		{
			return false;
		}
		return true;
	}

	public int GetHostPort()
	{
		if (m_hostSocket != null)
		{
			return m_hostSocket.GetHostPort();
		}
		return 0;
	}

	public long GetUID()
	{
		return m_zdoMan.GetMyID();
	}

	public long GetWorldUID()
	{
		return m_world.m_uid;
	}

	public string GetWorldName()
	{
		if (m_world != null)
		{
			return m_world.m_name;
		}
		return null;
	}

	public void SetCharacterID(ZDOID id)
	{
		m_characterID = id;
		if (!m_isServer)
		{
			m_peers[0].m_rpc.Invoke("CharacterID", id);
		}
	}

	private void RPC_CharacterID(ZRpc rpc, ZDOID characterID)
	{
		ZNetPeer peer = GetPeer(rpc);
		if (peer != null)
		{
			peer.m_characterID = characterID;
			ZLog.Log("Got character ZDOID from " + peer.m_playerName + " : " + characterID);
		}
	}

	public void SetPublicReferencePosition(bool pub)
	{
		m_publicReferencePosition = pub;
	}

	public bool IsReferencePositionPublic()
	{
		return m_publicReferencePosition;
	}

	public void SetReferencePosition(Vector3 pos)
	{
		m_referencePosition = pos;
	}

	public Vector3 GetReferencePosition()
	{
		return m_referencePosition;
	}

	public List<ZDO> GetAllCharacterZDOS()
	{
		List<ZDO> list = new List<ZDO>();
		ZDO zDO = m_zdoMan.GetZDO(m_characterID);
		if (zDO != null)
		{
			list.Add(zDO);
		}
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.IsReady() && !peer.m_characterID.IsNone())
			{
				ZDO zDO2 = m_zdoMan.GetZDO(peer.m_characterID);
				if (zDO2 != null)
				{
					list.Add(zDO2);
				}
			}
		}
		return list;
	}

	public int GetPeerConnections()
	{
		int num = 0;
		for (int i = 0; i < m_peers.Count; i++)
		{
			if (m_peers[i].IsReady())
			{
				num++;
			}
		}
		return num;
	}

	public ZNat GetZNat()
	{
		return m_nat;
	}

	public static void SetServer(bool server, bool openServer, bool publicServer, string serverName, string password, World world)
	{
		m_isServer = server;
		m_openServer = openServer;
		m_publicServer = publicServer;
		m_serverPassword = (string.IsNullOrEmpty(password) ? "" : HashPassword(password));
		m_ServerName = serverName;
		m_world = world;
	}

	private static string HashPassword(string password)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(password);
		byte[] bytes2 = new MD5CryptoServiceProvider().ComputeHash(bytes);
		return Encoding.ASCII.GetString(bytes2);
	}

	public static void ResetServerHost()
	{
		m_serverSteamID = 0uL;
		m_serverIPAddr.Clear();
	}

	public static void SetServerHost(ulong serverID)
	{
		m_serverSteamID = serverID;
		m_serverIPAddr.Clear();
	}

	public static void SetServerHost(SteamNetworkingIPAddr serverAddr)
	{
		m_serverSteamID = 0uL;
		m_serverIPAddr = serverAddr;
	}

	public static string GetServerString()
	{
		m_serverIPAddr.ToString(out var buf, bWithPort: true);
		return m_serverSteamID + "/" + buf;
	}

	public bool IsServer()
	{
		return m_isServer;
	}

	public bool IsDedicated()
	{
		return false;
	}

	private void UpdatePlayerList()
	{
		m_players.Clear();
		if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
		{
			PlayerInfo item = default(PlayerInfo);
			item.m_name = Game.instance.GetPlayerProfile().GetName();
			item.m_host = "";
			item.m_characterID = m_characterID;
			item.m_publicPosition = m_publicReferencePosition;
			if (item.m_publicPosition)
			{
				item.m_position = m_referencePosition;
			}
			m_players.Add(item);
		}
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.IsReady())
			{
				PlayerInfo item2 = default(PlayerInfo);
				item2.m_characterID = peer.m_characterID;
				item2.m_name = peer.m_playerName;
				item2.m_host = peer.m_socket.GetHostName();
				item2.m_publicPosition = peer.m_publicRefPos;
				if (item2.m_publicPosition)
				{
					item2.m_position = peer.m_refPos;
				}
				m_players.Add(item2);
			}
		}
	}

	private void SendPlayerList()
	{
		UpdatePlayerList();
		if (m_peers.Count <= 0)
		{
			return;
		}
		ZPackage zPackage = new ZPackage();
		zPackage.Write(m_players.Count);
		foreach (PlayerInfo player in m_players)
		{
			zPackage.Write(player.m_name);
			zPackage.Write(player.m_host);
			zPackage.Write(player.m_characterID);
			zPackage.Write(player.m_publicPosition);
			if (player.m_publicPosition)
			{
				zPackage.Write(player.m_position);
			}
		}
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.IsReady())
			{
				peer.m_rpc.Invoke("PlayerList", zPackage);
			}
		}
	}

	private void RPC_PlayerList(ZRpc rpc, ZPackage pkg)
	{
		m_players.Clear();
		int num = pkg.ReadInt();
		for (int i = 0; i < num; i++)
		{
			PlayerInfo item = default(PlayerInfo);
			item.m_name = pkg.ReadString();
			item.m_host = pkg.ReadString();
			item.m_characterID = pkg.ReadZDOID();
			item.m_publicPosition = pkg.ReadBool();
			if (item.m_publicPosition)
			{
				item.m_position = pkg.ReadVector3();
			}
			m_players.Add(item);
		}
	}

	public List<PlayerInfo> GetPlayerList()
	{
		return m_players;
	}

	public void GetOtherPublicPlayers(List<PlayerInfo> playerList)
	{
		foreach (PlayerInfo player in m_players)
		{
			if (player.m_publicPosition)
			{
				ZDOID characterID = player.m_characterID;
				if (!characterID.IsNone() && !(player.m_characterID == m_characterID))
				{
					playerList.Add(player);
				}
			}
		}
	}

	public int GetNrOfPlayers()
	{
		return m_players.Count;
	}

	public void GetNetStats(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
	{
		localQuality = 0f;
		remoteQuality = 0f;
		ping = 0;
		outByteSec = 0f;
		inByteSec = 0f;
		if (IsServer())
		{
			int num = 0;
			foreach (ZNetPeer peer in m_peers)
			{
				if (peer.IsReady())
				{
					num++;
					peer.m_socket.GetConnectionQuality(out var localQuality2, out var remoteQuality2, out var ping2, out var outByteSec2, out var inByteSec2);
					localQuality += localQuality2;
					remoteQuality += remoteQuality2;
					ping += ping2;
					outByteSec += outByteSec2;
					inByteSec += inByteSec2;
				}
			}
			if (num > 0)
			{
				localQuality /= num;
				remoteQuality /= num;
				ping /= num;
			}
		}
		else
		{
			if (m_connectionStatus != ConnectionStatus.Connected)
			{
				return;
			}
			foreach (ZNetPeer peer2 in m_peers)
			{
				if (peer2.IsReady())
				{
					peer2.m_socket.GetConnectionQuality(out localQuality, out remoteQuality, out ping, out outByteSec, out inByteSec);
					break;
				}
			}
		}
	}

	public void SetNetTime(double time)
	{
		m_netTime = time;
	}

	public DateTime GetTime()
	{
		long ticks = (long)(m_netTime * 1000.0 * 10000.0);
		return new DateTime(ticks);
	}

	public float GetWrappedDayTimeSeconds()
	{
		return (float)(m_netTime % 86400.0);
	}

	public double GetTimeSeconds()
	{
		return m_netTime;
	}

	public static ConnectionStatus GetConnectionStatus()
	{
		if (m_instance != null && m_instance.IsServer())
		{
			return ConnectionStatus.Connected;
		}
		return m_connectionStatus;
	}

	public bool HasBadConnection()
	{
		return GetServerPing() > m_badConnectionPing;
	}

	public float GetServerPing()
	{
		if (IsServer())
		{
			return 0f;
		}
		if (m_connectionStatus == ConnectionStatus.Connecting || m_connectionStatus == ConnectionStatus.None)
		{
			return 0f;
		}
		if (m_connectionStatus == ConnectionStatus.Connected)
		{
			foreach (ZNetPeer peer in m_peers)
			{
				if (peer.IsReady())
				{
					return peer.m_rpc.GetTimeSinceLastPing();
				}
			}
		}
		return 0f;
	}

	public ZNetPeer GetServerPeer()
	{
		if (IsServer())
		{
			return null;
		}
		if (m_connectionStatus == ConnectionStatus.Connecting || m_connectionStatus == ConnectionStatus.None)
		{
			return null;
		}
		if (m_connectionStatus == ConnectionStatus.Connected)
		{
			foreach (ZNetPeer peer in m_peers)
			{
				if (peer.IsReady())
				{
					return peer;
				}
			}
		}
		return null;
	}

	public ZRpc GetServerRPC()
	{
		return GetServerPeer()?.m_rpc;
	}

	public List<ZNetPeer> GetPeers()
	{
		return m_peers;
	}

	public void RemotePrint(ZRpc rpc, string text)
	{
		if (rpc == null)
		{
			if ((bool)Console.instance)
			{
				Console.instance.Print(text);
			}
		}
		else
		{
			rpc.Invoke("RemotePrint", text);
		}
	}

	private void RPC_RemotePrint(ZRpc rpc, string text)
	{
		if ((bool)Console.instance)
		{
			Console.instance.Print(text);
		}
	}

	public void Kick(string user)
	{
		if (IsServer())
		{
			InternalKick(user);
			return;
		}
		GetServerRPC()?.Invoke("Kick", user);
	}

	private void RPC_Kick(ZRpc rpc, string user)
	{
		if (!m_adminList.Contains(rpc.GetSocket().GetHostName()))
		{
			RemotePrint(rpc, "You are not admin");
			return;
		}
		RemotePrint(rpc, "Kicking user " + user);
		InternalKick(user);
	}

	private void InternalKick(string user)
	{
		if (!(user == ""))
		{
			ZNetPeer zNetPeer = GetPeerByHostName(user);
			if (zNetPeer == null)
			{
				zNetPeer = GetPeerByPlayerName(user);
			}
			if (zNetPeer != null)
			{
				InternalKick(zNetPeer);
			}
		}
	}

	private void InternalKick(ZNetPeer peer)
	{
		if (IsServer() && peer != null)
		{
			ZLog.Log("Kicking " + peer.m_playerName);
			SendDisconnect(peer);
			Disconnect(peer);
		}
	}

	public bool IsAllowed(string hostName, string playerName)
	{
		if (m_bannedList.Contains(hostName) || m_bannedList.Contains(playerName))
		{
			return false;
		}
		if (m_permittedList.Count() > 0 && !m_permittedList.Contains(hostName))
		{
			return false;
		}
		return true;
	}

	public void Ban(string user)
	{
		if (IsServer())
		{
			InternalBan(null, user);
			return;
		}
		GetServerRPC()?.Invoke("Ban", user);
	}

	private void RPC_Ban(ZRpc rpc, string user)
	{
		if (!m_adminList.Contains(rpc.GetSocket().GetHostName()))
		{
			RemotePrint(rpc, "You are not admin");
		}
		else
		{
			InternalBan(rpc, user);
		}
	}

	private void InternalBan(ZRpc rpc, string user)
	{
		if (IsServer() && !(user == ""))
		{
			ZNetPeer peerByPlayerName = GetPeerByPlayerName(user);
			if (peerByPlayerName != null)
			{
				user = peerByPlayerName.m_socket.GetHostName();
			}
			RemotePrint(rpc, "Banning user " + user);
			m_bannedList.Add(user);
		}
	}

	public void Unban(string user)
	{
		if (IsServer())
		{
			InternalUnban(null, user);
			return;
		}
		GetServerRPC()?.Invoke("Unban", user);
	}

	private void RPC_Unban(ZRpc rpc, string user)
	{
		if (!m_adminList.Contains(rpc.GetSocket().GetHostName()))
		{
			RemotePrint(rpc, "You are not admin");
		}
		else
		{
			InternalUnban(rpc, user);
		}
	}

	private void InternalUnban(ZRpc rpc, string user)
	{
		if (IsServer() && !(user == ""))
		{
			RemotePrint(rpc, "Unbanning user " + user);
			m_bannedList.Remove(user);
		}
	}

	public void PrintBanned()
	{
		if (IsServer())
		{
			InternalPrintBanned(null);
		}
		else
		{
			GetServerRPC()?.Invoke("PrintBanned");
		}
	}

	private void RPC_PrintBanned(ZRpc rpc)
	{
		if (!m_adminList.Contains(rpc.GetSocket().GetHostName()))
		{
			RemotePrint(rpc, "You are not admin");
		}
		else
		{
			InternalPrintBanned(rpc);
		}
	}

	private void InternalPrintBanned(ZRpc rpc)
	{
		RemotePrint(rpc, "Banned users");
		List<string> list = m_bannedList.GetList();
		if (list.Count == 0)
		{
			RemotePrint(rpc, "-");
		}
		else
		{
			for (int i = 0; i < list.Count; i++)
			{
				RemotePrint(rpc, i + ": " + list[i]);
			}
		}
		RemotePrint(rpc, "");
		RemotePrint(rpc, "Permitted users");
		List<string> list2 = m_permittedList.GetList();
		if (list2.Count == 0)
		{
			RemotePrint(rpc, "All");
			return;
		}
		for (int j = 0; j < list2.Count; j++)
		{
			RemotePrint(rpc, j + ": " + list2[j]);
		}
	}
}
