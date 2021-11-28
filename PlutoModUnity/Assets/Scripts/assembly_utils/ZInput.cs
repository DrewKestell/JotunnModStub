using System;
using System.Collections.Generic;
using UnityEngine;

public class ZInput
{
	public class ButtonDef
	{
		public string m_name;

		public bool m_gamepad;

		public bool m_save = true;

		public KeyCode m_key;

		public string m_axis;

		public bool m_inverted;

		public bool m_pressed;

		public bool m_wasPressed;

		public bool m_down;

		public bool m_up;

		public bool m_pressedFixed;

		public bool m_wasPressedFixed;

		public bool m_downFixed;

		public bool m_upFixed;

		public float m_pressedTimer;

		public float m_repeatDelay;

		public float m_repeatInterval;
	}

	private static ZInput m_instance;

	private const float m_stickDeadZone = 0.2f;

	private const float m_gamepadInactiveTimeout = 60f;

	private bool m_gamepadActive;

	private bool m_mouseActive = true;

	private bool m_gamepadEnabled;

	private Dictionary<string, ButtonDef> m_buttons = new Dictionary<string, ButtonDef>();

	private static ButtonDef m_binding;

	public static ZInput instance => m_instance;

	public static void Initialize()
	{
		if (m_instance == null)
		{
			m_instance = new ZInput();
		}
	}

	private ZInput()
	{
		Reset();
		Load();
	}

	public static void Update(float dt)
	{
		if (m_instance != null)
		{
			m_instance.InternalUpdate(dt);
		}
	}

	public static void FixedUpdate(float dt)
	{
		if (m_instance != null)
		{
			m_instance.InternalUpdateFixed(dt);
		}
	}

	private void InternalUpdate(float dt)
	{
		CheckMouseInput();
		foreach (ButtonDef value in m_buttons.Values)
		{
			if (!m_gamepadEnabled && value.m_gamepad)
			{
				continue;
			}
			value.m_wasPressed = value.m_pressed;
			if (!string.IsNullOrEmpty(value.m_axis))
			{
				float num = (value.m_inverted ? (0f - Input.GetAxis(value.m_axis)) : Input.GetAxis(value.m_axis));
				value.m_pressed = num > 0.4f;
			}
			else
			{
				value.m_pressed = Input.GetKey(value.m_key);
			}
			value.m_down = value.m_pressed && !value.m_wasPressed;
			value.m_up = !value.m_pressed && value.m_wasPressed;
			if (value.m_repeatDelay > 0f)
			{
				if (value.m_pressed)
				{
					value.m_pressedTimer += dt;
					if (value.m_pressedTimer > value.m_repeatDelay)
					{
						value.m_down = true;
						value.m_downFixed = true;
						value.m_pressedTimer -= value.m_repeatInterval;
					}
				}
				else
				{
					value.m_pressedTimer = 0f;
				}
			}
			if (value.m_gamepad && value.m_down)
			{
				OnGamepadInput();
			}
		}
	}

	private void CheckMouseInput()
	{
		if (Input.GetAxis("Mouse X") != 0f || Input.GetAxis("Mouse Y") != 0f)
		{
			OnMouseInput();
		}
	}

	private void OnMouseInput()
	{
		m_gamepadActive = false;
		m_mouseActive = true;
	}

	private void OnGamepadInput()
	{
		m_gamepadActive = true;
		m_mouseActive = false;
	}

	private void InternalUpdateFixed(float dt)
	{
		foreach (ButtonDef value in m_buttons.Values)
		{
			value.m_wasPressedFixed = value.m_pressedFixed;
			value.m_pressedFixed = value.m_pressed;
			value.m_downFixed = value.m_pressedFixed && !value.m_wasPressedFixed;
			value.m_upFixed = !value.m_pressedFixed && value.m_wasPressedFixed;
		}
	}

