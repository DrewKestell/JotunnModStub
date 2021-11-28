using UnityEngine;

public class KeyHints : MonoBehaviour
{
	private static KeyHints m_instance;

	[Header("Key hints")]
	public GameObject m_buildHints;

	public GameObject m_combatHints;

	public GameObject m_primaryAttackGP;

	public GameObject m_primaryAttackKB;

	public GameObject m_secondaryAttackGP;

	public GameObject m_secondaryAttackKB;

	public GameObject m_bowDrawGP;

	public GameObject m_bowDrawKB;

	private bool m_keyHintsEnabled = true;

	public static KeyHints instance => m_instance;

	private void OnDestroy()
	{
		m_instance = null;
	}

	private void Awake()
	{
		m_instance = this;
		ApplySettings();
	}

	private void Start()
	{
	}

	public void ApplySettings()
	{
		m_keyHintsEnabled = PlayerPrefs.GetInt("KeyHints", 1) == 1;
	}

	private void Update()
	{
		UpdateHints();
	}

	private void UpdateHints()
	{
		Player localPlayer = Player.m_localPlayer;
		if (!m_keyHintsEnabled || localPlayer == null || localPlayer.IsDead() || Chat.instance.IsChatDialogWindowVisible())
		{
			m_buildHints.SetActive(value: false);
			m_combatHints.SetActive(value: false);
			return;
		}
		_ = m_buildHints.activeSelf;
		_ = m_buildHints.activeSelf;
		ItemDrop.ItemData currentWeapon = localPlayer.GetCurrentWeapon();
		if (localPlayer.InPlaceMode())
		{
			m_buildHints.SetActive(value: true);
			m_combatHints.SetActive(value: false);
		}
		else if (localPlayer.GetDoodadController() != null)
		{
			m_buildHints.SetActive(value: false);
			m_combatHints.SetActive(value: false);
		}
		else if (currentWeapon != null && (currentWeapon != localPlayer.m_unarmedWeapon.m_itemData || localPlayer.IsTargeted()))
		{
			m_buildHints.SetActive(value: false);
			m_combatHints.SetActive(value: true);
			bool flag = currentWeapon.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow;
			bool active = !flag && currentWeapon.HavePrimaryAttack();
			bool active2 = !flag && currentWeapon.HaveSecondaryAttack();
			m_bowDrawGP.SetActive(flag);
			m_bowDrawKB.SetActive(flag);
			m_primaryAttackGP.SetActive(active);
			m_primaryAttackKB.SetActive(active);
			m_secondaryAttackGP.SetActive(active2);
			m_secondaryAttackKB.SetActive(active2);
		}
		else
		{
			m_buildHints.SetActive(value: false);
			m_combatHints.SetActive(value: false);
		}
	}
}
