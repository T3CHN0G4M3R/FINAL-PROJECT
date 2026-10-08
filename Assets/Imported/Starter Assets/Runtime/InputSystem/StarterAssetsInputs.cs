using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}
		
		private void Start()
		{
			SetCursorState(cursorLocked);
			StartCoroutine(LockCursorRoutine());
		}

		private void OnEnable()
		{
			SetCursorState(cursorLocked);
		}

		private System.Collections.IEnumerator LockCursorRoutine()
		{
			yield return null;
			if (cursorLocked && Time.timeScale > 0f)
			{
				SetCursorState(true);
			}
			yield return new WaitForSecondsRealtime(0.1f);
			if (cursorLocked && Time.timeScale > 0f)
			{
				SetCursorState(true);
			}
		}

		private void Update()
		{
			if (cursorLocked && Time.timeScale > 0f && Cursor.lockState != CursorLockMode.Locked)
			{
#if ENABLE_INPUT_SYSTEM
				bool mouseClick = (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame));
#else
				bool mouseClick = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
#endif
				if (mouseClick || Input.GetMouseButtonDown(0))
				{
					SetCursorState(true);
				}
			}
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		public void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !newState;
		}
	}
	
}