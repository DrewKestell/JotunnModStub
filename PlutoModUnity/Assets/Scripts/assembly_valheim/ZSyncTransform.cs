using System.Collections.Generic;
using UnityEngine;

public class ZSyncTransform : MonoBehaviour
{
	public bool m_syncPosition = true;

	public bool m_syncRotation = true;

	public bool m_syncScale;

	public bool m_syncBodyVelocity;

	public bool m_characterParentSync;

	private const float m_smoothness = 0.2f;

	private bool m_isKinematicBody;

	private bool m_useGravity = true;

	private Vector3 m_tempRelPos;

	private bool m_haveTempRelPos;

	private float m_targetPosTimer;

	private uint m_posRevision;

	private int m_lastUpdateFrame = -1;

	private static int m_velHash = "vel".GetStableHashCode();

	private static int m_scaleHash = "scale".GetStableHashCode();

	private static int m_bodyVelHash = "body_vel".GetStableHashCode();

	private static int m_bodyAVelHash = "body_avel".GetStableHashCode();

	private static int m_relPosHash = "relPos".GetStableHashCode();

	private static int m_attachJointHash = "attachJoint".GetStableHashCode();

	private static KeyValuePair<int, int> m_parentIDHash = ZDO.GetHashZDOID("parentID");

	private ZNetView m_nview;

	private Rigidbody m_body;

	private Projectile m_projectile;

