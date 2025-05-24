using Godot;
using System;

public partial class Axe : Weapon
{
	public override void _Ready() {
		base._Ready();
		Id = ID.AXE;
		Damage = 100;
	}

	float ZRot;
	float XRot;
	float CurveRotateVelocity;
	Vector3 VelocityLastFrame;
	bool Thrown = false;
	void HandleThrowPhysics() {
		if (!Thrown) {
			return;
		}
		float delta = (float)GetPhysicsProcessDeltaTime();
		float bounceRotationMultiplier = 1;
		if (Bouncing) {
			bounceRotationMultiplier = MathF.Pow(BounceProgressSpeed(), .5f);
		}
		float throwRotateVelocity = 50 * bounceRotationMultiplier;
		float addedXRot = delta * throwRotateVelocity;
		float addedZRot = delta * CurveRotateVelocity * bounceRotationMultiplier;
		ZRot += addedZRot;
		XRot += addedXRot;
		Basis rotationBasis = Transform3D.Identity.Basis;
		float yRot = Vector3.ModelFront.SignedAngleTo(LinearVelocity.Slide(Vector3.Up), Vector3.Up);
		rotationBasis = rotationBasis.Rotated(Vector3.Up, yRot);
		Vector3 zAxis = rotationBasis * Vector3.ModelFront;
		rotationBasis = rotationBasis.Rotated(zAxis, ZRot);
		Vector3 xAxis = rotationBasis * Vector3.ModelLeft;
		rotationBasis = rotationBasis.Rotated(xAxis, XRot);
		Transform = new Transform3D(rotationBasis, GlobalPosition);
	}

	public override void _PhysicsProcess(double delta) {
		base._PhysicsProcess(delta);
		HandleThrowPhysics();
		HandleBounce();
		HandlePull();
		// Have to store velocity last frame because this will be 0 on the frame when the object collides
		// But we need its impulse direction in order to apply impulse to the enemy
		VelocityLastFrame = LinearVelocity;
	}


	void SetThrown(bool flag) {
		Thrown = flag;
		LockRotation = flag;
		if (Thrown) {
			GravityScale = .65f;
			CollisionMask = Global.ENEMY_LAYER | Global.MAP_LAYER | Global.WEAPON_LAYER;
			CollisionLayer = 0;
			SetOutline(true);
		} else {
			GravityScale = 1;
			SetCollisions(true);
			Carrier = StoredCarrier;
			Pulling = false;
			Bouncing = false;
			SetOutline(false);
		}
	}

	Node3D StoredCarrier;
	public override void Attack() {
		if (Thrown) {
			return;
		}
		StoredCarrier = Carrier;
		Carrier = null;
		Vector3 throwDirection = Global.Player.LookDirection();
		float throwSpeed = 30;
		float playerContribution = Mathf.Max(0, Global.Player.LinearVelocity.Dot(throwDirection));
		Vector3 throwVelocity = throwDirection * (throwSpeed + playerContribution);
		ZRot = (float)GD.RandRange(-Mathf.Pi / 3, Mathf.Pi / 3);
		XRot = 0;
		CurveRotateVelocity = (float)GD.RandRange(-MathF.PI, MathF.PI);
		GlobalPosition = Global.Player.Cam.GlobalPosition + Global.Player.LookDirection() * .5f;
		LinearVelocity = throwVelocity;
		SetThrown(true);
	}
	public override void CancelAttack()
	{
	}


	float BounceLength;
	Vector3 BounceDirection;
	Vector3 InitialBouncePos;
	bool Bouncing = false;
	float BounceStart;
	bool DashUsed;
	const float BounceDuration = 1.5f;
	[Export]
	Curve BounceCurve;
	[Export]
	Curve BounceProgressCurve;
	Vector3 BounceCurveDirection;
	void OnBodyEntered(Node body) {
		if (!Thrown) {
			return;
		}
		if (body is Enemy) {
			Enemy enemy = body as Enemy;
			Vector3 impulseDirection = VelocityLastFrame.Slide(Vector3.Up).Normalized();
			impulseDirection = (impulseDirection + Vector3.Up * .5f).Normalized();
			float impulseMagnitude = VelocityLastFrame.Length() / 3f;
			Vector3 impulse = impulseDirection * impulseMagnitude * Mass;
			ApplyDamage(enemy, impulse);
			StartBounce();
		}
		else {
			StartBounce();
		}
	}

