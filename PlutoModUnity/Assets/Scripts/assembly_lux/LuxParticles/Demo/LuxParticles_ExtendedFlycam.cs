using UnityEngine;

namespace LuxParticles.Demo
{
	public class LuxParticles_ExtendedFlycam : MonoBehaviour
	{
		public float cameraSensitivity = 90f;

		public float climbSpeed = 4f;

		public float normalMoveSpeed = 10f;

		public float slowMoveFactor = 0.25f;

		public float fastMoveFactor = 3f;

		private float rotationX;

		private float rotationY;

		private bool isOrtho;

		private Camera cam;

		private void Start()
		{
			rotationX = base.transform.eulerAngles.y;
			cam = GetComponent<Camera>();
			if (cam != null)
			{
				isOrtho = cam.orthographic;
			}
		}

		private void Update()
		{
			float deltaTime = Time.deltaTime;
			rotationX += Input.GetAxis("Mouse X") * cameraSensitivity * deltaTime;
			rotationY += Input.GetAxis("Mouse Y") * cameraSensitivity * deltaTime;
			rotationY = Mathf.Clamp(rotationY, -90f, 90f);
			Quaternion b = Quaternion.AngleAxis(rotationX, Vector3.up);
			b *= Quaternion.AngleAxis(rotationY, Vector3.left);
			base.transform.localRotation = Quaternion.Slerp(base.transform.localRotation, b, deltaTime * 6f);
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				base.transform.position += base.transform.forward * (normalMoveSpeed * fastMoveFactor) * Input.GetAxis("Vertical") * deltaTime;
				base.transform.position += base.transform.right * (normalMoveSpeed * fastMoveFactor) * Input.GetAxis("Horizontal") * deltaTime;
			}
			else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
			{
				base.transform.position += base.transform.forward * (normalMoveSpeed * slowMoveFactor) * Input.GetAxis("Vertical") * deltaTime;
				base.transform.position += base.transform.right * (normalMoveSpeed * slowMoveFactor) * Input.GetAxis("Horizontal") * deltaTime;
			}
			else
			{
				if (isOrtho)
				{
					cam.orthographicSize *= 1f - Input.GetAxis("Vertical") * deltaTime;
				}
				else
				{
					base.transform.position += base.transform.forward * normalMoveSpeed * Input.GetAxis("Vertical") * deltaTime;
				}
				base.transform.position += base.transform.right * normalMoveSpeed * Input.GetAxis("Horizontal") * deltaTime;
			}
			if (Input.GetKey(KeyCode.Q))
			{
				base.transform.position -= base.transform.up * climbSpeed * deltaTime;
			}
			if (Input.GetKey(KeyCode.E))
			{
				base.transform.position += base.transform.up * climbSpeed * deltaTime;
			}
		}
	}
}
