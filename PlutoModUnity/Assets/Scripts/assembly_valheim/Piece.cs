using System;
using System.Collections.Generic;
using UnityEngine;

public class Piece : StaticTarget
{
	public enum PieceCategory
	{
		Misc = 0,
		Crafting = 1,
		Building = 2,
		Furniture = 3,
		Max = 4,
		All = 100
	}

	public enum ComfortGroup
	{
		None,
		Fire,
		Bed,
		Banner,
		Chair,
		Table
	}

	[Serializable]
	public class Requirement
	{
		[Header("Resource")]
		public ItemDrop m_resItem;

		public int m_amount = 1;

		[Header("Item")]
		public int m_amountPerLevel = 1;

		[Header("Piece")]
		public bool m_recover = true;

		public int GetAmount(int qualityLevel)
		{
			if (qualityLevel <= 1)
			{
				return m_amount;
			}
			return (qualityLevel - 1) * m_amountPerLevel;
		}
	}

	private static int pieceRayMask = 0;

	private static Collider[] pieceColliders = new Collider[2000];

	private static int ghostLayer = 0;

	public bool m_targetNonPlayerBuilt = true;

	[Header("Basic stuffs")]
	public Sprite m_icon;

	public string m_name = "";

	public string m_description = "";

	public bool m_enabled = true;

	public PieceCategory m_category;

	public bool m_isUpgrade;

	[Header("Comfort")]
	public int m_comfort;

	public ComfortGroup m_comfortGroup;

	public GameObject m_comfortObject;

	[Header("Placement rules")]
	public bool m_groundPiece;

	public bool m_allowAltGroundPlacement;

	public bool m_groundOnly;

	public bool m_cultivatedGroundOnly;

	public bool m_waterPiece;

	public bool m_clipGround;

	public bool m_clipEverything;

	public bool m_noInWater;

	public bool m_notOnWood;

	public bool m_notOnTiltingSurface;

	public bool m_inCeilingOnly;

	public bool m_notOnFloor;

	public bool m_noClipping;

	public bool m_onlyInTeleportArea;

	public bool m_allowedInDungeons;

	public float m_spaceRequirement;

	public bool m_repairPiece;

	public bool m_canBeRemoved = true;

	public bool m_allowRotatedOverlap;

	[BitMask(typeof(Heightmap.Biome))]
	public Heightmap.Biome m_onlyInBiome;

	[Header("Effects")]
	public EffectList m_placeEffect = new EffectList();

	[Header("Requirements")]
	public string m_dlc = "";

	public CraftingStation m_craftingStation;

	public Requirement[] m_resources = new Requirement[0];

	public GameObject m_destroyedLootPrefab;

	private ZNetView m_nview;

	private List<KeyValuePair<Renderer, Material[]>> m_invalidPlacementMaterials;

	private long m_creator;

	private int m_myListIndex = -1;

	private static List<Piece> m_allPieces = new List<Piece>();

	private static HashSet<Piece> m_allComfortPieces = new HashSet<Piece>();