	public void Reset()
	{
		m_buttons.Clear();
		float repeatDelay = 0.3f;
		float repeatInterval = 0.1f;
		AddButton("Attack", KeyCode.Mouse0);
		AddButton("SecondAttack", KeyCode.Mouse2);
		AddButton("Block", KeyCode.Mouse1);
		AddButton("Use", KeyCode.E);
		AddButton("Hide", KeyCode.R);
		AddButton("Jump", KeyCode.Space);
		AddButton("Crouch", KeyCode.LeftControl);
		AddButton("Run", KeyCode.LeftShift);
		AddButton("ToggleWalk", KeyCode.C);
		AddButton("AutoRun", KeyCode.Q);
		AddButton("Sit", KeyCode.X);
		AddButton("GPower", KeyCode.F);
		AddButton("AltPlace", KeyCode.LeftShift);
		AddButton("Forward", KeyCode.W, repeatDelay, repeatInterval);
		AddButton("Left", KeyCode.A, repeatDelay, repeatInterval);
		AddButton("Backward", KeyCode.S, repeatDelay, repeatInterval);
		AddButton("Right", KeyCode.D, repeatDelay, repeatInterval);
		AddButton("Inventory", KeyCode.Tab);
		AddButton("Map", KeyCode.M);
		AddButton("MapZoomOut", KeyCode.Comma);
		AddButton("MapZoomIn", KeyCode.Period);
		AddButton("BuildPrev", KeyCode.Q);
		AddButton("BuildNext", KeyCode.E);
		AddButton("BuildMenu", KeyCode.Mouse1);
		AddButton("Remove", KeyCode.Mouse2);
		AddButton("AutoPickup", KeyCode.V);
		AddButton("ScrollChatUp", KeyCode.PageUp, 0.5f, 0.05f);
		AddButton("ScrollChatDown", KeyCode.PageDown, 0.5f, 0.05f);
		AddButton("ChatUp", KeyCode.UpArrow, 0.5f, 0.05f);
		AddButton("ChatDown", KeyCode.DownArrow, 0.5f, 0.05f);
		AddButton("JoyUse", KeyCode.JoystickButton0);
		AddButton("JoyHide", KeyCode.JoystickButton9);
		AddButton("JoyJump", KeyCode.JoystickButton1);
		AddButton("JoySit", KeyCode.JoystickButton2);
		AddButton("JoyGPower", "JoyAxis 7", inverted: true);
		AddButton("JoyInventory", KeyCode.JoystickButton3);
		AddButton("JoyRun", KeyCode.JoystickButton4);
		AddButton("JoyCrouch", KeyCode.JoystickButton8);
		AddButton("JoyMap", KeyCode.JoystickButton6);
		AddButton("JoyMenu", KeyCode.JoystickButton7);
		AddButton("JoyBlock", "JoyAxis 3", inverted: true);
		AddButton("JoyAttack", "JoyAxis 3");
		AddButton("JoySecondAttack", KeyCode.JoystickButton5);
		AddButton("JoyAltPlace", KeyCode.JoystickButton4);
		AddButton("JoyRotate", "JoyAxis 3", inverted: true);
		AddButton("JoyPlace", "JoyAxis 10");
		AddButton("JoyRemove", KeyCode.JoystickButton5);
		AddButton("JoyTabLeft", KeyCode.JoystickButton4);
		AddButton("JoyTabRight", KeyCode.JoystickButton5);
		AddButton("JoyLStickLeft", "JoyAxis 1", inverted: true, repeatDelay, repeatInterval);
		AddButton("JoyLStickRight", "JoyAxis 1", inverted: false, repeatDelay, repeatInterval);
		AddButton("JoyLStickUp", "JoyAxis 2", inverted: true, repeatDelay, repeatInterval);
		AddButton("JoyLStickDown", "JoyAxis 2", inverted: false, repeatDelay, repeatInterval);
		AddButton("JoyButtonA", KeyCode.JoystickButton0);
		AddButton("JoyButtonB", KeyCode.JoystickButton1);
		AddButton("JoyButtonX", KeyCode.JoystickButton2);
		AddButton("JoyButtonY", KeyCode.JoystickButton3);
		AddButton("JoyBack", KeyCode.JoystickButton6);
		AddButton("JoyDPadLeft", "JoyAxis 6", inverted: true, repeatDelay, repeatInterval);
		AddButton("JoyDPadRight", "JoyAxis 6", inverted: false, repeatDelay, repeatInterval);
		AddButton("JoyDPadUp", "JoyAxis 7", inverted: false, repeatDelay, repeatInterval);
		AddButton("JoyDPadDown", "JoyAxis 7", inverted: true, repeatDelay, repeatInterval);
		AddButton("JoyLTrigger", "JoyAxis 3", inverted: true);
		AddButton("JoyRTrigger", "JoyAxis 3");
		AddButton("JoyLStick", KeyCode.JoystickButton8);
		AddButton("JoyRStick", KeyCode.JoystickButton9);
	}

