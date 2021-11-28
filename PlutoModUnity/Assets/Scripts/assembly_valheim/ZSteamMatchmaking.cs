using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Steamworks;

public class ZSteamMatchmaking
{
	private static ZSteamMatchmaking m_instance;

	private const int maxServers = 200;

	private List<ServerData> m_matchmakingServers = new List<ServerData>();

	private List<ServerData> m_dedicatedServers = new List<ServerData>();

	private List<ServerData> m_friendServers = new List<ServerData>();

	private int m_serverListRevision;

	private int m_updateTriggerAccumulator;

	private CallResult<LobbyCreated_t> m_lobbyCreated;

	private CallResult<LobbyMatchList_t> m_lobbyMatchList;

	private CallResult<LobbyEnter_t> m_lobbyEntered;

	private Callback<GameServerChangeRequested_t> m_changeServer;

	private Callback<GameLobbyJoinRequested_t> m_joinRequest;

	private Callback<LobbyDataUpdate_t> m_lobbyDataUpdate;

	private Callback<GetAuthSessionTicketResponse_t> m_authSessionTicketResponse;

	private Callback<SteamServerConnectFailure_t> m_steamServerConnectFailure;

	private Callback<SteamServersConnected_t> m_steamServersConnected;

	private Callback<SteamServersDisconnected_t> m_steamServersDisconnected;

	private CSteamID m_myLobby = CSteamID.Nil;

	private CSteamID m_joinUserID = CSteamID.Nil;

	private CSteamID m_queuedJoinLobby = CSteamID.Nil;

	private bool m_haveJoinAddr;

	private SteamNetworkingIPAddr m_joinAddr;

	private List<KeyValuePair<CSteamID, string>> m_requestedFriendGames = new List<KeyValuePair<CSteamID, string>>();

	private ISteamMatchmakingServerListResponse m_steamServerCallbackHandler;

	private ISteamMatchmakingPingResponse m_joinServerCallbackHandler;

	private HServerQuery m_joinQuery;

	private HServerListRequest m_serverListRequest;

	private bool m_haveListRequest;

	private bool m_refreshingDedicatedServers;

	private bool m_refreshingPublicGames;

	private string m_registerServerName = "";

	private bool m_registerPassword;

	private string m_registerVerson = "";

	private string m_nameFilter = "";

	private bool m_friendsFilter = true;

	private HAuthTicket m_authTicket = HAuthTicket.Invalid;

	public static ZSteamMatchmaking instance => m_instance;

	public static void Initialize()
	{
		if (m_instance == null)
		{
			m_instance = new ZSteamMatchmaking();
		}
	}

	private ZSteamMatchmaking()
	{
		m_steamServerCallbackHandler = new ISteamMatchmakingServerListResponse(OnServerResponded, OnServerFailedToRespond, OnRefreshComplete);
		m_joinServerCallbackHandler = new ISteamMatchmakingPingResponse(OnJoinServerRespond, OnJoinServerFailed);
		m_lobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
		m_lobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
		m_changeServer = Callback<GameServerChangeRequested_t>.Create(OnChangeServerRequest);
		m_joinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
		m_lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
		m_authSessionTicketResponse = Callback<GetAuthSessionTicketResponse_t>.Create(OnAuthSessionTicketResponse);
	}

	public byte[] RequestSessionTicket()
	{
		ReleaseSessionTicket();
		byte[] array = new byte[1024];
		uint pcbTicket = 0u;
		m_authTicket = SteamUser.GetAuthSessionTicket(array, 1024, out pcbTicket);
		if (m_authTicket == HAuthTicket.Invalid)
		{
			return null;
		}
		byte[] array2 = new byte[pcbTicket];
		Buffer.BlockCopy(array, 0, array2, 0, (int)pcbTicket);
		return array2;
	}

	public void ReleaseSessionTicket()
	{
		if (!(m_authTicket == HAuthTicket.Invalid))
		{
			SteamUser.CancelAuthTicket(m_authTicket);
			m_authTicket = HAuthTicket.Invalid;
			ZLog.Log("Released session ticket");
		}
	}

	public bool VerifySessionTicket(byte[] ticket, CSteamID steamID)
	{
		return SteamUser.BeginAuthSession(ticket, ticket.Length, steamID) == EBeginAuthSessionResult.k_EBeginAuthSessionResultOK;
	}

	private void OnAuthSessionTicketResponse(GetAuthSessionTicketResponse_t data)
	{
		ZLog.Log("Session auth respons callback");
	}

