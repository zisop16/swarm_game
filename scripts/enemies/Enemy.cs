using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

public partial class Enemy : RigidBody3D
{
	public enum ID { MUTANT, SPIDER, ZOMBIE };
	enum STATE { IDLE, WALK, RUN, ATTACK_0, ATTACK_1 };
	NavigationAgent3D NavAgent;
	PackedScene BloodSplatter;
	Node3D BloodSpawn;
	MeshContainer Model;
	AnimationTree AnimTree;
	public ID Id { get; protected set; }
	bool Dead = false;
	STATE AnimState = STATE.IDLE;
	static List<Enemy> AllEnemies = new List<Enemy>();
	public override void _Ready()
	{
		NavAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		BloodSplatter = GD.Load<PackedScene>("res://scenes/particle_effects/blood_splatter.tscn");
		BloodSpawn = GetNode<Node3D>("%BloodSpawn");
		Model = GetNode<MeshContainer>("%MeshContainer");
		AnimTree = GetNode<AnimationTree>("AnimationTree");
		SetStats();
		health = MaxHealth;
		animUpdateOffset = (float)GD.RandRange(0, timeBetweenAnimUpdate);
		AnimTree.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
		CollisionLayer = Global.ENEMY_LAYER;
		CollisionMask = Global.ENEMY_LAYER | Global.MAP_LAYER | Global.WEAPON_LAYER;
		LockRotation = true;
		NavAgent.VelocityComputed += OnVelocityComputed;
		AllEnemies.Add(this);
	}
	protected float MaxHealth;
	protected float MaxHorizSpeed;
	float health;
	public virtual void SetStats()
	{
		MaxHealth = 0;
		MaxHorizSpeed = 0;
	}
	private void SplatterBlood(Vector3 direction, float amount)
	{
		Node3D blood = (Node3D)BloodSplatter.Instantiate();
		Basis rotationBasis = Transform3D.Identity.Basis;
		float yRot = Vector3.ModelFront.SignedAngleTo(direction, Vector3.Up);
		rotationBasis = rotationBasis.Rotated(Vector3.Up, yRot);
		Vector3 xAxis = rotationBasis * Vector3.ModelRight;
		Vector3 zAxis = rotationBasis * Vector3.ModelFront;
		float zRot = zAxis.SignedAngleTo(direction, xAxis);
		rotationBasis = rotationBasis.Rotated(xAxis, zRot);
		Global.Map.AddChild(blood);
		blood.Transform = new Transform3D(rotationBasis, blood.GlobalPosition);
		blood.GlobalPosition = BloodSpawn.GlobalPosition;
		GpuParticles3D bloodParticles = (GpuParticles3D)blood.FindChild("blood");
		bloodParticles.AmountRatio = amount;
	}
	const float farNavigationTickrate = .2f;
	const float closeNavigationTickrate = .7f;
	float hitMovementDelay = .4f;
	float lastTimeHit = float.NegativeInfinity;
	// One navigation tick per second
	float lastNavigationUpdate = 0;
	bool startedNavigation = false;
	public void ReceiveDamage(float damage, Vector3 impulse)
	{
		health -= damage;
		LinearVelocity = Vector3.Zero;
		ApplyImpulse(impulse);
		lastTimeHit = (float)Global.Time;
		Vector3 blood_direction = impulse.Slide(Vector3.Up).Rotated(Vector3.Up, Mathf.Pi).Normalized() + Vector3.Up / 2;
		var bloodAmount = damage / MaxHealth;
		SplatterBlood(blood_direction, bloodAmount);
		Model.SetSwap(true);
		if (health <= 0)
		{
			Die();
		}
	}
	void Die()
	{
		Dead = true;
		Model.Erode();
	}