	private static int m_creatorHash = 0;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_allPieces.Add(this);
		m_myListIndex = m_allPieces.Count - 1;
		if (m_comfort > 0)
		{
			m_allComfortPieces.Add(this);
		}
		if ((bool)m_nview && m_nview.IsValid())
		{
			if (m_creatorHash == 0)
			{
				m_creatorHash = "creator".GetStableHashCode();
			}
			m_creator = m_nview.GetZDO().GetLong(m_creatorHash, 0L);
		}
	}

	private void OnDestroy()
	{
		if (m_myListIndex >= 0)
		{
			m_allPieces[m_myListIndex] = m_allPieces[m_allPieces.Count - 1];
			m_allPieces[m_myListIndex].m_myListIndex = m_myListIndex;
			m_allPieces.RemoveAt(m_allPieces.Count - 1);
			m_myListIndex = -1;
		}
		if (m_comfort > 0)
		{
			m_allComfortPieces.Remove(this);
		}
	}

	public bool CanBeRemoved()
	{
		Container componentInChildren = GetComponentInChildren<Container>();
		if (componentInChildren != null)
		{
			return componentInChildren.CanBeRemoved();
		}
		Ship componentInChildren2 = GetComponentInChildren<Ship>();
		if (componentInChildren2 != null)
		{
			return componentInChildren2.CanBeRemoved();
		}
		return true;
	}

	public void DropResources()
	{
		Container container = null;
		Requirement[] resources = m_resources;
		foreach (Requirement requirement in resources)
		{
			if (requirement.m_resItem == null || !requirement.m_recover)
			{
				continue;
			}
			GameObject gameObject = requirement.m_resItem.gameObject;
			int num = requirement.m_amount;
			if (!IsPlacedByPlayer())
			{
				num = Mathf.Max(1, num / 3);
			}
			if ((bool)m_destroyedLootPrefab)
			{
				while (num > 0)
				{
					ItemDrop.ItemData itemData = gameObject.GetComponent<ItemDrop>().m_itemData.Clone();
					itemData.m_dropPrefab = gameObject;
					itemData.m_stack = Mathf.Min(num, itemData.m_shared.m_maxStackSize);
					num -= itemData.m_stack;
					if (container == null || !container.GetInventory().HaveEmptySlot())
					{
						container = UnityEngine.Object.Instantiate(m_destroyedLootPrefab, base.transform.position + Vector3.up, Quaternion.identity).GetComponent<Container>();
					}
					container.GetInventory().AddItem(itemData);
				}
			}
			else
			{
				while (num > 0)
				{
					ItemDrop component = UnityEngine.Object.Instantiate(gameObject, base.transform.position + Vector3.up, Quaternion.identity).GetComponent<ItemDrop>();
					component.SetStack(Mathf.Min(num, component.m_itemData.m_shared.m_maxStackSize));
					num -= component.m_itemData.m_stack;
				}
			}
		}
	}

	public override bool IsPriorityTarget()
	{
		if (base.IsPriorityTarget() && (m_targetNonPlayerBuilt || IsPlacedByPlayer()))
		{
			return true;
		}
		return false;
	}

	public override bool IsRandomTarget()
	{
		if (base.IsRandomTarget() && (m_targetNonPlayerBuilt || IsPlacedByPlayer()))
		{
			return true;
		}
		return false;
	}

	public void SetCreator(long uid)
	{
		if (!(m_nview == null) && m_nview.IsOwner() && GetCreator() == 0L)
		{
			m_creator = uid;
			m_nview.GetZDO().Set(m_creatorHash, uid);
		}
	}

	public long GetCreator()
	{
		return m_creator;
	}

	public bool IsCreator()
	{
		long creator = GetCreator();
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		return creator == playerID;
	}

	public bool IsPlacedByPlayer()
	{
		return GetCreator() != 0;
	}

	public void SetInvalidPlacementHeightlight(bool enabled)
	{
		if ((enabled && m_invalidPlacementMaterials != null) || (!enabled && m_invalidPlacementMaterials == null))
		{
			return;
		}
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>();
		if (enabled)
		{
			m_invalidPlacementMaterials = new List<KeyValuePair<Renderer, Material[]>>();
			Renderer[] array = componentsInChildren;
			foreach (Renderer renderer in array)
			{
				Material[] sharedMaterials = renderer.sharedMaterials;
				m_invalidPlacementMaterials.Add(new KeyValuePair<Renderer, Material[]>(renderer, sharedMaterials));
			}
			array = componentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				Material[] materials = array[i].materials;
				foreach (Material material in materials)
				{
					if (material.HasProperty("_EmissionColor"))
					{
						material.SetColor("_EmissionColor", Color.red * 0.7f);
					}
					material.color = Color.red;
				}
			}
			return;
		}
		foreach (KeyValuePair<Renderer, Material[]> invalidPlacementMaterial in m_invalidPlacementMaterials)
		{
			if ((bool)invalidPlacementMaterial.Key)
			{
				invalidPlacementMaterial.Key.materials = invalidPlacementMaterial.Value;
			}
		}
		m_invalidPlacementMaterials = null;
	}

	public static void GetSnapPoints(Vector3 point, float radius, List<Transform> points, List<Piece> pieces)
	{
		if (pieceRayMask == 0)
		{
			pieceRayMask = LayerMask.GetMask("piece", "piece_nonsolid");
		}
		int num = Physics.OverlapSphereNonAlloc(point, radius, pieceColliders, pieceRayMask);
		for (int i = 0; i < num; i++)
		{
			Piece componentInParent = pieceColliders[i].GetComponentInParent<Piece>();
			if (componentInParent != null)
			{
				componentInParent.GetSnapPoints(points);
				pieces.Add(componentInParent);
			}
		}
	}

	public static void GetAllPiecesInRadius(Vector3 p, float radius, List<Piece> pieces)
	{
		if (ghostLayer == 0)
		{
			ghostLayer = LayerMask.NameToLayer("ghost");
		}
		foreach (Piece allPiece in m_allPieces)
		{
			if (allPiece.gameObject.layer != ghostLayer && Vector3.Distance(p, allPiece.transform.position) < radius)
			{
				pieces.Add(allPiece);
			}
		}
	}

	public static void GetAllComfortPiecesInRadius(Vector3 p, float radius, List<Piece> pieces)
	{
		if (ghostLayer == 0)
		{
			ghostLayer = LayerMask.NameToLayer("ghost");
		}
		foreach (Piece allComfortPiece in m_allComfortPieces)
		{
			if (allComfortPiece.gameObject.layer != ghostLayer && Vector3.Distance(p, allComfortPiece.transform.position) < radius)
			{
				pieces.Add(allComfortPiece);
			}
		}
	}

	public void GetSnapPoints(List<Transform> points)
	{
		for (int i = 0; i < base.transform.childCount; i++)
		{
			Transform child = base.transform.GetChild(i);
			if (child.CompareTag("snappoint"))
			{
				points.Add(child);
			}
		}
	}

	public int GetComfort()
	{
		if (m_comfortObject != null && !m_comfortObject.activeInHierarchy)
		{
			return 0;
		}
		return m_comfort;
	}
}