	bool Pulling = false;
	public void PullToCarrier() {
		Pulling = true;
		Bouncing = false;
		DashUsed = true;
		FirstPullFrame = true;
	}

	Vector3 CarrierHorizOffsetInitial;
	bool FirstPullFrame;
	void HandlePull() {
		if (!Pulling) {
			return;
		}
		Vector3 carrierOffset = StoredCarrier.GlobalPosition - GlobalPosition;
		float delta = (float)GetPhysicsProcessDeltaTime();
		float carrierDist = carrierOffset.Length();
		Vector3 carrierHorizOffset = carrierOffset.Slide(Vector3.Up);
		if (FirstPullFrame) {
			CarrierHorizOffsetInitial = carrierHorizOffset;
			FirstPullFrame = false;
		}
		if (carrierHorizOffset.Dot(CarrierHorizOffsetInitial) < 1) {
			SetThrown(false);
			return;
		}
		const float pull_strength = 5;
		float pull_speed = pull_strength * carrierDist;
		Vector3 pullDirec = carrierOffset.Normalized();
		GlobalPosition += pull_speed * pullDirec * delta;
	}

	void StartBounce() {
		Vector3 playerDirec = (Global.Player.GlobalPosition - GlobalPosition).Slide(Vector3.Up).Normalized();
		Vector3 primaryDirec = (playerDirec + Vector3.Up / 3f).Normalized();
		float angularSpread = .3f;
		float theta = (float)GD.RandRange(0, 2 * Mathf.Pi);
		float phi = (float)GD.RandRange(0, angularSpread);
		Basis rotBasis = Transform3D.Identity.Basis;
		float yRot = Vector3.ModelFront.SignedAngleTo(primaryDirec, Vector3.Up);
		rotBasis = rotBasis.Rotated(Vector3.Up, yRot);
		Vector3 xAxis = rotBasis * Vector3.ModelRight;
		Vector3 zAxis = rotBasis * Vector3.ModelFront;
		float xRotation = zAxis.SignedAngleTo(primaryDirec, xAxis) + phi;
		rotBasis = rotBasis.Rotated(xAxis, xRotation);
		rotBasis = rotBasis.Rotated(primaryDirec, theta);
		BounceLength = 5f;
		BounceDirection = rotBasis * Vector3.ModelFront;
		InitialBouncePos = GlobalPosition;

		Bouncing = true;
		DashUsed = false;
		BounceStart = (float)Global.Time;
		bool useLeft = Convert.ToBoolean(GD.Randi() & 0b1);
		BounceCurveDirection = (rotBasis * (useLeft ? Vector3.ModelLeft : Vector3.ModelRight)).Slide(Vector3.Up).Normalized();
		CollisionMask = 0;
		GravityScale = 0;
		LinearVelocity = Vector3.Zero;
	}

	public bool CanDash() {
		return Bouncing && !DashUsed;
	}

	void HandleBounce() {
		if (!Bouncing) {
			return;
		}
		float prog = BounceProgress();
		if (prog >= 1) {
			SetThrown(false);
			return;
		}
		Vector3 bounceOffset = BounceCurveDirection * BounceCurve.Sample(prog) * BounceLength * prog;
		Vector3 bounceMain = BounceDirection * BounceLength * prog;
		Vector3 bounceDisplace = bounceOffset + bounceMain;
		GlobalPosition = InitialBouncePos + bounceDisplace;
	}

	float BounceProgress() {
		float rawProgress = (float)(Global.Time - BounceStart) / BounceDuration;
		float actualProgress = BounceProgressCurve.Sample(rawProgress);
		return actualProgress;
	}

	float BounceProgressSpeed() {
		float t0 = (float)(Global.Time - BounceStart) / BounceDuration;
		float delta = .0001f;
		float t1 = t0 + delta;
		var prog0 = BounceProgressCurve.Sample(t0);
		var prog1 = BounceProgressCurve.Sample(t1);
		return (prog1 - prog0) / delta;
	}

}