	private void OnSteamServersConnected(SteamServersConnected_t data)
	{
		ZLog.Log("Game server connected");
	}

	private void OnSteamServersDisconnected(SteamServersDisconnected_t data)
	{
		ZLog.LogWarning("Game server disconnected");
	}

	private void OnSteamServersConnectFail(SteamServerConnectFailure_t data)
	{
		ZLog.LogWarning("Game server connected failed");
	}

	private void OnChangeServerRequest(GameServerChangeRequested_t data)
	{
		ZLog.Log("ZSteamMatchmaking got change server request to:" + data.m_rgchServer);
		QueueServerJoin(data.m_rgchServer);
	}

	private void OnJoinRequest(GameLobbyJoinRequested_t data)
	{
		ZLog.Log(string.Concat("ZSteamMatchmaking got join request friend:", data.m_steamIDFriend, "  lobby:", data.m_steamIDLobby));
		if (!Game.instance)
		{
			QueueLobbyJoin(data.m_steamIDLobby);
		}
	}

	private IPAddress FindIP(string host)
	{
		try
		{
			if (IPAddress.TryParse(host, out var address))
			{
				return address;
			}
			ZLog.Log("Not an ip address " + host + " doing dns lookup");
			IPHostEntry hostEntry = Dns.GetHostEntry(host);
			if (hostEntry.AddressList.Length == 0)
			{
				ZLog.Log("Dns lookup failed");
				return null;
			}
			ZLog.Log("Got dns entries: " + hostEntry.AddressList.Length);
			IPAddress[] addressList = hostEntry.AddressList;
			foreach (IPAddress iPAddress in addressList)
			{
				if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
				{
					return iPAddress;
				}
			}
			return null;
		}
		catch (Exception ex)
		{
			ZLog.Log("Exception while finding ip:" + ex.ToString());
			return null;
		}
	}

	public void QueueServerJoin(string addr)
	{
		try
		{
			string[] array = addr.Split(':');
			if (array.Length >= 2)
			{
				IPAddress iPAddress = FindIP(array[0]);
				if (iPAddress == null)
				{
					ZLog.Log("Invalid address " + array[0]);
					return;
				}
				uint nIP = (uint)IPAddress.HostToNetworkOrder(BitConverter.ToInt32(iPAddress.GetAddressBytes(), 0));
				int num = int.Parse(array[1]);
				ZLog.Log("connect to ip:" + iPAddress.ToString() + " port:" + num);
				m_joinAddr.SetIPv4(nIP, (ushort)num);
				m_haveJoinAddr = true;
			}
		}
		catch (Exception ex)
		{
			ZLog.Log("Server join exception:" + ex);
		}
	}

	private void OnJoinServerRespond(gameserveritem_t serverData)
	{
		ZLog.Log("Got join server data " + serverData.GetServerName() + "  " + serverData.m_steamID);
		m_joinAddr.SetIPv4(serverData.m_NetAdr.GetIP(), serverData.m_NetAdr.GetConnectionPort());
		m_haveJoinAddr = true;
	}

	private void OnJoinServerFailed()
	{
		ZLog.Log("Failed to get join server data");
	}

	public void QueueLobbyJoin(CSteamID lobbyID)
	{
		if (SteamMatchmaking.GetLobbyGameServer(lobbyID, out var _, out var _, out var psteamIDGameServer))
		{
			ZLog.Log("  hostid: " + psteamIDGameServer);
			m_joinUserID = psteamIDGameServer;
			m_queuedJoinLobby = CSteamID.Nil;
		}
		else
		{
			ZLog.Log(string.Concat("Failed to get lobby data for lobby ", lobbyID, ", requesting lobby data"));
			m_queuedJoinLobby = lobbyID;
			SteamMatchmaking.RequestLobbyData(lobbyID);
		}
	}

	private void OnLobbyDataUpdate(LobbyDataUpdate_t data)
	{
		CSteamID cSteamID = new CSteamID(data.m_ulSteamIDLobby);
		if (cSteamID == m_queuedJoinLobby)
		{
			ZLog.Log("Got lobby data, for queued lobby");
			if (SteamMatchmaking.GetLobbyGameServer(cSteamID, out var _, out var _, out var psteamIDGameServer))
			{
				m_joinUserID = psteamIDGameServer;
			}
			m_queuedJoinLobby = CSteamID.Nil;
			return;
		}
		ZLog.Log("Got requested lobby data");
		foreach (KeyValuePair<CSteamID, string> requestedFriendGame in m_requestedFriendGames)
		{
			if (requestedFriendGame.Key == cSteamID)
			{
				ServerData lobbyServerData = GetLobbyServerData(cSteamID);
				if (lobbyServerData != null)
				{
					lobbyServerData.m_name = requestedFriendGame.Value + " [" + lobbyServerData.m_name + "]";
					m_friendServers.Add(lobbyServerData);
					m_serverListRevision++;
				}
			}
		}
	}

