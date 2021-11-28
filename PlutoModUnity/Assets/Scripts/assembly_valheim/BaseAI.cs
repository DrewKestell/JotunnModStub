using System;
using System.Collections.Generic;
using UnityEngine;

public class BaseAI : MonoBehaviour
{
	private float m_lastMoveToWaterUpdate;

	private bool m_haveWaterPosition;

	private Vector3 m_moveToWaterPosition = Vector3.zero;

	private float m_fleeTargetUpdateTime;

	private Vector3 m_fleeTarget = Vector3.zero;

	private float m_nearFireTime;

	private EffectArea m_nearFireArea;

	private float aroundPointUpdateTime;

	private Vector3 arroundPointTarget = Vector3.zero;

	private Vector3 m_lastMovementCheck;

	private float m_lastMoveTime;

	private const bool m_debugDraw = false;

	public float m_viewRange = 50f;

	public float m_viewAngle = 90f;

	public float m_hearRange = 9999f;

	private const float m_interiorMaxHearRange = 8f;

	private const float m_despawnDistance = 80f;

	private const float m_regenAllHPTime = 3600f;

	public EffectList m_alertedEffects = new EffectList();

	public EffectList m_idleSound = new EffectList();

	public float m_idleSoundInterval = 5f;

	public float m_idleSoundChance = 0.5f;

	public Pathfinding.AgentType m_pathAgentType = Pathfinding.AgentType.Humanoid;

	public float m_moveMinAngle = 10f;

	public bool m_smoothMovement = true;

	public bool m_serpentMovement;

	public float m_serpentTurnRadius = 20f;

	public float m_jumpInterval;

	[Header("Random circle")]
	public float m_randomCircleInterval = 2f;

	[Header("Random movement")]
	public float m_randomMoveInterval = 5f;

	public float m_randomMoveRange = 4f;

	[Header("Fly behaviour")]
	public bool m_randomFly;

	public float m_chanceToTakeoff = 1f;

	public float m_chanceToLand = 1f;

	public float m_groundDuration = 10f;

	public float m_airDuration = 10f;

	public float m_maxLandAltitude = 5f;

	public float m_flyAltitudeMin = 3f;

	public float m_flyAltitudeMax = 10f;

	public float m_takeoffTime = 5f;

	[Header("Other")]
	public bool m_avoidFire;

	public bool m_afraidOfFire;

	public bool m_avoidWater = true;

	public string m_spawnMessage = "";

	public string m_deathMessage = "";

	private bool m_patrol;

	private Vector3 m_patrolPoint = Vector3.zero;

	private float m_patrolPointUpdateTime;

	protected ZNetView m_nview;

	protected Character m_character;

	protected ZSyncAnimation m_animator;

	protected Rigidbody m_body;

	protected Tameable m_tamable;

	private float m_updateTimer;

	private int m_solidRayMask;

	private int m_viewBlockMask;

	private int m_monsterTargetRayMask;

	private Vector3 m_randomMoveTarget = Vector3.zero;

	private float m_randomMoveUpdateTimer;

	private bool m_reachedRandomMoveTarget = true;

	private float m_jumpTimer;

	private float m_randomFlyTimer;

	private float m_regenTimer;

	protected bool m_alerted;

	protected bool m_huntPlayer;

	protected Vector3 m_spawnPoint = Vector3.zero;

	private const float m_getOfOfCornerMaxAngle = 20f;

	private float m_getOutOfCornerTimer;

	private float m_getOutOfCornerAngle;

	private Vector3 m_lastPosition = Vector3.zero;

	private float m_stuckTimer;

	protected float m_timeSinceHurt = 99999f;

	private Vector3 m_lastFindPathTarget = new Vector3(-999999f, -999999f, -999999f);

	private float m_lastFindPathTime;

	private bool m_lastFindPathResult;

	private List<Vector3> m_path = new List<Vector3>();

	private static RaycastHit[] m_tempRaycastHits = new RaycastHit[128];

	private static List<BaseAI> m_instances = new List<BaseAI>();

	private static int worldTimeHash = "lastWorldTime".GetStableHashCode();

	private static int spawnTimeHash = "spawntime".GetStableHashCode();

	private static int havetTargetHash = "haveTarget".GetStableHashCode();