	private Character m_character;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
		m_projectile = GetComponent<Projectile>();
		m_character = GetComponent<Character>();
		if (m_nview.GetZDO() == null)
		{
			base.enabled = false;
		}
		else if ((bool)m_body)
		{
			m_isKinematicBody = m_body.isKinematic;
			m_useGravity = m_body.useGravity;
		}
	}

	private Vector3 GetVelocity()
	{
		if (m_body != null)
		{
			return m_body.velocity;
		}
		if (m_projectile != null)
		{
			return m_projectile.GetVelocity();
		}
		return Vector3.zero;
	}

	private Vector3 GetPosition()
	{
		if (!m_body)
		{
			return base.transform.position;
		}
		return m_body.position;
	}

	private void OwnerSync()
	{
		ZDO zDO = m_nview.GetZDO();
		if (!zDO.IsOwner())
		{
			return;
		}
		if (base.transform.position.y < -5000f)
		{
			if ((bool)m_body)
			{
				m_body.velocity = Vector3.zero;
			}
			ZLog.Log("Object fell out of world:" + base.gameObject.name);
			float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
			Vector3 position = base.transform.position;
			position.y = groundHeight + 1f;
			base.transform.position = position;
			return;
		}
		if (m_syncPosition)
		{
			zDO.SetPosition(GetPosition());
			zDO.Set(m_velHash, GetVelocity());
			if (m_characterParentSync)
			{
				if (m_character.GetRelativePosition(out var parent, out var attachJoint, out var relativePos, out var relativeVel))
				{
					zDO.Set(m_parentIDHash, parent);
					zDO.Set(m_attachJointHash, attachJoint);
					zDO.Set(m_relPosHash, relativePos);
					zDO.Set(m_velHash, relativeVel);
				}
				else
				{
					zDO.Set(m_parentIDHash, ZDOID.None);
					zDO.Set(m_attachJointHash, "");
				}
			}
		}
		if (m_syncRotation && base.transform.hasChanged)
		{
			Quaternion rotation = (m_body ? m_body.rotation : base.transform.rotation);
			zDO.SetRotation(rotation);
		}
		if (m_syncScale && base.transform.hasChanged)
		{
			zDO.Set(m_scaleHash, base.transform.localScale);
		}
		if ((bool)m_body)
		{
			if (m_syncBodyVelocity)
			{
				m_nview.GetZDO().Set(m_bodyVelHash, m_body.velocity);
				m_nview.GetZDO().Set(m_bodyAVelHash, m_body.angularVelocity);
			}
			m_body.useGravity = m_useGravity;
		}
		base.transform.hasChanged = false;
	}

	private void SyncPosition(ZDO zdo, float dt)
	{
		if (m_characterParentSync && zdo.HasOwner())
		{
			ZDOID zDOID = zdo.GetZDOID(m_parentIDHash);
			if (!zDOID.IsNone())
			{
				GameObject gameObject = ZNetScene.instance.FindInstance(zDOID);
				if ((bool)gameObject)
				{
					ZSyncTransform component = gameObject.GetComponent<ZSyncTransform>();
					if ((bool)component)
					{
						component.ClientSync(dt);
					}
					string @string = zdo.GetString(m_attachJointHash);
					Vector3 vector = zdo.GetVec3(m_relPosHash, Vector3.zero);
					Vector3 vec = zdo.GetVec3(m_velHash, Vector3.zero);
					if (zdo.m_dataRevision != m_posRevision)
					{
						m_posRevision = zdo.m_dataRevision;
						m_targetPosTimer = 0f;
					}
					if (@string.Length > 0)
					{
						Transform transform = Utils.FindChild(gameObject.transform, @string);
						if ((bool)transform)
						{
							base.transform.position = transform.position;
						}
						return;
					}
					m_targetPosTimer += dt;
					m_targetPosTimer = Mathf.Min(m_targetPosTimer, 2f);
					vector += vec * m_targetPosTimer;
					if (!m_haveTempRelPos)
					{
						m_haveTempRelPos = true;
						m_tempRelPos = vector;
					}
					if (Vector3.Distance(m_tempRelPos, vector) > 0.001f)
					{
						m_tempRelPos = Vector3.Lerp(m_tempRelPos, vector, 0.2f);
						vector = m_tempRelPos;
					}
					Vector3 vector2 = gameObject.transform.TransformPoint(vector);
					if (Vector3.Distance(base.transform.position, vector2) > 0.001f)
					{
						base.transform.position = vector2;
					}
					return;
				}
			}
		}
		m_haveTempRelPos = false;
		Vector3 position = zdo.GetPosition();
		if (zdo.m_dataRevision != m_posRevision)
		{
			m_posRevision = zdo.m_dataRevision;
			m_targetPosTimer = 0f;
		}
		if (zdo.HasOwner())
		{
			m_targetPosTimer += dt;
			m_targetPosTimer = Mathf.Min(m_targetPosTimer, 2f);
			Vector3 vec2 = zdo.GetVec3(m_velHash, Vector3.zero);
			position += vec2 * m_targetPosTimer;
		}
		float num = Vector3.Distance(base.transform.position, position);
		if (num > 0.001f)
		{
			base.transform.position = ((num < 5f) ? Vector3.Lerp(base.transform.position, position, 0.2f) : position);
		}
	}

	private void ClientSync(float dt)
	{
		ZDO zDO = m_nview.GetZDO();
		if (zDO.IsOwner())
		{
			return;
		}
		int frameCount = Time.frameCount;
		if (m_lastUpdateFrame == frameCount)
		{
			return;
		}
		m_lastUpdateFrame = frameCount;
		if (m_isKinematicBody)
		{
			if (m_syncPosition)
			{
				Vector3 vector = zDO.GetPosition();
				if (Vector3.Distance(m_body.position, vector) > 5f)
				{
					m_body.position = vector;
				}
				else
				{
					if (Vector3.Distance(m_body.position, vector) > 0.01f)
					{
						vector = Vector3.Lerp(m_body.position, vector, 0.2f);
					}
					m_body.MovePosition(vector);
				}
			}
			if (m_syncRotation)
			{
				Quaternion rotation = zDO.GetRotation();
				if (Quaternion.Angle(m_body.rotation, rotation) > 45f)
				{
					m_body.rotation = rotation;
				}
				else
				{
					m_body.MoveRotation(rotation);
				}
			}
		}
		else
		{
			if (m_syncPosition)
			{
				SyncPosition(zDO, dt);
			}
			if (m_syncRotation)
			{
				Quaternion rotation2 = zDO.GetRotation();
				if (Quaternion.Angle(base.transform.rotation, rotation2) > 0.001f)
				{
					base.transform.rotation = Quaternion.Slerp(base.transform.rotation, rotation2, 0.2f);
				}
			}
			if ((bool)m_body)
			{
				m_body.useGravity = false;
				if (m_syncBodyVelocity && m_nview.HasOwner())
				{
					Vector3 vec = zDO.GetVec3(m_bodyVelHash, Vector3.zero);
					Vector3 vec2 = zDO.GetVec3(m_bodyAVelHash, Vector3.zero);
					if (vec.magnitude > 0.01f || vec2.magnitude > 0.01f)
					{
						m_body.velocity = vec;
						m_body.angularVelocity = vec2;
					}
					else
					{
						m_body.Sleep();
					}
				}
				else if (!m_body.IsSleeping())
				{
					m_body.velocity = Vector3.zero;
					m_body.angularVelocity = Vector3.zero;
					m_body.Sleep();
				}
			}
		}
		if (m_syncScale)
		{
			Vector3 vec3 = zDO.GetVec3(m_scaleHash, base.transform.localScale);
			base.transform.localScale = vec3;
		}
	}

	private void FixedUpdate()
	{
		if (m_nview.IsValid())
		{
			ClientSync(Time.fixedDeltaTime);
		}
	}

	private void LateUpdate()
	{
		if (m_nview.IsValid())
		{
			OwnerSync();
		}
	}

	public void SyncNow()
	{
		if (m_nview.IsValid())
		{
			OwnerSync();
		}
	}
}
