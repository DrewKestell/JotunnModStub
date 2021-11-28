using System;
using System.Collections.Generic;
using UnityEngine;

public class FootStep : MonoBehaviour
{
	public enum MotionType
	{
		Jog = 1,
		Run = 2,
		Sneak = 4,
		Climbing = 8,
		Swiming = 0x10,
		Land = 0x20,
		Walk = 0x40
	}

	public enum GroundMaterial
	{
		None = 0,
		Default = 1,
		Water = 2,
		Stone = 4,
		Wood = 8,
		Snow = 0x10,
		Mud = 0x20,
		Grass = 0x40,
		GenericGround = 0x80,
		Metal = 0x100,
		Tar = 0x200
	}

	[Serializable]
	public class StepEffect
	{
		public string m_name = "";

		[BitMask(typeof(MotionType))]
		public MotionType m_motionType = MotionType.Jog;

		[BitMask(typeof(GroundMaterial))]
		public GroundMaterial m_material = GroundMaterial.Default;

		public GameObject[] m_effectPrefabs = new GameObject[0];
	}

	private static Queue<GameObject> m_stepInstances = new Queue<GameObject>();

	private const int m_maxFootstepInstances = 30;

	public float m_footstepCullDistance = 20f;

	public List<StepEffect> m_effects = new List<StepEffect>();

	public Transform[] m_feet = new Transform[0];

	private static int m_footstepID = 0;

	private static int m_forwardSpeedID = 0;

	private static int m_sidewaySpeedID = 0;

	private float m_footstep;

	private float m_footstepTimer;

	private const float m_minFootstepInterval = 0.2f;

	private int m_pieceLayer;

	private Animator m_animator;

	private Character m_character;

	private ZNetView m_nview;

	private void Start()
	{
		m_animator = GetComponentInChildren<Animator>();
		m_character = GetComponent<Character>();
		m_nview = GetComponent<ZNetView>();
		if (m_footstepID == 0)
		{
			m_footstepID = Animator.StringToHash("footstep");
			m_forwardSpeedID = Animator.StringToHash("forward_speed");
			m_sidewaySpeedID = Animator.StringToHash("sideway_speed");
		}
		m_footstep = m_animator.GetFloat(m_footstepID);
		if (m_pieceLayer == 0)
		{
			m_pieceLayer = LayerMask.NameToLayer("piece");
		}
		Character character = m_character;
		character.m_onLand = (Action<Vector3>)Delegate.Combine(character.m_onLand, new Action<Vector3>(OnLand));
		if (m_nview.IsValid())
		{
			m_nview.Register<int, Vector3>("Step", RPC_Step);
		}
	}