	public static bool IsGamepadEnabled()
	{
		return m_instance.m_gamepadEnabled;
	}

	public static void SetGamepadEnabled(bool enabled)
	{
		m_instance.m_gamepadEnabled = enabled;
	}

	public static bool IsGamepadActive()
	{
		if (!m_instance.m_gamepadEnabled)
		{
			return false;
		}
		return m_instance.m_gamepadActive;
	}

	public static bool IsMouseActive()
	{
		return m_instance.m_mouseActive;
	}

	public void Save()
	{
		PlayerPrefs.SetInt("gamepad_enabled", m_gamepadEnabled ? 1 : 0);
		foreach (ButtonDef value in m_buttons.Values)
		{
			if (value.m_save)
			{
				PlayerPrefs.SetInt("key_" + value.m_name, (int)value.m_key);
			}
		}
	}

	public void Load()
	{
		Reset();
		m_gamepadEnabled = PlayerPrefs.GetInt("gamepad_enabled", 1) == 1;
		foreach (ButtonDef value in m_buttons.Values)
		{
			if (value.m_save)
			{
				KeyCode keyCode = (value.m_key = (KeyCode)PlayerPrefs.GetInt("key_" + value.m_name, (int)value.m_key));
			}
		}
	}

	public ButtonDef AddButton(string name, KeyCode key, float repeatDelay = 0f, float repeatInterval = 0f)
	{
		ButtonDef buttonDef = new ButtonDef();
		buttonDef.m_name = name;
		buttonDef.m_key = key;
		buttonDef.m_gamepad = name.StartsWith("Joy");
		buttonDef.m_save = !buttonDef.m_gamepad;
		buttonDef.m_repeatDelay = repeatDelay;
		buttonDef.m_repeatInterval = repeatInterval;
		m_buttons.Add(name, buttonDef);
		return buttonDef;
	}

	public ButtonDef AddButton(string name, string axis, bool inverted = false, float repeatDelay = 0f, float repeatInterval = 0f)
	{
		ButtonDef buttonDef = new ButtonDef();
		buttonDef.m_name = name;
		buttonDef.m_axis = axis;
		buttonDef.m_inverted = inverted;
		buttonDef.m_gamepad = name.StartsWith("Joy");
		buttonDef.m_save = !buttonDef.m_gamepad;
		buttonDef.m_repeatDelay = repeatDelay;
		buttonDef.m_repeatInterval = repeatInterval;
		m_buttons.Add(name, buttonDef);
		return buttonDef;
	}

	public void Setbutton(string name, KeyCode key)
	{
		m_buttons[name].m_key = key;
	}

	public string GetBoundKeyString(string name)
	{
		if (m_buttons.TryGetValue(name, out var value))
		{
			switch (value.m_key)
			{
			case KeyCode.Comma:
				return ",";
			case KeyCode.Period:
				return ".";
			case KeyCode.Mouse0:
				return "$button_mouse0";
			case KeyCode.Mouse1:
				return "$button_mouse1";
			case KeyCode.Mouse2:
				return "$button_mouse2";
			case KeyCode.Space:
				return "$button_space";
			case KeyCode.LeftShift:
				return "$button_lshift";
			case KeyCode.RightShift:
				return "$button_rshift";
			case KeyCode.LeftAlt:
				return "$button_lalt";
			case KeyCode.RightAlt:
				return "$button_ralt";
			case KeyCode.LeftControl:
				return "$button_lctrl";
			case KeyCode.RightControl:
				return "$button_rctrl";
			case KeyCode.Return:
				return "$button_return";
			default:
				return value.m_key.ToString();
			}
		}
		return "MISSING KEY BINDING \"" + name + "\"";
	}