	public void RegisterServer(string name, bool password, string version, bool publicServer, string worldName)
	{
		UnregisterServer();
		SteamAPICall_t hAPICall = SteamMatchmaking.CreateLobby((!publicServer) ? ELobbyType.k_ELobbyTypeFriendsOnly : ELobbyType.k_ELobbyTypePublic, 32);
		m_lobbyCreated.Set(hAPICall);
		m_registerServerName = name;
		m_registerPassword = password;
		m_registerVerson = version;
		ZLog.Log("Registering lobby");
	}

	private void OnLobbyCreated(LobbyCreated_t data, bool ioError)
	{
		ZLog.Log(string.Concat("Lobby was created ", data.m_eResult, "  ", data.m_ulSteamIDLobby, "  error:", ioError.ToString()));
		if (!ioError)
		{
			m_myLobby = new CSteamID(data.m_ulSteamIDLobby);
			SteamMatchmaking.SetLobbyData(m_myLobby, "name", m_registerServerName);
			SteamMatchmaking.SetLobbyData(m_myLobby, "password", m_registerPassword ? "1" : "0");
			SteamMatchmaking.SetLobbyData(m_myLobby, "version", m_registerVerson);
			SteamMatchmaking.SetLobbyGameServer(m_myLobby, 0u, 0, SteamUser.GetSteamID());
		}
	}

	private void OnLobbyEnter(LobbyEnter_t data, bool ioError)
	{
		ZLog.LogWarning("Entering lobby " + data.m_ulSteamIDLobby);
	}

	public void UnregisterServer()
	{
		if (m_myLobby != CSteamID.Nil)
		{
			SteamMatchmaking.SetLobbyJoinable(m_myLobby, bLobbyJoinable: false);
			SteamMatchmaking.LeaveLobby(m_myLobby);
			m_myLobby = CSteamID.Nil;
		}
	}

	public void RequestServerlist()
	{
		RequestFriendGames();
		RequestPublicLobbies();
		RequestDedicatedServers();
	}

	public void StopServerListing()
	{
		if (m_haveListRequest)
		{
			SteamMatchmakingServers.ReleaseRequest(m_serverListRequest);
			m_haveListRequest = false;
		}
	}

	private void RequestFriendGames()
	{
		m_friendServers.Clear();
		m_requestedFriendGames.Clear();
		int num = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
		if (num == -1)
		{
			ZLog.Log("GetFriendCount returned -1, the current user is not logged in.");
			num = 0;
		}
		for (int i = 0; i < num; i++)
		{
			CSteamID friendByIndex = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
			string friendPersonaName = SteamFriends.GetFriendPersonaName(friendByIndex);
			if (SteamFriends.GetFriendGamePlayed(friendByIndex, out var pFriendGameInfo) && pFriendGameInfo.m_gameID == (CGameID)SteamManager.APP_ID && pFriendGameInfo.m_steamIDLobby != CSteamID.Nil)
			{
				ZLog.Log("Friend is in our game");
				m_requestedFriendGames.Add(new KeyValuePair<CSteamID, string>(pFriendGameInfo.m_steamIDLobby, friendPersonaName));
				SteamMatchmaking.RequestLobbyData(pFriendGameInfo.m_steamIDLobby);
			}
		}
		m_serverListRevision++;
	}

	private void RequestPublicLobbies()
	{
		SteamAPICall_t hAPICall = SteamMatchmaking.RequestLobbyList();
		m_lobbyMatchList.Set(hAPICall);
		m_refreshingPublicGames = true;
	}

	private void RequestDedicatedServers()
	{
		if (m_haveListRequest)
		{
			SteamMatchmakingServers.ReleaseRequest(m_serverListRequest);
			m_haveListRequest = false;
		}
		m_dedicatedServers.Clear();
		m_serverListRequest = SteamMatchmakingServers.RequestInternetServerList(SteamUtils.GetAppID(), new MatchMakingKeyValuePair_t[0], 0u, m_steamServerCallbackHandler);
		m_haveListRequest = true;
	}

