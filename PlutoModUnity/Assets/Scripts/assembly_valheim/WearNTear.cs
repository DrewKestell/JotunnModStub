using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WearNTear : MonoBehaviour, IDestructible
{
	public enum MaterialType
	{
		Wood,
		Stone,
		Iron,
		HardWood
	}

	private struct BoundData
	{
		public Vector3 m_pos;

		public Quaternion m_rot;

		public Vector3 m_size;
	}

	private struct OldMeshData
	{
		public Renderer m_renderer;

		public Material[] m_materials;

		public Color[] m_color;

		public Color[] m_emissiveColor;
	}

	public static bool m_randomInitialDamage = false;

	public Action m_onDestroyed;

	public Action m_onDamaged;

	[Header("Wear")]
	public GameObject m_new;

	public GameObject m_worn;

	public GameObject m_broken;

	public GameObject m_wet;

	public bool m_noRoofWear = true;

	public bool m_noSupportWear = true;

	public MaterialType m_materialType;

	public bool m_supports = true;

	public Vector3 m_comOffset = Vector3.zero;

	[Header("Destruction")]
	public float m_health = 100f;

	public HitData.DamageModifiers m_damages;

	public float m_hitNoise;

	public float m_destroyNoise;

	[Header("Effects")]
	public EffectList m_destroyedEffect = new EffectList();

	public EffectList m_hitEffect = new EffectList();

	public EffectList m_switchEffect = new EffectList();

	public bool m_autoCreateFragments = true;

	public GameObject[] m_fragmentRoots;

	private const float m_noFireDrain = 0.00496031763f;

	private const float m_noSupportDrain = 25f;

	private const float m_rainDamageTime = 60f;

	private const float m_rainDamage = 5f;

	private const float m_comTestWidth = 0.2f;

	private const float m_comMinAngle = 100f;

	private const float m_minFireDistance = 20f;

	private const int m_wearUpdateIntervalMinutes = 60;

	private const float m_privateAreaModifier = 0.5f;

	private static RaycastHit[] m_raycastHits = new RaycastHit[128];

	private static Collider[] m_tempColliders = new Collider[128];

	private static int m_rayMask = 0;

	private static List<WearNTear> m_allInstances = new List<WearNTear>();

	private static List<Vector3> m_tempSupportPoints = new List<Vector3>();

	private static List<float> m_tempSupportPointValues = new List<float>();

	private ZNetView m_nview;

	private Collider[] m_colliders;

	private float m_support = 1f;

	private float m_createTime;

	private int m_myIndex = -1;

	private float m_rainTimer;

	private float m_lastRepair;

	private Piece m_piece;

	private List<BoundData> m_bounds;

	private List<OldMeshData> m_oldMaterials;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_piece = GetComponent<Piece>();
		if (m_nview.GetZDO() != null)
		{
			m_nview.Register<HitData>("WNTDamage", RPC_Damage);
			m_nview.Register("WNTRemove", RPC_Remove);
			m_nview.Register("WNTRepair", RPC_Repair);
			m_nview.Register<float>("WNTHealthChanged", RPC_HealthChanged);
			if (m_autoCreateFragments)
			{
				m_nview.Register("WNTCreateFragments", RPC_CreateFragments);
			}
			if (m_rayMask == 0)
			{
				m_rayMask = LayerMask.GetMask("piece", "Default", "static_solid", "Default_small", "terrain");
			}
			m_allInstances.Add(this);
			m_myIndex = m_allInstances.Count - 1;
			m_createTime = Time.time;
			m_support = GetMaxSupport();
			if (m_randomInitialDamage)
			{
				float value = UnityEngine.Random.Range(0.1f * m_health, m_health * 0.6f);
				m_nview.GetZDO().Set("health", value);
			}
			UpdateVisual(triggerEffects: false);
		}
	}

	private void OnDestroy()
	{
		if (m_myIndex != -1)
		{
			m_allInstances[m_myIndex] = m_allInstances[m_allInstances.Count - 1];
			m_allInstances[m_myIndex].m_myIndex = m_myIndex;
			m_allInstances.RemoveAt(m_allInstances.Count - 1);
		}
	}

	public bool Repair()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		if (m_nview.GetZDO().GetFloat("health", m_health) >= m_health)
		{
			return false;
		}
		if (Time.time - m_lastRepair < 1f)
		{
			return false;
		}
		m_lastRepair = Time.time;
		m_nview.InvokeRPC("WNTRepair");
		return true;
	}

	private void RPC_Repair(long sender)
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			m_nview.GetZDO().Set("health", m_health);
			m_nview.InvokeRPC(ZNetView.Everybody, "WNTHealthChanged", m_health);
		}
	}

	private float GetSupport()
	{
		if (!m_nview.IsValid())
		{
			return GetMaxSupport();
		}
		if (!m_nview.HasOwner())
		{
			return GetMaxSupport();
		}
		if (m_nview.IsOwner())
		{
			return m_support;
		}
		return m_nview.GetZDO().GetFloat("support", GetMaxSupport());
	}

	private float GetSupportColorValue()
	{
		float support = GetSupport();
		GetMaterialProperties(out var maxSupport, out var minSupport, out var _, out var _);
		if (support >= maxSupport)
		{
			return -1f;
		}
		support -= minSupport;
		return Mathf.Clamp01(support / (maxSupport * 0.5f - minSupport));
	}

	public void OnPlaced()
	{
		m_createTime = -1f;
	}

	private List<Renderer> GetHighlightRenderers()
	{
		MeshRenderer[] componentsInChildren = GetComponentsInChildren<MeshRenderer>(includeInactive: true);
		SkinnedMeshRenderer[] componentsInChildren2 = GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
		List<Renderer> list = new List<Renderer>();
		list.AddRange(componentsInChildren);
		list.AddRange(componentsInChildren2);
		return list;
	}

	public void Highlight()
	{
		if (m_oldMaterials == null)
		{
			m_oldMaterials = new List<OldMeshData>();
			foreach (Renderer highlightRenderer in GetHighlightRenderers())
			{
				OldMeshData item = default(OldMeshData);
				item.m_materials = highlightRenderer.sharedMaterials;
				item.m_color = new Color[item.m_materials.Length];
				item.m_emissiveColor = new Color[item.m_materials.Length];
				for (int i = 0; i < item.m_materials.Length; i++)
				{
					if (item.m_materials[i].HasProperty("_Color"))
					{
						item.m_color[i] = item.m_materials[i].GetColor("_Color");
					}
					if (item.m_materials[i].HasProperty("_EmissionColor"))
					{
						item.m_emissiveColor[i] = item.m_materials[i].GetColor("_EmissionColor");
					}
				}
				item.m_renderer = highlightRenderer;
				m_oldMaterials.Add(item);
			}
		}
		float supportColorValue = GetSupportColorValue();
		Color color = new Color(0.6f, 0.8f, 1f);
		if (supportColorValue >= 0f)
		{
			color = Color.Lerp(new Color(1f, 0f, 0f), new Color(0f, 1f, 0f), supportColorValue);
			Color.RGBToHSV(color, out var H, out var S, out var V);
			S = Mathf.Lerp(1f, 0.5f, supportColorValue);
			V = Mathf.Lerp(1.2f, 0.9f, supportColorValue);
			color = Color.HSVToRGB(H, S, V);
		}
		foreach (OldMeshData oldMaterial in m_oldMaterials)
		{
			if ((bool)oldMaterial.m_renderer)
			{
				Material[] materials = oldMaterial.m_renderer.materials;
				foreach (Material obj in materials)
				{
					obj.SetColor("_EmissionColor", color * 0.4f);
					obj.color = color;
				}
			}
		}
		CancelInvoke("ResetHighlight");
		Invoke("ResetHighlight", 0.2f);
	}

	private void ResetHighlight()
	{
		if (m_oldMaterials == null)
		{
			return;
		}
		foreach (OldMeshData oldMaterial in m_oldMaterials)
		{
			if (!oldMaterial.m_renderer)
			{
				continue;
			}
			Material[] materials = oldMaterial.m_renderer.materials;
			if (materials.Length == 0)
			{
				continue;
			}
			if (materials[0] == oldMaterial.m_materials[0])
			{
				if (materials.Length != oldMaterial.m_color.Length)
				{
					continue;
				}
				for (int i = 0; i < materials.Length; i++)
				{
					if (materials[i].HasProperty("_Color"))
					{
						materials[i].SetColor("_Color", oldMaterial.m_color[i]);
					}
					if (materials[i].HasProperty("_EmissionColor"))
					{
						materials[i].SetColor("_EmissionColor", oldMaterial.m_emissiveColor[i]);
					}
				}
			}
			else if (materials.Length == oldMaterial.m_materials.Length)
			{
				oldMaterial.m_renderer.materials = oldMaterial.m_materials;
			}
		}
		m_oldMaterials = null;
	}

	private void SetupColliders()
	{
		m_colliders = GetComponentsInChildren<Collider>(includeInactive: true);
		m_bounds = new List<BoundData>();
		Collider[] colliders = m_colliders;
		foreach (Collider collider in colliders)
		{
			if (!collider.isTrigger && !(collider.attachedRigidbody != null))
			{
				BoundData item = default(BoundData);
				if (collider is BoxCollider)
				{
					BoxCollider boxCollider = collider as BoxCollider;
					item.m_rot = boxCollider.transform.rotation;
					item.m_pos = boxCollider.transform.position + boxCollider.transform.TransformVector(boxCollider.center);
					item.m_size = new Vector3(boxCollider.transform.lossyScale.x * boxCollider.size.x, boxCollider.transform.lossyScale.y * boxCollider.size.y, boxCollider.transform.lossyScale.z * boxCollider.size.z);
				}
				else
				{
					item.m_rot = Quaternion.identity;
					item.m_pos = collider.bounds.center;
					item.m_size = collider.bounds.size;
				}
				item.m_size.x += 0.3f;
				item.m_size.y += 0.3f;
				item.m_size.z += 0.3f;
				item.m_size *= 0.5f;
				m_bounds.Add(item);
			}
		}
	}

	private bool ShouldUpdate()
	{
		if (!(m_createTime < 0f))
		{
			return Time.time - m_createTime > 30f;
		}
		return true;
	}

	public void UpdateWear()
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		if (m_nview.IsOwner() && ShouldUpdate())
		{
			if (ZNetScene.instance.OutsideActiveArea(base.transform.position))
			{
				m_support = GetMaxSupport();
				m_nview.GetZDO().Set("support", m_support);
				return;
			}
			float num = 0f;
			bool flag = HaveRoof();
			bool flag2 = EnvMan.instance.IsWet() && !flag;
			if ((bool)m_wet)
			{
				m_wet.SetActive(flag2);
			}
			if (m_noRoofWear && GetHealthPercentage() > 0.5f)
			{
				if (flag2 || IsUnderWater())
				{
					if (m_rainTimer == 0f)
					{
						m_rainTimer = Time.time;
					}
					else if (Time.time - m_rainTimer > 60f)
					{
						m_rainTimer = Time.time;
						num += 5f;
					}
				}
				else
				{
					m_rainTimer = 0f;
				}
			}
			if (m_noSupportWear)
			{
				UpdateSupport();
				if (!HaveSupport())
				{
					num = 100f;
				}
			}
			if (num > 0f && !CanBeRemoved())
			{
				num = 0f;
			}
			if (num > 0f)
			{
				float damage = num / 100f * m_health;
				ApplyDamage(damage);
			}
		}
		UpdateVisual(triggerEffects: true);
	}

	private Vector3 GetCOM()
	{
		return base.transform.position + base.transform.rotation * m_comOffset;
	}

	private void UpdateSupport()
	{
		if (m_colliders == null)
		{
			SetupColliders();
		}
		GetMaterialProperties(out var maxSupport, out var _, out var horizontalLoss, out var verticalLoss);
		m_tempSupportPoints.Clear();
		m_tempSupportPointValues.Clear();
		Vector3 cOM = GetCOM();
		float a = 0f;
		foreach (BoundData bound in m_bounds)
		{
			int num = Physics.OverlapBoxNonAlloc(bound.m_pos, bound.m_size, m_tempColliders, bound.m_rot, m_rayMask);
			for (int i = 0; i < num; i++)
			{
				Collider collider = m_tempColliders[i];
				if (m_colliders.Contains(collider) || collider.attachedRigidbody != null || collider.isTrigger)
				{
					continue;
				}
				WearNTear componentInParent = collider.GetComponentInParent<WearNTear>();
				if (componentInParent == null)
				{
					m_support = maxSupport;
					m_nview.GetZDO().Set("support", m_support);
					return;
				}
				if (!componentInParent.m_supports)
				{
					continue;
				}
				float num2 = Vector3.Distance(cOM, componentInParent.transform.position) + 0.1f;
				float support = componentInParent.GetSupport();
				a = Mathf.Max(a, support - horizontalLoss * num2 * support);
				Vector3 vector = FindSupportPoint(cOM, componentInParent, collider);
				if (vector.y < cOM.y + 0.05f)
				{
					Vector3 normalized = (vector - cOM).normalized;
					if (normalized.y < 0f)
					{
						float t = Mathf.Acos(1f - Mathf.Abs(normalized.y)) / ((float)Math.PI / 2f);
						float num3 = Mathf.Lerp(horizontalLoss, verticalLoss, t);
						float b = support - num3 * num2 * support;
						a = Mathf.Max(a, b);
					}
					float item = support - verticalLoss * num2 * support;
					m_tempSupportPoints.Add(vector);
					m_tempSupportPointValues.Add(item);
				}
			}
		}
		if (m_tempSupportPoints.Count > 0 && m_tempSupportPoints.Count >= 2)
		{
			for (int j = 0; j < m_tempSupportPoints.Count; j++)
			{
				Vector3 from = m_tempSupportPoints[j] - cOM;
				from.y = 0f;
				for (int k = 0; k < m_tempSupportPoints.Count; k++)
				{
					if (j != k)
					{
						Vector3 to = m_tempSupportPoints[k] - cOM;
						to.y = 0f;
						if (Vector3.Angle(from, to) >= 100f)
						{
							float b2 = (m_tempSupportPointValues[j] + m_tempSupportPointValues[k]) * 0.5f;
							a = Mathf.Max(a, b2);
						}
					}
				}
			}
		}
		m_support = Mathf.Min(a, maxSupport);
		m_nview.GetZDO().Set("support", m_support);
	}

	private Vector3 FindSupportPoint(Vector3 com, WearNTear wnt, Collider otherCollider)
	{
		MeshCollider meshCollider = otherCollider as MeshCollider;
		if (meshCollider != null && !meshCollider.convex)
		{
			if (meshCollider.Raycast(new Ray(com, Vector3.down), out var hitInfo, 10f))
			{
				return hitInfo.point;
			}
			return (com + wnt.GetCOM()) * 0.5f;
		}
		return otherCollider.ClosestPoint(com);
	}

	private bool HaveSupport()
	{
		return m_support >= GetMinSupport();
	}

	private bool IsUnderWater()
	{
		float liquidLevel = Floating.GetLiquidLevel(base.transform.position);
		return base.transform.position.y < liquidLevel;
	}

	private bool HaveRoof()
	{
		int num = Physics.SphereCastNonAlloc(base.transform.position, 0.1f, Vector3.up, m_raycastHits, 100f, m_rayMask);
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = m_raycastHits[i];
			if (!raycastHit.collider.gameObject.CompareTag("leaky"))
			{
				return true;
			}
		}
		return false;
	}

	private void RPC_HealthChanged(long peer, float health)
	{
		float health2 = health / m_health;
		SetHealthVisual(health2, triggerEffects: true);
	}

	private void UpdateVisual(bool triggerEffects)
	{
		if (m_nview.IsValid())
		{
			SetHealthVisual(GetHealthPercentage(), triggerEffects);
		}
	}

	private void SetHealthVisual(float health, bool triggerEffects)
	{
		if (m_worn == null && m_broken == null && m_new == null)
		{
			return;
		}
		if (health > 0.75f)
		{
			if (m_worn != m_new)
			{
				m_worn.SetActive(value: false);
			}
			if (m_broken != m_new)
			{
				m_broken.SetActive(value: false);
			}
			m_new.SetActive(value: true);
		}
		else if (health > 0.25f)
		{
			if (triggerEffects && !m_worn.activeSelf)
			{
				m_switchEffect.Create(base.transform.position, base.transform.rotation, base.transform);
			}
			if (m_new != m_worn)
			{
				m_new.SetActive(value: false);
			}
			if (m_broken != m_worn)
			{
				m_broken.SetActive(value: false);
			}
			m_worn.SetActive(value: true);
		}
		else
		{
			if (triggerEffects && !m_broken.activeSelf)
			{
				m_switchEffect.Create(base.transform.position, base.transform.rotation, base.transform);
			}
			if (m_new != m_broken)
			{
				m_new.SetActive(value: false);
			}
			if (m_worn != m_broken)
			{
				m_worn.SetActive(value: false);
			}
			m_broken.SetActive(value: true);
		}
	}

	public float GetHealthPercentage()
	{
		if (!m_nview.IsValid())
		{
			return 1f;
		}
		return Mathf.Clamp01(m_nview.GetZDO().GetFloat("health", m_health) / m_health);
	}

	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Default;
	}

	public void Damage(HitData hit)
	{
		if (m_nview.IsValid())
		{
			m_nview.InvokeRPC("WNTDamage", hit);
		}
	}

	private bool CanBeRemoved()
	{
		if ((bool)m_piece)
		{
			return m_piece.CanBeRemoved();
		}
		return true;
	}

	private void RPC_Damage(long sender, HitData hit)
	{
		if (!m_nview.IsOwner() || m_nview.GetZDO().GetFloat("health", m_health) <= 0f)
		{
			return;
		}
		hit.ApplyResistance(m_damages, out var significantModifier);
		float totalDamage = hit.GetTotalDamage();
		if ((bool)m_piece && m_piece.IsPlacedByPlayer())
		{
			PrivateArea.CheckInPrivateArea(base.transform.position, flash: true);
		}
		DamageText.instance.ShowText(significantModifier, hit.m_point, totalDamage);
		if (totalDamage <= 0f)
		{
			return;
		}
		ApplyDamage(totalDamage);
		m_hitEffect.Create(hit.m_point, Quaternion.identity, base.transform);
		if (m_hitNoise > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(hit.m_point, 10f);
			if ((bool)closestPlayer)
			{
				closestPlayer.AddNoise(m_hitNoise);
			}
		}
		if (m_onDamaged != null)
		{
			m_onDamaged();
		}
	}

	public bool ApplyDamage(float damage)
	{
		float @float = m_nview.GetZDO().GetFloat("health", m_health);
		if (@float <= 0f)
		{
			return false;
		}
		@float -= damage;
		m_nview.GetZDO().Set("health", @float);
		if (@float <= 0f)
		{
			Destroy();
		}
		else
		{
			m_nview.InvokeRPC(ZNetView.Everybody, "WNTHealthChanged", @float);
		}
		return true;
	}

	public void Remove()
	{
		if (m_nview.IsValid())
		{
			m_nview.InvokeRPC("WNTRemove");
		}
	}

	private void RPC_Remove(long sender)
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			Destroy();
		}
	}

	private void Destroy()
	{
		m_nview.GetZDO().Set("health", 0f);
		if ((bool)m_piece)
		{
			m_piece.DropResources();
		}
		if (m_onDestroyed != null)
		{
			m_onDestroyed();
		}
		if (m_destroyNoise > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 10f);
			if ((bool)closestPlayer)
			{
				closestPlayer.AddNoise(m_destroyNoise);
			}
		}
		m_destroyedEffect.Create(base.transform.position, base.transform.rotation, base.transform);
		if (m_autoCreateFragments)
		{
			m_nview.InvokeRPC(ZNetView.Everybody, "WNTCreateFragments");
		}
		ZNetScene.instance.Destroy(base.gameObject);
	}

	private void RPC_CreateFragments(long peer)
	{
		ResetHighlight();
		if (m_fragmentRoots != null && m_fragmentRoots.Length != 0)
		{
			GameObject[] fragmentRoots = m_fragmentRoots;
			foreach (GameObject obj in fragmentRoots)
			{
				obj.SetActive(value: true);
				Destructible.CreateFragments(obj, visibleOnly: false);
			}
		}
		else
		{
			Destructible.CreateFragments(base.gameObject);
		}
	}

	private float GetMaxSupport()
	{
		GetMaterialProperties(out var maxSupport, out var _, out var _, out var _);
		return maxSupport;
	}

	private float GetMinSupport()
	{
		GetMaterialProperties(out var _, out var minSupport, out var _, out var _);
		return minSupport;
	}

	private void GetMaterialProperties(out float maxSupport, out float minSupport, out float horizontalLoss, out float verticalLoss)
	{
		switch (m_materialType)
		{
		case MaterialType.Wood:
			maxSupport = 100f;
			minSupport = 10f;
			verticalLoss = 0.125f;
			horizontalLoss = 0.2f;
			break;
		case MaterialType.HardWood:
			maxSupport = 140f;
			minSupport = 10f;
			verticalLoss = 0.1f;
			horizontalLoss = 355f / (678f * (float)Math.PI);
			break;
		case MaterialType.Stone:
			maxSupport = 1000f;
			minSupport = 100f;
			verticalLoss = 0.125f;
			horizontalLoss = 1f;
			break;
		case MaterialType.Iron:
			maxSupport = 1500f;
			minSupport = 20f;
			verticalLoss = 0.07692308f;
			horizontalLoss = 0.07692308f;
			break;
		default:
			maxSupport = 0f;
			minSupport = 0f;
			verticalLoss = 0f;
			horizontalLoss = 0f;
			break;
		}
	}

	public static List<WearNTear> GetAllInstaces()
	{
		return m_allInstances;
	}
}