	protected virtual void Awake()
	{
		m_instances.Add(this);
		m_nview = GetComponent<ZNetView>();
		m_character = GetComponent<Character>();
		m_animator = GetComponent<ZSyncAnimation>();
		m_body = GetComponent<Rigidbody>();
		m_tamable = GetComponent<Tameable>();
		m_solidRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
		m_viewBlockMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "viewblock", "vehicle");
		m_monsterTargetRayMask = LayerMask.GetMask("piece", "piece_nonsolid", "Default", "static_solid", "Default_small", "vehicle");
		Character character = m_character;
		character.m_onDamaged = (Action<float, Character>)Delegate.Combine(character.m_onDamaged, new Action<float, Character>(OnDamaged));
		Character character2 = m_character;
		character2.m_onDeath = (Action)Delegate.Combine(character2.m_onDeath, new Action(OnDeath));
		if (m_nview.IsOwner() && m_nview.GetZDO().GetLong(spawnTimeHash, 0L) == 0L)
		{
			m_nview.GetZDO().Set(spawnTimeHash, ZNet.instance.GetTime().Ticks);
			if (!string.IsNullOrEmpty(m_spawnMessage))
			{
				MessageHud.instance.MessageAll(MessageHud.MessageType.Center, m_spawnMessage);
			}
		}
		m_randomMoveUpdateTimer = UnityEngine.Random.Range(0f, m_randomMoveInterval);
		m_nview.Register("Alert", RPC_Alert);
		m_nview.Register<Vector3, float, ZDOID>("OnNearProjectileHit", RPC_OnNearProjectileHit);
		m_huntPlayer = m_nview.GetZDO().GetBool("huntplayer", m_huntPlayer);
		m_spawnPoint = m_nview.GetZDO().GetVec3("spawnpoint", base.transform.position);
		if (m_nview.IsOwner())
		{
			m_nview.GetZDO().Set("spawnpoint", m_spawnPoint);
		}
		InvokeRepeating("DoIdleSound", m_idleSoundInterval, m_idleSoundInterval);
	}

	private void OnDestroy()
	{
		m_instances.Remove(this);
	}

	public void SetPatrolPoint()
	{
		SetPatrolPoint(base.transform.position);
	}

	public void SetPatrolPoint(Vector3 point)
	{
		m_patrol = true;
		m_patrolPoint = point;
		m_nview.GetZDO().Set("patrolPoint", point);
		m_nview.GetZDO().Set("patrol", value: true);
	}

	public void ResetPatrolPoint()
	{
		m_patrol = false;
		m_nview.GetZDO().Set("patrol", value: false);
	}

	public bool GetPatrolPoint(out Vector3 point)
	{
		if (Time.time - m_patrolPointUpdateTime > 1f)
		{
			m_patrolPointUpdateTime = Time.time;
			m_patrol = m_nview.GetZDO().GetBool("patrol");
			if (m_patrol)
			{
				m_patrolPoint = m_nview.GetZDO().GetVec3("patrolPoint", m_patrolPoint);
			}
		}
		point = m_patrolPoint;
		return m_patrol;
	}

	private void FixedUpdate()
	{
		if (m_nview.IsValid())
		{
			m_updateTimer += Time.fixedDeltaTime;
			if (m_updateTimer >= 0.05f)
			{
				UpdateAI(0.05f);
				m_updateTimer -= 0.05f;
			}
		}
	}

	protected virtual void UpdateAI(float dt)
	{
		if (m_nview.IsOwner())
		{
			UpdateTakeoffLanding(dt);
			if (m_jumpInterval > 0f)
			{
				m_jumpTimer += dt;
			}
			if (m_randomMoveUpdateTimer > 0f)
			{
				m_randomMoveUpdateTimer -= dt;
			}
			UpdateRegeneration(dt);
			m_timeSinceHurt += dt;
		}
		else
		{
			m_alerted = m_nview.GetZDO().GetBool("alert");
		}
	}

	private void UpdateRegeneration(float dt)
	{
		m_regenTimer += dt;
		if (m_regenTimer > 2f)
		{
			m_regenTimer = 0f;
			if (!m_tamable || !m_character.IsTamed() || !m_tamable.IsHungry())
			{
				float worldTimeDelta = GetWorldTimeDelta();
				float num = m_character.GetMaxHealth() / 3600f;
				m_character.Heal(num * worldTimeDelta, (bool)m_tamable && m_character.IsTamed());
			}
		}
	}

	public bool IsTakingOff()
	{
		if (m_randomFly && m_character.IsFlying() && m_randomFlyTimer < m_takeoffTime)
		{
			return true;
		}
		return false;
	}

	public void UpdateTakeoffLanding(float dt)
	{
		if (!m_randomFly)
		{
			return;
		}
		m_randomFlyTimer += dt;
		if (m_character.InAttack() || m_character.IsStaggering())
		{
			return;
		}
		if (m_character.IsFlying())
		{
			if (m_randomFlyTimer > m_airDuration && GetAltitude() < m_maxLandAltitude)
			{
				m_randomFlyTimer = 0f;
				if (UnityEngine.Random.value <= m_chanceToLand)
				{
					m_character.m_flying = false;
					m_animator.SetTrigger("fly_land");
				}
			}
		}
		else if (m_randomFlyTimer > m_groundDuration)
		{
			m_randomFlyTimer = 0f;
			if (UnityEngine.Random.value <= m_chanceToTakeoff)
			{
				m_character.m_flying = true;
				m_character.m_jumpEffects.Create(m_character.transform.position, Quaternion.identity);
				m_animator.SetTrigger("fly_takeoff");
			}
		}
	}

	private float GetWorldTimeDelta()
	{
		DateTime time = ZNet.instance.GetTime();
		long @long = m_nview.GetZDO().GetLong(worldTimeHash, 0L);
		if (@long == 0L)
		{
			m_nview.GetZDO().Set(worldTimeHash, time.Ticks);
			return 0f;
		}
		DateTime dateTime = new DateTime(@long);
		TimeSpan timeSpan = time - dateTime;
		m_nview.GetZDO().Set(worldTimeHash, time.Ticks);
		return (float)timeSpan.TotalSeconds;
	}

	public TimeSpan GetTimeSinceSpawned()
	{
		long num = m_nview.GetZDO().GetLong("spawntime", 0L);
		if (num == 0L)
		{
			num = ZNet.instance.GetTime().Ticks;
			m_nview.GetZDO().Set("spawntime", num);
		}
		DateTime dateTime = new DateTime(num);
		return ZNet.instance.GetTime() - dateTime;
	}

	private void DoIdleSound()
	{
		if (!IsSleeping() && !(UnityEngine.Random.value > m_idleSoundChance))
		{
			m_idleSound.Create(base.transform.position, Quaternion.identity);
		}
	}

	protected void Follow(GameObject go, float dt)
	{
		float num = Vector3.Distance(go.transform.position, base.transform.position);
		bool run = num > 10f;
		if (num < 3f)
		{
			StopMoving();
		}
		else
		{
			MoveTo(dt, go.transform.position, 0f, run);
		}
	}

	protected void MoveToWater(float dt, float maxRange)
	{
		float num = (m_haveWaterPosition ? 2f : 0.5f);
		if (Time.time - m_lastMoveToWaterUpdate > num)
		{
			m_lastMoveToWaterUpdate = Time.time;
			Vector3 moveToWaterPosition = base.transform.position;
			for (int i = 0; i < 10; i++)
			{
				Vector3 vector = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * UnityEngine.Random.Range(4f, maxRange);
				Vector3 vector2 = base.transform.position + vector;
				vector2.y = ZoneSystem.instance.GetSolidHeight(vector2);
				if (vector2.y < moveToWaterPosition.y)
				{
					moveToWaterPosition = vector2;
				}
			}
			if (moveToWaterPosition.y < ZoneSystem.instance.m_waterLevel)
			{
				m_moveToWaterPosition = moveToWaterPosition;
				m_haveWaterPosition = true;
			}
			else
			{
				m_haveWaterPosition = false;
			}
		}
		if (m_haveWaterPosition)
		{
			MoveTowards(m_moveToWaterPosition - base.transform.position, run: true);
		}
	}

	protected void MoveAwayAndDespawn(float dt, bool run)
	{
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 40f);
		if (closestPlayer != null)
		{
			Vector3 normalized = (closestPlayer.transform.position - base.transform.position).normalized;
			MoveTo(dt, base.transform.position - normalized * 5f, 0f, run);
		}
		else
		{
			m_nview.Destroy();
		}
	}

	protected void IdleMovement(float dt)
	{
		Vector3 centerPoint = ((m_character.IsTamed() || HuntPlayer()) ? base.transform.position : m_spawnPoint);
		if (GetPatrolPoint(out var point))
		{
			centerPoint = point;
		}
		RandomMovement(dt, centerPoint);
	}

	protected void RandomMovement(float dt, Vector3 centerPoint)
	{
		if (m_randomMoveUpdateTimer <= 0f)
		{
			if (Utils.DistanceXZ(centerPoint, base.transform.position) > m_randomMoveRange * 2f)
			{
				Vector3 vector = centerPoint - base.transform.position;
				vector.y = 0f;
				vector.Normalize();
				vector = Quaternion.Euler(0f, UnityEngine.Random.Range(-30, 30), 0f) * vector;
				m_randomMoveTarget = base.transform.position + vector * m_randomMoveRange * 2f;
			}
			else
			{
				Vector3 vector2 = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f) * base.transform.forward * UnityEngine.Random.Range(m_randomMoveRange * 0.7f, m_randomMoveRange);
				m_randomMoveTarget = centerPoint + vector2;
			}
			if (m_character.IsFlying() && ZoneSystem.instance.GetSolidHeight(m_randomMoveTarget, out var height))
			{
				if (height < ZoneSystem.instance.m_waterLevel)
				{
					height = ZoneSystem.instance.m_waterLevel;
				}
				m_randomMoveTarget.y = height + UnityEngine.Random.Range(m_flyAltitudeMin, m_flyAltitudeMax);
			}
			if (!IsValidRandomMovePoint(m_randomMoveTarget))
			{
				return;
			}
			m_reachedRandomMoveTarget = false;
			m_randomMoveUpdateTimer = UnityEngine.Random.Range(m_randomMoveInterval, m_randomMoveInterval + m_randomMoveInterval / 2f);
			if (m_avoidWater && m_character.IsSwiming())
			{
				m_randomMoveUpdateTimer /= 4f;
			}
		}
		if (!m_reachedRandomMoveTarget)
		{
			bool flag = IsAlerted() || Utils.DistanceXZ(base.transform.position, centerPoint) > m_randomMoveRange * 2f;
			if (MoveTo(dt, m_randomMoveTarget, 0f, flag))
			{
				m_reachedRandomMoveTarget = true;
				if (flag)
				{
					m_randomMoveUpdateTimer = 0f;
				}
			}
		}
		else
		{
			StopMoving();
		}
	}

	public void ResetRandomMovement()
	{
		m_reachedRandomMoveTarget = true;
		m_randomMoveUpdateTimer = UnityEngine.Random.Range(m_randomMoveInterval, m_randomMoveInterval + m_randomMoveInterval / 2f);
	}

	protected bool Flee(float dt, Vector3 from)
	{
		float time = Time.time;
		if (time - m_fleeTargetUpdateTime > 2f)
		{
			m_fleeTargetUpdateTime = time;
			Vector3 vector = -(from - base.transform.position);
			vector.y = 0f;
			vector.Normalize();
			bool flag = false;
			for (int i = 0; i < 4; i++)
			{
				m_fleeTarget = base.transform.position + Quaternion.Euler(0f, UnityEngine.Random.Range(-45f, 45f), 0f) * vector * 25f;
				if (HavePath(m_fleeTarget) && (!m_avoidWater || m_character.IsSwiming() || !(ZoneSystem.instance.GetSolidHeight(m_fleeTarget) < ZoneSystem.instance.m_waterLevel)))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				m_fleeTarget = base.transform.position + Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * 25f;
			}
		}
		return MoveTo(dt, m_fleeTarget, 1f, IsAlerted());
	}

	protected bool AvoidFire(float dt, Character moveToTarget, bool superAfraid)
	{
		if (superAfraid)
		{
			EffectArea effectArea = EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Fire, 3f);
			if ((bool)effectArea)
			{
				m_nearFireTime = Time.time;
				m_nearFireArea = effectArea;
			}
			if (Time.time - m_nearFireTime < 6f && (bool)m_nearFireArea)
			{
				SetAlerted(alert: true);
				Flee(dt, m_nearFireArea.transform.position);
				return true;
			}
		}
		else
		{
			EffectArea effectArea2 = EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Fire, 3f);
			if ((bool)effectArea2)
			{
				if (moveToTarget != null && (bool)EffectArea.IsPointInsideArea(moveToTarget.transform.position, EffectArea.Type.Fire))
				{
					RandomMovementArroundPoint(dt, effectArea2.transform.position, effectArea2.GetRadius() + 3f + 1f, IsAlerted());
					return true;
				}
				RandomMovementArroundPoint(dt, effectArea2.transform.position, (effectArea2.GetRadius() + 3f) * 1.5f, IsAlerted());
				return true;
			}
		}
		return false;
	}

	protected void RandomMovementArroundPoint(float dt, Vector3 point, float distance, bool run)
	{
		float time = Time.time;
		if (time - aroundPointUpdateTime > m_randomCircleInterval)
		{
			aroundPointUpdateTime = time;
			Vector3 vector = base.transform.position - point;
			vector.y = 0f;
			vector.Normalize();
			float num = ((!(Vector3.Distance(base.transform.position, point) < distance / 2f)) ? ((float)(((double)UnityEngine.Random.value > 0.5) ? 40 : (-40))) : ((float)(((double)UnityEngine.Random.value > 0.5) ? 90 : (-90))));
			Vector3 vector2 = Quaternion.Euler(0f, num, 0f) * vector;
			arroundPointTarget = point + vector2 * distance;
			if (Vector3.Dot(base.transform.forward, arroundPointTarget - base.transform.position) < 0f)
			{
				vector2 = Quaternion.Euler(0f, 0f - num, 0f) * vector;
				arroundPointTarget = point + vector2 * distance;
				if (m_serpentMovement && Vector3.Distance(point, base.transform.position) > distance / 2f && Vector3.Dot(base.transform.forward, arroundPointTarget - base.transform.position) < 0f)
				{
					arroundPointTarget = point - vector2 * distance;
				}
			}
			if (m_character.IsFlying())
			{
				arroundPointTarget.y += UnityEngine.Random.Range(m_flyAltitudeMin, m_flyAltitudeMax);
			}
		}
		if (MoveTo(dt, arroundPointTarget, 0f, run))
		{
			if (run)
			{
				aroundPointUpdateTime = 0f;
			}
			if (!m_serpentMovement && !run)
			{
				LookAt(point);
			}
		}
	}

	private bool GetSolidHeight(Vector3 p, out float height, float maxYDistance)
	{
		if (Physics.Raycast(p + Vector3.up * maxYDistance, Vector3.down, out var hitInfo, maxYDistance * 2f, m_solidRayMask))
		{
			height = hitInfo.point.y;
			return true;
		}
		height = 0f;
		return false;
	}

	protected bool IsValidRandomMovePoint(Vector3 point)
	{
		if (m_character.IsFlying())
		{
			return true;
		}
		if (m_avoidWater && GetSolidHeight(point, out var height, 50f))
		{
			if (m_character.IsSwiming())
			{
				if (GetSolidHeight(base.transform.position, out var height2, 50f) && height < height2)
				{
					return false;
				}
			}
			else if (height < ZoneSystem.instance.m_waterLevel)
			{
				return false;
			}
		}
		if ((m_afraidOfFire || m_avoidFire) && (bool)EffectArea.IsPointInsideArea(point, EffectArea.Type.Fire))
		{
			return false;
		}
		return true;
	}

	protected virtual void OnDamaged(float damage, Character attacker)
	{
		m_timeSinceHurt = 0f;
	}

	protected virtual void OnDeath()
	{
		if (!string.IsNullOrEmpty(m_deathMessage))
		{
			MessageHud.instance.MessageAll(MessageHud.MessageType.Center, m_deathMessage);
		}
	}

	public bool CanSenseTarget(Character target)
	{
		if (CanHearTarget(target))
		{
			return true;
		}
		if (CanSeeTarget(target))
		{
			return true;
		}
		return false;
	}

	public bool CanHearTarget(Character target)
	{
		if (target.IsPlayer())
		{
			Player player = target as Player;
			if (player.InDebugFlyMode() || player.InGhostMode())
			{
				return false;
			}
		}
		float num = Vector3.Distance(target.transform.position, base.transform.position);
		float num2 = m_hearRange;
		if (m_character.InInterior())
		{
			num2 = Mathf.Min(8f, num2);
		}
		if (num > num2)
		{
			return false;
		}
		if (num < target.GetNoiseRange())
		{
			return true;
		}
		return false;
	}

	public bool CanSeeTarget(Character target)
	{
		if (target.IsPlayer())
		{
			Player player = target as Player;
			if (player.InDebugFlyMode() || player.InGhostMode())
			{
				return false;
			}
		}
		float num = Vector3.Distance(target.transform.position, base.transform.position);
		if (num > m_viewRange)
		{
			return false;
		}
		float factor = 1f - num / m_viewRange;
		float stealthFactor = target.GetStealthFactor();
		float num2 = m_viewRange * stealthFactor;
		if (num > num2)
		{
			target.OnStealthSuccess(m_character, factor);
			return false;
		}
		if (!IsAlerted() && Vector3.Angle(target.transform.position - m_character.transform.position, base.transform.forward) > m_viewAngle)
		{
			target.OnStealthSuccess(m_character, factor);
			return false;
		}
		Vector3 vector = (target.IsCrouching() ? target.GetCenterPoint() : target.m_eye.position) - m_character.m_eye.position;
		if (Physics.Raycast(m_character.m_eye.position, vector.normalized, vector.magnitude, m_viewBlockMask))
		{
			target.OnStealthSuccess(m_character, factor);
			return false;
		}
		return true;
	}

	public bool CanSeeTarget(StaticTarget target)
	{
		Vector3 center = target.GetCenter();
		if (Vector3.Distance(center, base.transform.position) > m_viewRange)
		{
			return false;
		}
		Vector3 rhs = center - m_character.m_eye.position;
		if (!IsAlerted() && Vector3.Dot(base.transform.forward, rhs) < 0f)
		{
			return false;
		}
		List<Collider> allColliders = target.GetAllColliders();
		int num = Physics.RaycastNonAlloc(m_character.m_eye.position, rhs.normalized, m_tempRaycastHits, rhs.magnitude, m_viewBlockMask);
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = m_tempRaycastHits[i];
			if (!allColliders.Contains(raycastHit.collider))
			{
				return false;
			}
		}
		return true;
	}

	protected void MoveTowardsSwoop(Vector3 dir, bool run, float distance)
	{
		dir = dir.normalized;
		float num = Mathf.Clamp01(Vector3.Dot(dir, m_character.transform.forward));
		num *= num;
		float num2 = Mathf.Clamp01(distance / m_serpentTurnRadius);
		float num3 = 1f - (1f - num2) * (1f - num);
		num3 = num3 * 0.9f + 0.1f;
		Vector3 moveDir = base.transform.forward * num3;
		LookTowards(dir);
		m_character.SetMoveDir(moveDir);
		m_character.SetRun(run);
	}

	public void MoveTowards(Vector3 dir, bool run)
	{
		dir = dir.normalized;
		LookTowards(dir);
		if (m_smoothMovement)
		{
			float num = Vector3.Angle(dir, base.transform.forward);
			float num2 = 1f - Mathf.Clamp01(num / m_moveMinAngle);
			Vector3 moveDir = base.transform.forward * num2;
			moveDir.y = dir.y;
			m_character.SetMoveDir(moveDir);
			m_character.SetRun(run);
			if (m_jumpInterval > 0f && m_jumpTimer >= m_jumpInterval)
			{
				m_jumpTimer = 0f;
				m_character.Jump();
			}
		}
		else if (IsLookingTowards(dir, m_moveMinAngle))
		{
			m_character.SetMoveDir(dir);
			m_character.SetRun(run);
			if (m_jumpInterval > 0f && m_jumpTimer >= m_jumpInterval)
			{
				m_jumpTimer = 0f;
				m_character.Jump();
			}
		}
		else
		{
			StopMoving();
		}
	}

	protected void LookAt(Vector3 point)
	{
		Vector3 vector = point - m_character.m_eye.position;
		if (!(Utils.LengthXZ(vector) < 0.01f))
		{
			vector.Normalize();
			LookTowards(vector);
		}
	}

	public void LookTowards(Vector3 dir)
	{
		m_character.SetLookDir(dir);
	}

	protected bool IsLookingAt(Vector3 point, float minAngle)
	{
		return IsLookingTowards((point - base.transform.position).normalized, minAngle);
	}

	public bool IsLookingTowards(Vector3 dir, float minAngle)
	{
		dir.y = 0f;
		Vector3 forward = base.transform.forward;
		forward.y = 0f;
		return Vector3.Angle(dir, forward) < minAngle;
	}

	public void StopMoving()
	{
		m_character.SetMoveDir(Vector3.zero);
	}

	protected bool HavePath(Vector3 target)
	{
		if (m_character.IsFlying())
		{
			return true;
		}
		return Pathfinding.instance.HavePath(base.transform.position, target, m_pathAgentType);
	}

	protected bool FindPath(Vector3 target)
	{
		float time = Time.time;
		float num = time - m_lastFindPathTime;
		if (num < 1f)
		{
			return m_lastFindPathResult;
		}
		if (Vector3.Distance(target, m_lastFindPathTarget) < 1f && num < 5f)
		{
			return m_lastFindPathResult;
		}
		m_lastFindPathTarget = target;
		m_lastFindPathTime = time;
		m_lastFindPathResult = Pathfinding.instance.GetPath(base.transform.position, target, m_path, m_pathAgentType);
		return m_lastFindPathResult;
	}

	protected bool FoundPath()
	{
		return m_lastFindPathResult;
	}

	protected bool MoveTo(float dt, Vector3 point, float dist, bool run)
	{
		if (m_character.m_flying)
		{
			dist = Mathf.Max(dist, 1f);
			if (ZoneSystem.instance.GetSolidHeight(point, out var height))
			{
				point.y = Mathf.Max(point.y, height + m_flyAltitudeMin);
			}
			return MoveAndAvoid(dt, point, dist, run);
		}
		float num = (run ? 1f : 0.5f);
		if (m_serpentMovement)
		{
			num = 3f;
		}
		if (Utils.DistanceXZ(point, base.transform.position) < Mathf.Max(dist, num))
		{
			StopMoving();
			return true;
		}
		if (!FindPath(point))
		{
			StopMoving();
			return true;
		}
		if (m_path.Count == 0)
		{
			StopMoving();
			return true;
		}
		Vector3 vector = m_path[0];
		if (Utils.DistanceXZ(vector, base.transform.position) < num)
		{
			m_path.RemoveAt(0);
			if (m_path.Count == 0)
			{
				StopMoving();
				return true;
			}
		}
		else if (m_serpentMovement)
		{
			float distance = Vector3.Distance(vector, base.transform.position);
			Vector3 normalized = (vector - base.transform.position).normalized;
			MoveTowardsSwoop(normalized, run, distance);
		}
		else
		{
			Vector3 normalized2 = (vector - base.transform.position).normalized;
			MoveTowards(normalized2, run);
		}
		return false;
	}

	protected bool MoveAndAvoid(float dt, Vector3 point, float dist, bool run)
	{
		Vector3 vector = point - base.transform.position;
		if (m_character.IsFlying())
		{
			if (vector.magnitude < dist)
			{
				StopMoving();
				return true;
			}
		}
		else
		{
			vector.y = 0f;
			if (vector.magnitude < dist)
			{
				StopMoving();
				return true;
			}
		}
		vector.Normalize();
		float radius = m_character.GetRadius();
		float num = radius + 1f;
		if (!m_character.InAttack())
		{
			m_getOutOfCornerTimer -= dt;
			if (m_getOutOfCornerTimer > 0f)
			{
				Vector3 dir = Quaternion.Euler(0f, m_getOutOfCornerAngle, 0f) * -vector;
				MoveTowards(dir, run);
				return false;
			}
			m_stuckTimer += Time.fixedDeltaTime;
			if (m_stuckTimer > 1.5f)
			{
				if (Vector3.Distance(base.transform.position, m_lastPosition) < 0.2f)
				{
					m_getOutOfCornerTimer = 4f;
					m_getOutOfCornerAngle = UnityEngine.Random.Range(-20f, 20f);
					m_stuckTimer = 0f;
					return false;
				}
				m_stuckTimer = 0f;
				m_lastPosition = base.transform.position;
			}
		}
		if (CanMove(vector, radius, num))
		{
			MoveTowards(vector, run);
		}
		else
		{
			Vector3 forward = base.transform.forward;
			if (m_character.IsFlying())
			{
				forward.y = 0.2f;
				forward.Normalize();
			}
			Vector3 vector2 = base.transform.right * radius * 0.75f;
			float num2 = num * 1.5f;
			Vector3 centerPoint = m_character.GetCenterPoint();
			float num3 = Raycast(centerPoint - vector2, forward, num2, 0.1f);
			float num4 = Raycast(centerPoint + vector2, forward, num2, 0.1f);
			if (num3 >= num2 && num4 >= num2)
			{
				MoveTowards(forward, run);
			}
			else
			{
				Vector3 dir2 = Quaternion.Euler(0f, -20f, 0f) * forward;
				Vector3 dir3 = Quaternion.Euler(0f, 20f, 0f) * forward;
				if (num3 > num4)
				{
					MoveTowards(dir2, run);
				}
				else
				{
					MoveTowards(dir3, run);
				}
			}
		}
		return false;
	}

	private bool CanMove(Vector3 dir, float checkRadius, float distance)
	{
		Vector3 centerPoint = m_character.GetCenterPoint();
		Vector3 right = base.transform.right;
		if (Raycast(centerPoint, dir, distance, 0.1f) < distance)
		{
			return false;
		}
		if (Raycast(centerPoint - right * (checkRadius - 0.1f), dir, distance, 0.1f) < distance)
		{
			return false;
		}
		if (Raycast(centerPoint + right * (checkRadius - 0.1f), dir, distance, 0.1f) < distance)
		{
			return false;
		}
		return true;
	}

	public float Raycast(Vector3 p, Vector3 dir, float distance, float radius)
	{
		if (radius == 0f)
		{
			if (Physics.Raycast(p, dir, out var hitInfo, distance, m_solidRayMask))
			{
				return hitInfo.distance;
			}
			return distance;
		}
		if (Physics.SphereCast(p, radius, dir, out var hitInfo2, distance, m_solidRayMask))
		{
			return hitInfo2.distance;
		}
		return distance;
	}

	public bool IsEnemey(Character other)
	{
		return IsEnemy(m_character, other);
	}

	public static bool IsEnemy(Character a, Character b)
	{
		if (a == b)
		{
			return false;
		}
		string group = a.GetGroup();
		if (group.Length > 0 && group == b.GetGroup())
		{
			return false;
		}
		Character.Faction faction = a.GetFaction();
		Character.Faction faction2 = b.GetFaction();
		bool flag = a.IsTamed();
		bool flag2 = b.IsTamed();
		if (flag || flag2)
		{
			if ((flag && flag2) || (flag && faction2 == Character.Faction.Players) || (flag2 && faction == Character.Faction.Players))
			{
				return false;
			}
			return true;
		}
		if (faction == faction2)
		{
			return false;
		}
		switch (faction)
		{
		case Character.Faction.AnimalsVeg:
			return true;
		case Character.Faction.Players:
			return true;
		case Character.Faction.ForestMonsters:
			if (faction2 != Character.Faction.AnimalsVeg)
			{
				return faction2 != Character.Faction.Boss;
			}
			return false;
		case Character.Faction.Undead:
			if (faction2 != Character.Faction.Demon)
			{
				return faction2 != Character.Faction.Boss;
			}
			return false;
		case Character.Faction.Demon:
			if (faction2 != Character.Faction.Undead)
			{
				return faction2 != Character.Faction.Boss;
			}
			return false;
		case Character.Faction.MountainMonsters:
			return faction2 != Character.Faction.Boss;
		case Character.Faction.SeaMonsters:
			return faction2 != Character.Faction.Boss;
		case Character.Faction.PlainsMonsters:
			return faction2 != Character.Faction.Boss;
		case Character.Faction.Boss:
			return faction2 == Character.Faction.Players;
		default:
			return false;
		}
	}

	protected StaticTarget FindRandomStaticTarget(float maxDistance)
	{
		float radius = m_character.GetRadius();
		Collider[] array = Physics.OverlapSphere(base.transform.position, radius + maxDistance, m_monsterTargetRayMask);
		if (array.Length == 0)
		{
			return null;
		}
		List<StaticTarget> list = new List<StaticTarget>();
		Collider[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			StaticTarget componentInParent = array2[i].GetComponentInParent<StaticTarget>();
			if (!(componentInParent == null) && componentInParent.IsRandomTarget() && CanSeeTarget(componentInParent))
			{
				list.Add(componentInParent);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	protected StaticTarget FindClosestStaticPriorityTarget()
	{
		Collider[] array = Physics.OverlapSphere(base.transform.position, m_viewRange, m_monsterTargetRayMask);
		if (array.Length == 0)
		{
			return null;
		}
		StaticTarget result = null;
		float num = m_viewRange;
		Collider[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			StaticTarget componentInParent = array2[i].GetComponentInParent<StaticTarget>();
			if (!(componentInParent == null) && componentInParent.IsPriorityTarget())
			{
				float num2 = Vector3.Distance(base.transform.position, componentInParent.GetCenter());
				if (num2 < num && CanSeeTarget(componentInParent))
				{
					result = componentInParent;
					num = num2;
				}
			}
		}
		return result;
	}

	protected void HaveFriendsInRange(float range, out Character hurtFriend, out Character friend)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		friend = HaveFriendInRange(allCharacters, range);
		hurtFriend = HaveHurtFriendInRange(allCharacters, range);
	}

	private Character HaveFriendInRange(List<Character> characters, float range)
	{
		foreach (Character character in characters)
		{
			if (!(character == m_character) && !IsEnemy(m_character, character) && !(Vector3.Distance(character.transform.position, base.transform.position) > range))
			{
				return character;
			}
		}
		return null;
	}

	protected Character HaveFriendInRange(float range)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		return HaveFriendInRange(allCharacters, range);
	}

	private Character HaveHurtFriendInRange(List<Character> characters, float range)
	{
		foreach (Character character in characters)
		{
			if (!IsEnemy(m_character, character) && !(Vector3.Distance(character.transform.position, base.transform.position) > range) && character.GetHealth() < character.GetMaxHealth())
			{
				return character;
			}
		}
		return null;
	}

	protected float StandStillDuration(float distanceTreshold)
	{
		if (Vector3.Distance(base.transform.position, m_lastMovementCheck) > distanceTreshold)
		{
			m_lastMovementCheck = base.transform.position;
			m_lastMoveTime = Time.time;
		}
		return Time.time - m_lastMoveTime;
	}

	protected Character HaveHurtFriendInRange(float range)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		return HaveHurtFriendInRange(allCharacters, range);
	}

	protected Character FindEnemy()
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		Character character = null;
		float num = 99999f;
		foreach (Character item in allCharacters)
		{
			if (!IsEnemy(m_character, item) || item.IsDead())
			{
				continue;
			}
			BaseAI baseAI = item.GetBaseAI();
			if ((!(baseAI != null) || !baseAI.IsSleeping()) && CanSenseTarget(item))
			{
				float num2 = Vector3.Distance(item.transform.position, base.transform.position);
				if (num2 < num || character == null)
				{
					character = item;
					num = num2;
				}
			}
		}
		if (character == null && HuntPlayer())
		{
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 200f);
			if ((bool)closestPlayer && (closestPlayer.InDebugFlyMode() || closestPlayer.InGhostMode()))
			{
				return null;
			}
			return closestPlayer;
		}
		return character;
	}

	public void SetHuntPlayer(bool hunt)
	{
		if (m_huntPlayer != hunt)
		{
			m_huntPlayer = hunt;
			if (m_nview.IsOwner())
			{
				m_nview.GetZDO().Set("huntplayer", m_huntPlayer);
			}
		}
	}

	public virtual bool HuntPlayer()
	{
		return m_huntPlayer;
	}

	protected bool HaveAlertedCreatureInRange(float range)
	{
		foreach (BaseAI instance in m_instances)
		{
			if (Vector3.Distance(base.transform.position, instance.transform.position) < range && instance.IsAlerted())
			{
				return true;
			}
		}
		return false;
	}

	public static void DoProjectileHitNoise(Vector3 center, float range, Character attacker)
	{
		foreach (BaseAI instance in m_instances)
		{
			if ((!attacker || instance.IsEnemey(attacker)) && Vector3.Distance(instance.transform.position, center) < range && (bool)instance.m_nview && instance.m_nview.IsValid())
			{
				instance.m_nview.InvokeRPC("OnNearProjectileHit", center, range, attacker ? attacker.GetZDOID() : ZDOID.None);
			}
		}
	}

	protected virtual void RPC_OnNearProjectileHit(long sender, Vector3 center, float range, ZDOID attacker)
	{
		Alert();
	}

	public void Alert()
	{
		if (m_nview.IsValid() && !IsAlerted())
		{
			if (m_nview.IsOwner())
			{
				SetAlerted(alert: true);
			}
			else
			{
				m_nview.InvokeRPC("Alert");
			}
		}
	}

	private void RPC_Alert(long sender)
	{
		if (m_nview.IsOwner())
		{
			SetAlerted(alert: true);
		}
	}

	protected virtual void SetAlerted(bool alert)
	{
		if (m_alerted != alert)
		{
			m_alerted = alert;
			m_animator.SetBool("alert", m_alerted);
			if (m_nview.IsOwner())
			{
				m_nview.GetZDO().Set("alert", m_alerted);
			}
			if (m_alerted)
			{
				m_alertedEffects.Create(base.transform.position, Quaternion.identity);
			}
		}
	}

	public static bool InStealthRange(Character me)
	{
		bool result = false;
		foreach (BaseAI allInstance in GetAllInstances())
		{
			if (!IsEnemy(me, allInstance.m_character))
			{
				continue;
			}
			float num = Vector3.Distance(me.transform.position, allInstance.transform.position);
			if (num < allInstance.m_viewRange || num < 10f)
			{
				if (allInstance.IsAlerted())
				{
					return false;
				}
				result = true;
			}
		}
		return result;
	}

	public static Character FindClosestEnemy(Character me, Vector3 point, float maxDistance)
	{
		Character character = null;
		float num = maxDistance;
		foreach (Character allCharacter in Character.GetAllCharacters())
		{
			if (IsEnemy(me, allCharacter))
			{
				float num2 = Vector3.Distance(allCharacter.transform.position, point);
				if (character == null || num2 < num)
				{
					character = allCharacter;
					num = num2;
				}
			}
		}
		return character;
	}

	public static Character FindRandomEnemy(Character me, Vector3 point, float maxDistance)
	{
		List<Character> list = new List<Character>();
		foreach (Character allCharacter in Character.GetAllCharacters())
		{
			if (IsEnemy(me, allCharacter) && Vector3.Distance(allCharacter.transform.position, point) < maxDistance)
			{
				list.Add(allCharacter);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	public bool IsAlerted()
	{
		return m_alerted;
	}

	protected void SetTargetInfo(ZDOID targetID)
	{
		m_nview.GetZDO().Set(havetTargetHash, !targetID.IsNone());
	}

	public bool HaveTarget()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		return m_nview.GetZDO().GetBool(havetTargetHash);
	}

	protected float GetAltitude()
	{
		float groundHeight = ZoneSystem.instance.GetGroundHeight(m_character.transform.position);
		return m_character.transform.position.y - groundHeight;
	}

	public static List<BaseAI> GetAllInstances()
	{
		return m_instances;
	}

	protected virtual void OnDrawGizmosSelected()
	{
		if (m_lastFindPathResult)
		{
			Gizmos.color = Color.yellow;
			for (int i = 0; i < m_path.Count - 1; i++)
			{
				Vector3 vector = m_path[i];
				Gizmos.DrawLine(to: m_path[i + 1] + Vector3.up * 0.1f, from: vector + Vector3.up * 0.1f);
			}
			Gizmos.color = Color.cyan;
			foreach (Vector3 item in m_path)
			{
				Gizmos.DrawSphere(item + Vector3.up * 0.1f, 0.1f);
			}
			Gizmos.color = Color.green;
			Gizmos.DrawLine(base.transform.position, m_lastFindPathTarget);
			Gizmos.DrawSphere(m_lastFindPathTarget, 0.2f);
		}
		else
		{
			Gizmos.color = Color.red;
			Gizmos.DrawLine(base.transform.position, m_lastFindPathTarget);
			Gizmos.DrawSphere(m_lastFindPathTarget, 0.2f);
		}
	}

	public virtual bool IsSleeping()
	{
		return false;
	}

	public bool HasZDOOwner()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		return m_nview.GetZDO().HasOwner();
	}

	public static bool CanUseAttack(Character character, ItemDrop.ItemData item)
	{
		bool flag = character.IsFlying();
		bool flag2 = character.IsSwiming();
		if (item.m_shared.m_aiWhenFlying && flag)
		{
			return true;
		}
		if (item.m_shared.m_aiWhenWalking && !flag && !flag2)
		{
			return true;
		}
		if (item.m_shared.m_aiWhenSwiming && flag2)
		{
			return true;
		}
		return false;
	}

	public virtual Character GetTargetCreature()
	{
		return null;
	}

	public bool HaveRider()
	{
		if ((bool)m_tamable)
		{
			return m_tamable.HaveRider();
		}
		return false;
	}

	public float GetRiderSkill()
	{
		if ((bool)m_tamable)
		{
			return m_tamable.GetRiderSkill();
		}
		return 0f;
	}
}