	private void OnLobbyMatchList(LobbyMatchList_t data, bool ioError)
	{
		m_refreshingPublicGames = false;
		m_matchmakingServers.Clear();
		for (int i = 0; i < data.m_nLobbiesMatching; i++)
		{
			CSteamID lobbyByIndex = SteamMatchmaking.GetLobbyByIndex(i);
			ServerData lobbyServerData = GetLobbyServerData(lobbyByIndex);
			if (lobbyServerData != null)
			{
				m_matchmakingServers.Add(lobbyServerData);
			}
		}
		m_serverListRevision++;
	}

	private ServerData GetLobbyServerData(CSteamID lobbyID)
	{
		string lobbyData = SteamMatchmaking.GetLobbyData(lobbyID, "name");
		bool password = SteamMatchmaking.GetLobbyData(lobbyID, "password") == "1";
		string lobbyData2 = SteamMatchmaking.GetLobbyData(lobbyID, "version");
		int numLobbyMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
		if (SteamMatchmaking.GetLobbyGameServer(lobbyID, out var _, out var _, out var psteamIDGameServer))
		{
			return new ServerData
			{
				m_name = lobbyData,
				m_password = password,
				m_version = lobbyData2,
				m_players = numLobbyMembers,
				m_steamHostID = (ulong)psteamIDGameServer
			};
		}
		ZLog.Log("Failed to get lobby gameserver");
		return null;
	}

	public void GetServers(List<ServerData> allServers)
	{
		if (m_friendsFilter)
		{
			FilterServers(m_friendServers, allServers);
			return;
		}
		FilterServers(m_matchmakingServers, allServers);
		FilterServers(m_dedicatedServers, allServers);
	}

	private void FilterServers(List<ServerData> input, List<ServerData> allServers)
	{
		string text = m_nameFilter.ToLowerInvariant();
		foreach (ServerData item in input)
		{
			if (text.Length == 0 || item.m_name.ToLowerInvariant().Contains(text))
			{
				allServers.Add(item);
			}
			if (allServers.Count >= 200)
			{
				break;
			}
		}
	}

	public bool GetJoinHost(out CSteamID steamID, out SteamNetworkingIPAddr addr)
	{
		steamID = m_joinUserID;
		addr = m_joinAddr;
		if (m_joinUserID.IsValid() || m_haveJoinAddr)
		{
			m_joinUserID = CSteamID.Nil;
			m_haveJoinAddr = false;
			m_joinAddr.Clear();
			return true;
		}
		return false;
	}

	private void OnServerResponded(HServerListRequest request, int iServer)
	{
		gameserveritem_t serverDetails = SteamMatchmakingServers.GetServerDetails(request, iServer);
		string serverName = serverDetails.GetServerName();
		ServerData serverData = new ServerData();
		serverData.m_name = serverName;
		serverData.m_steamHostAddr.SetIPv4(serverDetails.m_NetAdr.GetIP(), serverDetails.m_NetAdr.GetConnectionPort());
		serverData.m_password = serverDetails.m_bPassword;
		serverData.m_players = serverDetails.m_nPlayers;
		serverData.m_version = serverDetails.GetGameTags();
		m_dedicatedServers.Add(serverData);
		m_updateTriggerAccumulator++;
		if (m_updateTriggerAccumulator > 100)
		{
			m_updateTriggerAccumulator = 0;
			m_serverListRevision++;
		}
	}

	private void OnServerFailedToRespond(HServerListRequest request, int iServer)
	{
	}

	private void OnRefreshComplete(HServerListRequest request, EMatchMakingServerResponse response)
	{
		ZLog.Log("Refresh complete " + m_dedicatedServers.Count + "  " + response);
		m_serverListRevision++;
	}

	public void SetNameFilter(string filter)
	{
		if (!(m_nameFilter == filter))
		{
			m_nameFilter = filter;
			m_serverListRevision++;
		}
	}

	public void SetFriendFilter(bool enabled)
	{
		if (m_friendsFilter != enabled)
		{
			m_friendsFilter = enabled;
			m_serverListRevision++;
		}
	}

	public int GetServerListRevision()
	{
		return m_serverListRevision;
	}

	public int GetTotalNrOfServers()
	{
		return m_matchmakingServers.Count + m_dedicatedServers.Count + m_friendServers.Count;
	}
}
