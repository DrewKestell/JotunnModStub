using UnityEngine;

public class PlayerController : MonoBehaviour
{
	private Player m_character;

	private ZNetView m_nview;

	public static float m_mouseSens = 1f;

	public static float m_gamepadSens = 1f;

	public static bool m_invertMouse = false;

	public float m_minDodgeTime = 0.2f;

	private bool m_attackWasPressed;

	private bool m_secondAttackWasPressed;

	private bool m_blockWasPressed;

	private bool m_lastJump;

	private bool m_lastCrouch;

	private void Awake()
	{
		m_character = GetComponent<Player>();
		m_nview = GetComponent<ZNetView>();
		if (m_nview.GetZDO() == null)
		{
			base.enabled = false;
			return;
		}
		m_mouseSens = PlayerPrefs.GetFloat("MouseSensitivity", m_mouseSens);
		m_gamepadSens = PlayerPrefs.GetFloat("GamepadSensitivity", m_gamepadSens);
		m_invertMouse = PlayerPrefs.GetInt("InvertMouse", 0) == 1;
	}

	private void FixedUpdate()
	{
		if ((bool)m_nview && !m_nview.IsOwner())
		{
			return;
		}
		if (!TakeInput())
		{
			m_character.SetControls(Vector3.zero, attack: false, attackHold: false, secondaryAttack: false, secondaryAttackHold: false, block: false, blockHold: false, jump: false, crouch: false, run: false, autoRun: false);
			return;
		}
		bool flag = InInventoryEtc();
		Vector3 zero = Vector3.zero;
		if (ZInput.GetButton("Forward"))
		{
			zero.z += 1f;
		}
		if (ZInput.GetButton("Backward"))
		{
			zero.z -= 1f;
		}
		if (ZInput.GetButton("Left"))
		{
			zero.x -= 1f;
		}
		if (ZInput.GetButton("Right"))
		{
			zero.x += 1f;
		}
		zero.x += ZInput.GetJoyLeftStickX();
		zero.z += 0f - ZInput.GetJoyLeftStickY();
		if (zero.magnitude > 1f)
		{
			zero.Normalize();
		}
		bool flag2 = (ZInput.GetButton("Attack") || ZInput.GetButton("JoyAttack")) && !flag;
		bool attackHold = flag2;
		bool attack = flag2 && !m_attackWasPressed;
		m_attackWasPressed = flag2;
		bool flag3 = (ZInput.GetButton("SecondAttack") || ZInput.GetButton("JoySecondAttack")) && !flag;
		bool secondaryAttackHold = flag3;
		bool secondaryAttack = flag3 && !m_secondAttackWasPressed;
		m_secondAttackWasPressed = flag3;
		bool flag4 = (ZInput.GetButton("Block") || ZInput.GetButton("JoyBlock")) && !flag;
		bool blockHold = flag4;
		bool block = flag4 && !m_blockWasPressed;
		m_blockWasPressed = flag4;
		bool button = ZInput.GetButton("Jump");
		bool jump = (button && !m_lastJump) || ZInput.GetButtonDown("JoyJump");
		m_lastJump = button;
		bool flag5 = InventoryGui.IsVisible();
		bool flag6 = (ZInput.GetButton("Crouch") || ZInput.GetButton("JoyCrouch")) && !flag5;
		bool crouch = flag6 && !m_lastCrouch;
		m_lastCrouch = flag6;
		bool run = ZInput.GetButton("Run") || ZInput.GetButton("JoyRun");
		bool button2 = ZInput.GetButton("AutoRun");
		m_character.SetControls(zero, attack, attackHold, secondaryAttack, secondaryAttackHold, block, blockHold, jump, crouch, run, button2);
	}

	private static bool DetectTap(bool pressed, float dt, float minPressTime, bool run, ref float pressTimer, ref float releasedTimer, ref bool tapPressed)
	{
		bool result = false;
		if (pressed)
		{
			if ((releasedTimer > 0f && releasedTimer < minPressTime) & tapPressed)
			{
				tapPressed = false;
				result = true;
			}
			pressTimer += dt;
			releasedTimer = 0f;
		}
		else
		{
			if (pressTimer > 0f)
			{
				tapPressed = pressTimer < minPressTime;
				if (run & tapPressed)
				{
					tapPressed = false;
					result = true;
				}
			}
			releasedTimer += dt;
			pressTimer = 0f;
		}
		return result;
	}

	private bool TakeInput()
	{
		if (GameCamera.InFreeFly())
		{
			return false;
		}
		if ((!Chat.instance || !Chat.instance.HasFocus()) && !Menu.IsVisible() && !Console.IsVisible() && !TextInput.IsVisible() && !Minimap.InTextInput() && (!ZInput.IsGamepadActive() || !Minimap.IsOpen()) && (!ZInput.IsGamepadActive() || !InventoryGui.IsVisible()) && (!ZInput.IsGamepadActive() || !StoreGui.IsVisible()))
		{
			if (ZInput.IsGamepadActive())
			{
				return !Hud.IsPieceSelectionVisible();
			}
			return true;
		}
		return false;
	}

	private bool InInventoryEtc()
	{
		if (!InventoryGui.IsVisible() && !Minimap.IsOpen() && !StoreGui.IsVisible())
		{
			return Hud.IsPieceSelectionVisible();
		}
		return true;
	}

	private void LateUpdate()
	{
		if (!TakeInput() || InInventoryEtc())
		{
			m_character.SetMouseLook(Vector2.zero);
			return;
		}
		Vector2 zero = Vector2.zero;
		zero.x = Input.GetAxis("Mouse X") * m_mouseSens;
		zero.y = Input.GetAxis("Mouse Y") * m_mouseSens;
		if (!m_character.InPlaceMode() || !ZInput.GetButton("JoyRotate"))
		{
			zero.x += ZInput.GetJoyRightStickX() * 110f * Time.deltaTime * m_gamepadSens;
			zero.y += (0f - ZInput.GetJoyRightStickY()) * 110f * Time.deltaTime * m_gamepadSens;
		}
		if (m_invertMouse)
		{
			zero.y *= -1f;
		}
		m_character.SetMouseLook(zero);
	}
}