	public static void ResetButtonStatus(string name)
	{
		if (m_instance.m_buttons.TryGetValue(name, out var value))
		{
			value.m_down = false;
			value.m_up = false;
			value.m_downFixed = false;
			value.m_upFixed = false;
		}
	}

	public static bool GetButtonDown(string name)
	{
		if (m_instance.m_buttons.TryGetValue(name, out var value))
		{
			if (!instance.m_gamepadEnabled && value.m_gamepad)
			{
				return false;
			}
			if (Time.inFixedTimeStep)
			{
				return value.m_downFixed;
			}
			return value.m_down;
		}
		return false;
	}

	public static bool GetButtonUp(string name)
	{
		if (m_instance.m_buttons.TryGetValue(name, out var value))
		{
			if (!instance.m_gamepadEnabled && value.m_gamepad)
			{
				return false;
			}
			if (Time.inFixedTimeStep)
			{
				return value.m_upFixed;
			}
			return value.m_up;
		}
		return false;
	}

	public static bool GetButton(string name)
	{
		if (m_instance.m_buttons.TryGetValue(name, out var value))
		{
			if (!instance.m_gamepadEnabled && value.m_gamepad)
			{
				return false;
			}
			return value.m_pressed;
		}
		return false;
	}

	private KeyCode GetPressedKey()
	{
		foreach (KeyCode value in Enum.GetValues(typeof(KeyCode)))
		{
			if (Input.GetKey(value))
			{
				return value;
			}
		}
		return KeyCode.None;
	}

	public void StartBindKey(string name)
	{
		if (m_buttons.TryGetValue(name, out var value))
		{
			m_binding = value;
		}
	}

	public bool EndBindKey()
	{
		if (m_binding == null)
		{
			return true;
		}
		KeyCode pressedKey = GetPressedKey();
		if (pressedKey != 0)
		{
			m_binding.m_key = pressedKey;
			return true;
		}
		return false;
	}

	private static float ApplyDeadzone(float v, bool soften)
	{
		float num = Mathf.Sign(v);
		v = Mathf.Abs(v);
		v = Mathf.Clamp01(v - 0.2f);
		v *= 1.25f;
		if (soften)
		{
			v *= v;
		}
		v *= num;
		return v;
	}

	public static float GetJoyLeftStickX(bool smooth = false)
	{
		if (!m_instance.m_gamepadEnabled)
		{
			return 0f;
		}
		float num = ApplyDeadzone(Input.GetAxis("JoyAxis 1"), smooth);
		if (num != 0f)
		{
			m_instance.OnGamepadInput();
		}
		return num;
	}

	public static float GetJoyLeftStickY(bool smooth = true)
	{
		if (!m_instance.m_gamepadEnabled)
		{
			return 0f;
		}
		float num = ApplyDeadzone(Input.GetAxis("JoyAxis 2"), smooth);
		if (num != 0f)
		{
			m_instance.OnGamepadInput();
		}
		return num;
	}

	public static float GetJoyRightStickX()
	{
		if (!m_instance.m_gamepadEnabled)
		{
			return 0f;
		}
		float num = ApplyDeadzone(Input.GetAxis("JoyAxis 4"), soften: true);
		if (num != 0f)
		{
			m_instance.OnGamepadInput();
		}
		return num;
	}

	public static float GetJoyRightStickY()
	{
		if (!m_instance.m_gamepadEnabled)
		{
			return 0f;
		}
		float num = ApplyDeadzone(Input.GetAxis("JoyAxis 5"), soften: true);
		if (num != 0f)
		{
			m_instance.OnGamepadInput();
		}
		return num;
	}

	public static float GetJoyLTrigger()
	{
		if (!m_instance.m_gamepadEnabled)
		{
			return 0f;
		}
		return Input.GetAxis("JoyAxis 3");
	}

	public static float GetJoyRTrigger()
	{
		if (!m_instance.m_gamepadEnabled)
		{
			return 0f;
		}
		return Input.GetAxis("JoyAxis 6");
	}
}