	void HandleNavigation()
	{
		if (RecentlyDamaged())
		{
			return;
		}
		if (!NavAgent.IsTargetReachable())
		{
			return;
		}
		Vector3 pathPoint = NavAgent.GetNextPathPosition();
		Vector3 movementDirec = (pathPoint - GlobalPosition).Slide(Vector3.Up).Normalized();
		NavAgent.Velocity = MaxHorizSpeed * movementDirec + LinearVelocity.Project(Vector3.Up);
	}
	void UpdateNavigationTarget()
	{
		if (RecentlyDamaged())
		{
			return;
		}
		if (!startedNavigation || ShouldUpdateNavigation())
		{
			Vector3 target = Global.Player.PosLastFrame + Global.Player.VelLastFrame;
			float yLevel = Global.Terrain.GetHeight(target);
			target.Y = yLevel;
			NavAgent.TargetPosition = target;
			lastNavigationUpdate = (float)Global.Time;
			startedNavigation = true;
			Global.Map.NavUpdatesThisFrame += 1;
		}
	}
	public static void MassUpdateNavigation(int threadNum)
	{
		int numThreads = Map.NumNavThreads;
		int startInd = 0;
		int size = AllEnemies.Count;
		int baseNum = size / numThreads;
		int excess = size % numThreads;
		int endInd = baseNum;
		if (excess > 0)
		{
			excess -= 1;
			endInd += 1;
		}
		int thing = threadNum;
		while (thing > 0)
		{
			startInd = endInd;
			endInd += baseNum;
			if (excess > 0)
			{
				excess -= 1;
				endInd += 1;
			}
			thing -= 1;
		}
		for (int i = startInd; i < endInd; i++)
		{
			Enemy curr = AllEnemies[i];
			if (Global.Map.NavUpdatesThisFrame >= Map.MaxNavUpdates)
			{
				return;
			}
			curr.UpdateNavigationTarget();
		}
	}

	void OnVelocityComputed(Vector3 safeVelocity)
	{
		LinearVelocity = safeVelocity.Slide(Vector3.Up) + LinearVelocity.Project(Vector3.Up);
	}

	float DetermineNavigationTickrate()
	{
		float playerDist = (Global.Player.PosLastFrame - PosLastFrame).Length();
		float actualNavTickrate;
		if (playerDist < 20)
		{
			actualNavTickrate = closeNavigationTickrate;
		}
		else
		{
			actualNavTickrate = farNavigationTickrate;
		}
		return actualNavTickrate;
	}

	bool ShouldUpdateNavigation()
	{
		float doubleTickRate = DetermineNavigationTickrate() * 2;
		float halfTickTime = 1 / doubleTickRate;
		if ((Global.Time - lastNavigationUpdate) < halfTickTime)
		{
			return false;
		}
		double delta = GetPhysicsProcessDeltaTime();
		bool probabilityUpdate = GD.Randf() < (delta * doubleTickRate);
		return probabilityUpdate;
	}
	bool RecentlyDamaged()
	{
		return (Global.Time - lastTimeHit) < hitMovementDelay;
	}
	float timeSinceAnimUpdate = 0;
	const float timeBetweenAnimUpdate = .1f;
	float animUpdateOffset;
	void DetermineAnimState()
	{
		if (!Visible)
		{
			return;
		}
		if (RecentlyDamaged())
		{
			AnimState = STATE.IDLE;
		}
		else
		{
			AnimState = STATE.RUN;
		}
		Model.SetSwap(RecentlyDamaged());
	}
	Vector3 PosLastFrame = Vector3.Zero;
	void AnimatedBodyPhys()
	{
		HandleNavigation();
		DetermineAnimState();
		PosLastFrame = GlobalPosition;
	}

	public override void _Process(double delta) {
		timeSinceAnimUpdate += (float)delta;
		if (timeSinceAnimUpdate >= timeBetweenAnimUpdate + animUpdateOffset) {
			timeSinceAnimUpdate -= timeBetweenAnimUpdate;
			AnimTree.Advance(timeBetweenAnimUpdate);
			var player_direc = (Global.Player.GlobalPosition - GlobalPosition).Slide(Vector3.Up);
			GlobalRotation = Utils.SetY(GlobalRotation, Vector3.ModelFront.SignedAngleTo(player_direc, Vector3.Up));
		}
	}
	public override void _PhysicsProcess(double delta)
	{
		if (!Dead){
			AnimatedBodyPhys();
		}
		else
		{
			if (Model.FullyEroded())
			{
				QueueFree();
			}
		}
	}
}