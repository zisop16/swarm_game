using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class Player : RigidBody3D
{
	public Camera3D Cam;
	public AnimationPlayer AnimPlayer;
	RayCast3D GroundRay;
	RayCast3D InteractionRay;
	Node3D Model;
	Node3D WeaponHolder;
	Dictionary<Weapon.ID, Node3D> Attachments;
	void SetChildVariables() {
		Cam = GetNode<Camera3D>("Camera");
		GroundRay = GetNode<RayCast3D>("GroundRay");
		InteractionRay = GetNode<RayCast3D>("%InteractionRay");
		Model = GetNode<Node3D>("Model");
		AnimPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		WeaponHolder = GetNode<Node3D>("Camera/WeaponHolder");
		Attachments = new Dictionary<Weapon.ID, Node3D>() {
			{ Weapon.ID.AXE, GetNode<Node3D>("Camera/WeaponHolder/Axe") },
			{ Weapon.ID.WIND_SWORD, GetNode<Node3D>("Camera/WeaponHolder/WindSword") }
		};
	}
	float SpeedControl = 25;
	float JumpSpeed = 8;
	float MaxGroundSpeed = 10;
	float MaxAirSpeed = 20;
	float BaseControl = .3f;
	int EquippedSlot = 0;
	static int InventorySize = 3;
	Weapon[] Weapons = new Weapon[InventorySize];
	public override void _Ready() {
		Global.Player = this;
		SetChildVariables();
		CollisionLayer = Global.PLAYER_LAYER;
		CollisionMask = Global.MAP_LAYER;
	}

	public Weapon EquippedWeapon { get => Weapons[EquippedSlot]; }
	public Vector3 LookDirection() {
		return -Cam.GlobalTransform.Basis.Z;
	}
	Vector3 MovementForceDirec;
	Vector3 CalculateMovementForce() {
		Vector2 movementDirec = Input.GetVector("Left", "Right", "Back", "Forward");
		Basis camBasis = Cam.GlobalTransform.Basis;
		Vector3 camForward = -camBasis.Z.Slide(Vector3.Up);
		float rotAngle = Vector3.ModelFront.SignedAngleTo(camForward, Vector3.Up);
		MovementForceDirec = (movementDirec.X * Vector3.ModelRight + movementDirec.Y * Vector3.ModelFront).Normalized();
		MovementForceDirec = MovementForceDirec.Rotated(Vector3.Up, rotAngle);
		float targetSpeed = OnFloor() ? MaxGroundSpeed : MaxAirSpeed;
		Vector3 targetHorizVelocity = targetSpeed * MovementForceDirec;
		Vector3 currHorizVelocity = LinearVelocity.Slide(Vector3.Up);
		Vector3 velocityDiff = targetHorizVelocity - currHorizVelocity;
		float velAdjustmentMagnitude = velocityDiff.Length() / targetSpeed;
		float movementAccel = (velAdjustmentMagnitude + BaseControl) * SpeedControl;
		Vector3 movementForce = velocityDiff.Normalized() * movementAccel * Mass;
		return movementForce;
	}

	double CoyoteTime = .17;
	double LastTimeOnFloor = float.NegativeInfinity;
	const float JUMP_COOLDOWN = .1f;
	double LastTimeJumped = 0;
	bool UsedJump = false;
	void HandleJump() {
		if (JumpOnCooldown())
		{
			return;
		}
		if (OnFloor()) {
			LastTimeOnFloor = Global.Time;
		}
		else {
			if (!CanCoyoteJump()) {
				return;
			}
		}
		if (UsedJump)
		{
			if (OnFloor())
			{
				UsedJump = false;
			}
			else
			{
				return;
			}
		}
		var jumpInput = Input.IsActionJustPressed("Jump");
		if (!jumpInput)
		{
			return;
		}
		UsedJump = true;
		LastTimeJumped = Global.Time;
		var jumpImpulse = Vector3.Up * JumpSpeed * Mass;
		if (LinearVelocity.Y < 0) {
			LinearVelocity = LinearVelocity.Slide(Vector3.Up);
		}
		ApplyImpulse(jumpImpulse);
	}

	bool JumpOnCooldown() {
		return (Global.Time - LastTimeJumped) < JUMP_COOLDOWN;
	}
	bool CanCoyoteJump() {
		return (Global.Time - LastTimeOnFloor) < CoyoteTime;
	}
	bool OnFloor() {
		return GroundRay.IsColliding();
	}
	void HandleFastfall() {
		float fastFallScale = .4f;
		if (LinearVelocity.Y < -1) {
			ApplyCentralForce(fastFallScale * Mass * GetGravity());
		}
	}

	Vector3 DetermineGroundFriction() {
		float fricExponent = .3f;
		float fricStrength = 3;
		float movingFricMultiplier = .1f;
		float actualFricStrength = fricStrength * (MovementForceDirec == Vector3.Zero ? 1f : movingFricMultiplier);
		float fricMagnitude = Mathf.Pow(LinearVelocity.LengthSquared(), (fricExponent / 2)) * actualFricStrength * Mass;
		return fricMagnitude * -LinearVelocity.Normalized();
	}
	Vector3 DetermineAirFriction() {
		float fricExponent = 1.2f;
		float fricStrength = .004f;
		float fricMagnitude = Mathf.Pow(LinearVelocity.LengthSquared(), (fricExponent / 2f)) * Mass * fricStrength;
		return fricMagnitude * -LinearVelocity.Normalized();
	}

	void HandleFriction() {
		Vector3 friction;
		if (OnFloor()) {
			friction = DetermineGroundFriction();
		}
		else {
			friction = DetermineAirFriction();
		}
		ApplyCentralForce(friction);
	}
	void HandleGroundSnap() {
		if (UsedJump) {
			return;
		}
		if (!GroundRay.IsColliding()) {
			return;
		}
		Vector3 snapVector = new Vector3(0, GlobalPosition.Y - GroundRay.GetCollisionPoint().Y, 0) * .5f;
		GlobalPosition -= snapVector;
	}

	void DetermineInteractionTarget() {
		if (!InteractionRay.IsColliding()) {
			Global.InteractionTarget = null;
		}
		GodotObject target = InteractionRay.GetCollider();
		if (target is Weapon) {
			Global.InteractionTarget = target as Weapon;
		}
		else {
			Global.InteractionTarget = null;
		}
	}

	void HandleInteraction() {
		if (Global.InteractionTarget == null) {
			return;
		}
		if (Input.IsActionJustPressed("Equip")) {
			EquipWeapon(Global.InteractionTarget);
		}
	}

	void HandleWeaponInputs() {
		if (EquippedWeapon == null) {
			return;
		}
		bool shouldAttack = Input.IsActionJustPressed("Attack");
		if (shouldAttack) {
			EquippedWeapon.Attack();
		}
		if (EquippedWeapon is Axe) {
			var dashInput = Input.IsActionJustPressed("AxeDash");
			var axe = EquippedWeapon as Axe;
			if (dashInput && axe.CanDash() && !OnFloor())
			{
				Vector3 axeOffset = (axe.GlobalPosition - GlobalPosition).Slide(Vector3.Up);
				float dashStrength = 3;
				float maxDashSpeed = 100;
				var dashVec = dashStrength * axeOffset;
				if (dashVec.Length() > maxDashSpeed)
				{
					dashVec = maxDashSpeed * axeOffset.Normalized();
				}
				ApplyImpulse(dashVec * Mass);
				axe.PullToCarrier();
			}
		}
	}

	void HandleInventoryInputs() {
		var slot0 = Input.IsActionJustPressed("Item1");
		var slot1 = Input.IsActionJustPressed("Item2");
		var slot2 = Input.IsActionJustPressed("Item3");
		var scrollUp = Input.IsActionJustPressed("ScrollItemUp");
		var scrollDown = Input.IsActionJustPressed("ScrollItemDown");
		if (EquippedWeapon != null) {
			EquippedWeapon.Visible = false;
		}
		Weapon prevWeapon = EquippedWeapon;
		if (slot0) {
			EquippedSlot = 0;
		}
		else if (slot1) {
			EquippedSlot = 1;
		}
		else if (slot2) {
			EquippedSlot = 2;
		}
		else if (scrollUp) {
			EquippedSlot += 1;
		}
		else if (scrollDown) {
			EquippedSlot -= 1;
		}
		EquippedSlot = EquippedSlot % InventorySize;
		if (prevWeapon != EquippedWeapon && prevWeapon != null) {
			prevWeapon.CancelAttack();
		}
		if (EquippedWeapon != null) {
			EquippedWeapon.Visible = true;
		}
	}

	float WeaponTheta = 0;
	void AnimateWeaponHolder() {
		float horizSpeed = LinearVelocity.Slide(Vector3.Up).Length();
		float maxOscillationAmplitude = .1f;
		float oscillationAmplitude = MathF.Min(horizSpeed * .003f, maxOscillationAmplitude);
		float oscillationFrequency = 3 * MathF.Pow(horizSpeed, .5f);
		if (!OnFloor()) {
			oscillationFrequency /= 3.5f;
			oscillationAmplitude /= 1.2f;
		}
		float theta_diff = oscillationFrequency * (float)GetPhysicsProcessDeltaTime();
		WeaponTheta += theta_diff;
		Vector3 oscillationDirection = (Vector3.Up + Vector3.ModelFront * .5f).Normalized();
		WeaponHolder.Position = oscillationDirection * MathF.Sin(WeaponTheta) * oscillationAmplitude;
	}
	public Vector3 VelLastFrame;
	public Vector3 PosLastFrame;
	public override void _PhysicsProcess(double delta)
	{
		Vector3 movementForce = CalculateMovementForce();
		ApplyCentralForce(movementForce);
		HandleJump();
		HandleFastfall();
		HandleFriction();
		HandleGroundSnap();
		DetermineInteractionTarget();
		HandleInteraction();
		HandleWeaponInputs();
		AnimateWeaponHolder();
		VelLastFrame = LinearVelocity;
		PosLastFrame = GlobalPosition;
	}

	public override void _Process(double delta)
	{
		HandleInventoryInputs();
	}

	void EquipWeapon(Weapon weapon) {
		Node3D attachNode = Attachments[weapon.Id];
		weapon.Attach(attachNode);
		if (EquippedWeapon == null) {
			Weapons[EquippedSlot] = weapon;
		}
		else {
			bool foundSlot = false;
			for (int i = 0; i < InventorySize; i++) {
				Weapon currWeapon = Weapons[i];
				if (currWeapon == null) {
					Weapons[i] = weapon;
					weapon.Visible = false;
					foundSlot = true;
					break;
				}
			}
			if (!foundSlot) {
				EquippedWeapon.Detach();
				Weapons[EquippedSlot] = weapon;
			}
			
		}
	}

	void DequipWeapon() {
		if (EquippedWeapon == null) {
			return;
		}
		EquippedWeapon.Detach();
		Weapons[EquippedSlot] = null;
	}

	int SwordAnimHistory = 0;
	void SwingWindSword() {
		int choice = (int)GD.Randi() % SwordAnimHistory;
		if (choice >= 0) {
			AnimPlayer.Play("swing_wind");
			SwordAnimHistory -= 1;
		} else {
			AnimPlayer.Play("swing_wind_2");
			SwordAnimHistory += 1;
		}
	}
	public bool Swinging = false;
	public void Swing() {
		Weapon.ID id = EquippedWeapon.Id;
		switch(id) {
			case Weapon.ID.WIND_SWORD: {
				SwingWindSword();
				break;
			}
			default: {
				Debug.Assert(false, "Attempted to Swing a weapon that wasn't implemented");
				break;
			}
		}
		Swinging = true;
	}

	void EndSwing() {
		Swinging = false;
	}

	void ActivateSwordHitbox() {
		Swingable sword = EquippedWeapon as Swingable;
		sword.ActivateHitbox();
	}

	void DeactivateSwordHitbox() {
		Swingable sword = EquippedWeapon as Swingable;
		sword.DeactivateHitbox();
	}
}
