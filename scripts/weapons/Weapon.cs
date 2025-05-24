using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public abstract partial class Weapon : RigidBody3D
{
	public enum ID { AXE, WIND_SWORD };
	public ID Id { get; protected set; }
	public Node3D Carrier = null;
	List<SwappableMesh> AllMeshes = new List<SwappableMesh>();
	protected float Damage = 0;
	public override void _Ready() {
		MeshContainer model = GetNode<MeshContainer>("Model");
		FindMeshesRecursive(model);
		SetCollisions(true);
	}

	protected void SetCollisions(bool flag) {
		if (flag) {
			CollisionLayer = Global.WEAPON_LAYER;
			CollisionMask = Global.WEAPON_LAYER | Global.PLAYER_LAYER | Global.MAP_LAYER;
		}
		else {
			CollisionLayer = 0;
			CollisionMask = 0;
		}
	}

	void FindMeshesRecursive(Node obj) {
		if (obj is MeshInstance3D) {
			Debug.Assert(obj is SwappableMesh, "Weapon had standard mesh instead of outlineable mesh");
			AllMeshes.Add(obj as SwappableMesh);
		}
		foreach (Node child in obj.GetChildren()) {
			FindMeshesRecursive(child);
		}
	}

	bool Outlined = false;
	protected void SetOutline(bool flag) {
		if (Outlined == flag) {
			return;
		}
		foreach (SwappableMesh mesh in AllMeshes) {
			mesh.SetSwap(flag);
		}
		Outlined = flag;
	}

	protected void ApplyDamage(Enemy target, Vector3 impulse) {
		target.ReceiveDamage(Damage, impulse);
	}

	public void Attach(Node3D target) {
		Carrier = target;
		SetCollisions(false);
		GravityScale = 0;
		SetOutline(false);
	}

	public void Detach() {
		SetCollisions(true);
		GravityScale = 1;
		Carrier = null;
	}

	public override void _Process(double delta) {
		if (Carrier != null) {
			GlobalPosition = Carrier.GlobalPosition;
			GlobalRotation = Carrier.GlobalRotation;
		}
	}

	public override void _PhysicsProcess(double delta) {
		if (Carrier == null) {
			SetOutline(Global.InteractionTarget == this);
		}
		float terrain_height = Global.Terrain.GetHeight(GlobalPosition);
		if (GlobalPosition.Y + 2f < terrain_height) {
			GlobalPosition = Utils.SetY(GlobalPosition, terrain_height + 1f);
			LinearVelocity = Vector3.Zero;
		}
	}
	public abstract void Attack();
	public abstract void CancelAttack();
}
