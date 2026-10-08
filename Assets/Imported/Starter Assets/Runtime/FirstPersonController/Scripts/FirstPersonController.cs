using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
	[RequireComponent(typeof(PlayerInput))]
#endif
	public class FirstPersonController : MonoBehaviour
	{
		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		public float MoveSpeed = 4.0f;
		[Tooltip("Sprint speed of the character in m/s")]
		public float SprintSpeed = 6.0f;
		[Tooltip("Rotation speed of the character")]
		public float RotationSpeed = 1.0f;
		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;

		[Space(10)]
		[Tooltip("The height the player can jump")]
		public float JumpHeight = 1.2f;
		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		public float Gravity = -15.0f;

		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		public float JumpTimeout = 0.1f;
		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		public float FallTimeout = 0.15f;

		[Header("Player Grounded")]
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		public bool Grounded = true;
		[Tooltip("Useful for rough ground")]
		public float GroundedOffset = -0.14f;
		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float GroundedRadius = 0.5f;
		[Tooltip("What layers the character uses as ground")]
		public LayerMask GroundLayers;

		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject CinemachineCameraTarget;
		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -90.0f;
		[Tooltip("Camera offset from the head bone or eye position")]
		public Vector3 CameraOffset = new Vector3(0.0f, 0.05f, 0.12f);

		[Header("Audio")]
		public AudioClip LandingAudioClip;
		public AudioClip[] FootstepAudioClips;
		[Range(0, 1)] public float FootstepAudioVolume = 0.5f;

		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _animationBlend;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;

		// animation IDs
		private int _animIDSpeed;
		private int _animIDGrounded;
		private int _animIDJump;
		private int _animIDFreeFall;
		private int _animIDMotionSpeed;

		private Animator _animator;
		private bool _hasAnimator;
		private Transform _headBone;
		private AudioSource _audioSource;

		// Footstep & Landing tracking
		private bool _previouslyGrounded = true;
		private float _airborneTimer;
		private float _stepDistanceCounter;
		private float _lastFootstepTime;
		private const float MinFootstepInterval = 0.22f;

#if ENABLE_INPUT_SYSTEM
		private PlayerInput _playerInput;
#endif
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		private GameObject _mainCamera;

		private const float _threshold = 0.01f;

		private bool IsCurrentDeviceMouse
		{
			get
			{
				#if ENABLE_INPUT_SYSTEM
				return _playerInput.currentControlScheme == "KeyboardMouse";
				#else
				return false;
				#endif
			}
		}

		private void Awake()
		{
			// get a reference to our main camera
			if (_mainCamera == null)
			{
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}

			if (!TryGetComponent(out _audioSource))
			{
				_audioSource = gameObject.AddComponent<AudioSource>();
			}
			_audioSource.spatialBlend = 0.15f;
			_audioSource.playOnAwake = false;
		}

		private void Start()
		{
			_hasAnimator = TryGetComponent(out _animator);
			if (_hasAnimator && _animator != null)
			{
				_animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
				if (_animator.isHuman)
				{
					_headBone = _animator.GetBoneTransform(HumanBodyBones.Head);
				}
			}
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
			_playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

			AssignAnimationIDs();

			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;

			// Apply sensitivity from PlayerPrefs if saved
			if (PlayerPrefs.HasKey("XSensitivity"))
			{
				RotationSpeed = PlayerPrefs.GetFloat("XSensitivity");
			}
		}

		private void AssignAnimationIDs()
		{
			_animIDSpeed = Animator.StringToHash("Speed");
			_animIDGrounded = Animator.StringToHash("Grounded");
			_animIDJump = Animator.StringToHash("Jump");
			_animIDFreeFall = Animator.StringToHash("FreeFall");
			_animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
		}

		private void Update()
		{
			_hasAnimator = TryGetComponent(out _animator);

			GroundedCheck();
			JumpAndGravity();
			Move();
		}

		private void LateUpdate()
		{
			CameraRotation();
		}

		private readonly Collider[] _groundHits = new Collider[8];

		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			
			// Use OverlapSphereNonAlloc to exclude the player's own CharacterController / colliders
			int hitCount = Physics.OverlapSphereNonAlloc(spherePosition, GroundedRadius, _groundHits, GroundLayers, QueryTriggerInteraction.Ignore);
			
			bool isGrounded = false;
			for (int i = 0; i < hitCount; i++)
			{
				if (_groundHits[i] != null && _groundHits[i].gameObject != gameObject && !_groundHits[i].transform.IsChildOf(transform))
				{
					isGrounded = true;
					break;
				}
			}

			bool wasGrounded = Grounded;
			Grounded = isGrounded;

			if (!wasGrounded && Grounded)
			{
				if (_airborneTimer > 0.15f)
				{
					PlayLandingSound();
				}
				_airborneTimer = 0f;
			}
			else if (!Grounded)
			{
				_airborneTimer += Time.deltaTime;
			}

			if (_hasAnimator)
			{
				_animator.SetBool(_animIDGrounded, Grounded);
			}
		}

		private void CameraRotation()
		{
			if (Time.timeScale == 0f) return;

			// if there is an input
			if (_input.look.sqrMagnitude >= _threshold)
			{
				//Don't multiply mouse input by Time.deltaTime
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
				float invertMultiplier = PlayerPrefs.GetInt("Inverted", 0) == 1 ? -1.0f : 1.0f;
				
				_cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier * invertMultiplier;
				_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

				// clamp our pitch rotation
				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

				// rotate the player left and right
				transform.Rotate(Vector3.up * _rotationVelocity);
			}

			// Follow the animated head bone or body position
			if (CinemachineCameraTarget != null)
			{
				if (_headBone != null)
				{
					CinemachineCameraTarget.transform.position = _headBone.position + transform.TransformDirection(CameraOffset);
				}
				CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch, transform.eulerAngles.y, 0.0f);
			}
		}

		private void Move()
		{
			if (Time.timeScale == 0f) return;

			// set target speed based on move speed, sprint speed and if sprint is pressed
			float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

			// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

			// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is no input, set the target speed to 0
			if (_input.move == Vector2.zero) targetSpeed = 0.0f;

			// a reference to the players current horizontal velocity
			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

			float speedOffset = 0.1f;
			float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

			// accelerate or decelerate to target speed
			if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				// creates curved result rather than a linear one giving a more organic speed change
				// note T in Lerp is clamped, so we don't need to clamp our speed
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

				// round speed to 3 decimal places
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}

			_animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
			if (_animationBlend < 0.01f) _animationBlend = 0f;

			// normalise input direction
			Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

			// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is a move input rotate player when the player is moving
			if (_input.move != Vector2.zero)
			{
				// move
				inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
			}

			// move the player
			_controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

			// Footstep stride tracking
			if (Grounded && currentHorizontalSpeed > 0.2f && _input.move != Vector2.zero)
			{
				float stepStride = _input.sprint ? 1.8f : 1.35f;
				_stepDistanceCounter += currentHorizontalSpeed * Time.deltaTime;
				if (_stepDistanceCounter >= stepStride)
				{
					_stepDistanceCounter = 0f;
					PlayFootstepSound();
				}
			}
			else
			{
				_stepDistanceCounter = 0f;
			}

			if (_hasAnimator)
			{
				_animator.SetFloat(_animIDSpeed, _animationBlend);
				_animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
			}
		}

		private void JumpAndGravity()
		{
			if (Grounded)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = FallTimeout;

				if (_hasAnimator)
				{
					_animator.SetBool(_animIDJump, false);
					_animator.SetBool(_animIDFreeFall, false);
				}

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
				{
					_verticalVelocity = -2f;
				}

				// Jump
				if (_input.jump && _jumpTimeoutDelta <= 0.0f)
				{
					// the square root of H * -2 * G = how much velocity needed to reach desired height
					_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

					if (_hasAnimator)
					{
						_animator.SetBool(_animIDJump, true);
					}

					_input.jump = false;
				}

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f)
				{
					_jumpTimeoutDelta -= Time.deltaTime;
				}
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
				{
					_fallTimeoutDelta -= Time.deltaTime;
				}
				else
				{
					if (_hasAnimator)
					{
						_animator.SetBool(_animIDFreeFall, true);
					}
				}

				// if we are not grounded, do not jump
				_input.jump = false;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity)
			{
				_verticalVelocity += Gravity * Time.deltaTime;
			}
		}

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}

		private void PlayFootstepSound()
		{
			if (Time.time - _lastFootstepTime < MinFootstepInterval) return;
			_lastFootstepTime = Time.time;

			if (FootstepAudioClips != null && FootstepAudioClips.Length > 0)
			{
				var index = Random.Range(0, FootstepAudioClips.Length);
				var clip = FootstepAudioClips[index];
				if (clip != null)
				{
					if (_audioSource != null)
					{
						_audioSource.pitch = Random.Range(0.92f, 1.08f);
						_audioSource.PlayOneShot(clip, FootstepAudioVolume);
					}
					else
					{
						AudioSource.PlayClipAtPoint(clip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
					}
				}
			}
		}

		private void PlayLandingSound()
		{
			if (LandingAudioClip != null)
			{
				if (_audioSource != null)
				{
					_audioSource.pitch = Random.Range(0.95f, 1.05f);
					_audioSource.PlayOneShot(LandingAudioClip, FootstepAudioVolume);
				}
				else
				{
					AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
				}
			}
		}

		private void OnFootstep(AnimationEvent animationEvent)
		{
			if (animationEvent.animatorClipInfo.weight > 0.15f)
			{
				PlayFootstepSound();
			}
		}

		private void OnLand(AnimationEvent animationEvent)
		{
			PlayLandingSound();
		}
	}
}