	private void Update()
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			UpdateFootstep(Time.deltaTime);
		}
	}

	private void UpdateFootstep(float dt)
	{
		if (m_feet.Length != 0)
		{
			Camera mainCamera = Utils.GetMainCamera();
			if (!(mainCamera == null) && !(Vector3.Distance(base.transform.position, mainCamera.transform.position) > m_footstepCullDistance))
			{
				UpdateFootstepCurveTrigger(dt);
			}
		}
	}

	private void UpdateFootstepCurveTrigger(float dt)
	{
		m_footstepTimer += dt;
		float @float = m_animator.GetFloat(m_footstepID);
		if (Mathf.Sign(@float) != Mathf.Sign(m_footstep) && Mathf.Max(Mathf.Abs(m_animator.GetFloat(m_forwardSpeedID)), Mathf.Abs(m_animator.GetFloat(m_sidewaySpeedID))) > 0.2f && m_footstepTimer > 0.2f)
		{
			m_footstepTimer = 0f;
			OnFoot();
		}
		m_footstep = @float;
	}

	private Transform FindActiveFoot()
	{
		Transform transform = null;
		float num = 9999f;
		Vector3 forward = base.transform.forward;
		Transform[] feet = m_feet;
		foreach (Transform transform2 in feet)
		{
			Vector3 rhs = transform2.position - base.transform.position;
			float num2 = Vector3.Dot(forward, rhs);
			if (num2 > num || transform == null)
			{
				transform = transform2;
				num = num2;
			}
		}
		return transform;
	}

	private Transform FindFoot(string name)
	{
		Transform[] feet = m_feet;
		foreach (Transform transform in feet)
		{
			if (transform.gameObject.name == name)
			{
				return transform;
			}
		}
		return null;
	}

	public void OnFoot()
	{
		Transform transform = FindActiveFoot();
		if (!(transform == null))
		{
			OnFoot(transform);
		}
	}

	public void OnFoot(string name)
	{
		Transform transform = FindFoot(name);
		if (transform == null)
		{
			ZLog.LogWarning("FAiled to find foot:" + name);
		}
		else
		{
			OnFoot(transform);
		}
	}

	private void OnLand(Vector3 point)
	{
		if (m_nview.IsValid())
		{
			GroundMaterial groundMaterial = GetGroundMaterial(m_character, point);
			int num = FindBestStepEffect(groundMaterial, MotionType.Land);
			if (num != -1)
			{
				m_nview.InvokeRPC(ZNetView.Everybody, "Step", num, point);
			}
		}
	}

	private void OnFoot(Transform foot)
	{
		if (m_nview.IsValid())
		{
			Vector3 vector = ((foot != null) ? foot.position : base.transform.position);
			MotionType motionType = GetMotionType(m_character);
			GroundMaterial groundMaterial = GetGroundMaterial(m_character, vector);
			int num = FindBestStepEffect(groundMaterial, motionType);
			if (num != -1)
			{
				m_nview.InvokeRPC(ZNetView.Everybody, "Step", num, vector);
			}
		}
	}

	private static void PurgeOldEffects()
	{
		while (m_stepInstances.Count > 30)
		{
			GameObject gameObject = m_stepInstances.Dequeue();
			if ((bool)gameObject)
			{
				UnityEngine.Object.Destroy(gameObject);
			}
		}
	}

	private void DoEffect(StepEffect effect, Vector3 point)
	{
		GameObject[] effectPrefabs = effect.m_effectPrefabs;
		foreach (GameObject gameObject in effectPrefabs)
		{
			GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, point, base.transform.rotation);
			m_stepInstances.Enqueue(gameObject2);
			if (gameObject2.GetComponent<ZNetView>() != null)
			{
				ZLog.LogWarning("Foot step effect " + effect.m_name + " prefab " + gameObject.name + " in " + m_character.gameObject.name + " should not contain a ZNetView component");
			}
		}
		PurgeOldEffects();
	}

	private void RPC_Step(long sender, int effectIndex, Vector3 point)
	{
		StepEffect effect = m_effects[effectIndex];
		DoEffect(effect, point);
	}

	private MotionType GetMotionType(Character character)
	{
		if (m_character.IsSwiming())
		{
			return MotionType.Swiming;
		}
		if (m_character.IsWallRunning())
		{
			return MotionType.Climbing;
		}
		if (m_character.IsRunning())
		{
			return MotionType.Run;
		}
		if (m_character.IsSneaking())
		{
			return MotionType.Sneak;
		}
		if (m_character.IsWalking())
		{
			return MotionType.Walk;
		}
		return MotionType.Jog;
	}

	private GroundMaterial GetGroundMaterial(Character character, Vector3 point)
	{
		if (character.InWater())
		{
			return GroundMaterial.Water;
		}
		if (character.InLiquid())
		{
			return GroundMaterial.Tar;
		}
		if (!character.IsOnGround())
		{
			return GroundMaterial.None;
		}
		float num = Mathf.Acos(Mathf.Clamp01(character.GetLastGroundNormal().y)) * 57.29578f;
		Collider lastGroundCollider = character.GetLastGroundCollider();
		if ((bool)lastGroundCollider)
		{
			Heightmap component = lastGroundCollider.GetComponent<Heightmap>();
			if (component != null)
			{
				switch (component.GetBiome(point))
				{
				case Heightmap.Biome.Mountain:
				case Heightmap.Biome.DeepNorth:
					if (num < 40f && !component.IsCleared(point))
					{
						return GroundMaterial.Snow;
					}
					break;
				case Heightmap.Biome.Swamp:
					if (num < 40f)
					{
						return GroundMaterial.Mud;
					}
					break;
				case Heightmap.Biome.Meadows:
				case Heightmap.Biome.BlackForest:
					if (num < 25f)
					{
						return GroundMaterial.Grass;
					}
					break;
				}
				return GroundMaterial.GenericGround;
			}
			if (lastGroundCollider.gameObject.layer == m_pieceLayer)
			{
				WearNTear componentInParent = lastGroundCollider.GetComponentInParent<WearNTear>();
				if ((bool)componentInParent)
				{
					switch (componentInParent.m_materialType)
					{
					case WearNTear.MaterialType.Wood:
						return GroundMaterial.Wood;
					case WearNTear.MaterialType.Stone:
						return GroundMaterial.Stone;
					case WearNTear.MaterialType.HardWood:
						return GroundMaterial.Wood;
					case WearNTear.MaterialType.Iron:
						return GroundMaterial.Metal;
					}
				}
			}
		}
		return GroundMaterial.Default;
	}

	public void FindJoints()
	{
		ZLog.Log("Finding joints");
		Transform transform = Utils.FindChild(base.transform, "LeftFootFront");
		Transform transform2 = Utils.FindChild(base.transform, "RightFootFront");
		Transform transform3 = Utils.FindChild(base.transform, "LeftFoot");
		if (transform3 == null)
		{
			transform3 = Utils.FindChild(base.transform, "LeftFootBack");
		}
		if (transform3 == null)
		{
			transform3 = Utils.FindChild(base.transform, "l_foot");
		}
		if (transform3 == null)
		{
			transform3 = Utils.FindChild(base.transform, "Foot.l");
		}
		if (transform3 == null)
		{
			transform3 = Utils.FindChild(base.transform, "foot.l");
		}
		Transform transform4 = Utils.FindChild(base.transform, "RightFoot");
		if (transform4 == null)
		{
			transform4 = Utils.FindChild(base.transform, "RightFootBack");
		}
		if (transform4 == null)
		{
			transform4 = Utils.FindChild(base.transform, "r_foot");
		}
		if (transform4 == null)
		{
			transform4 = Utils.FindChild(base.transform, "Foot.r");
		}
		if (transform4 == null)
		{
			transform4 = Utils.FindChild(base.transform, "foot.r");
		}
		List<Transform> list = new List<Transform>();
		if ((bool)transform)
		{
			list.Add(transform);
		}
		if ((bool)transform2)
		{
			list.Add(transform2);
		}
		if ((bool)transform3)
		{
			list.Add(transform3);
		}
		if ((bool)transform4)
		{
			list.Add(transform4);
		}
		m_feet = list.ToArray();
	}

	private int FindBestStepEffect(GroundMaterial material, MotionType motion)
	{
		StepEffect stepEffect = null;
		int result = -1;
		for (int i = 0; i < m_effects.Count; i++)
		{
			StepEffect stepEffect2 = m_effects[i];
			if (((stepEffect2.m_material & material) != 0 || (stepEffect == null && (stepEffect2.m_material & GroundMaterial.Default) != 0)) && (stepEffect2.m_motionType & motion) != 0)
			{
				stepEffect = stepEffect2;
				result = i;
			}
		}
		return result;
	}
}
