using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Swingable : Weapon
{
	Area3D Hitbox;
	GpuParticles3D Trail;

	Vector3 TrailPos;
	int TrailLength;
	int MultiHit = 5;
	public override void _Ready() {
		base._Ready();
		Hitbox = GetNode<Area3D>("Hitbox");
		Trail = GetNode<GpuParticles3D>("GPUTrail3D");
		Hitbox.CollisionLayer = 0;
		// These weapons collide with monsters on layer 3
		Hitbox.CollisionMask = Global.ENEMY_LAYER;
		TrailPos = Trail.Position;
		// TrailLength = Trail.Length;
		TrailLength = (int)Trail.Get("length");
		DeactivateHitbox();
	}

	public override void Attack() {
		if (Global.Player.Swinging) {
			return;
		}
		Global.Player.Swing();
		Trail.TopLevel = false;
		Trail.Position = TrailPos;
		Trail.Set("length", 0);
		HitCount = 0;
		AlreadyHit.Clear();
	}

	public void ActivateHitbox() {
		Hitbox.Monitoring = true;
		Trail.Set("length", TrailLength);
	}
	public void DeactivateHitbox() {
		Hitbox.Monitoring = false;
		Trail.TopLevel = true;
	}
	public override void CancelAttack() {
		DeactivateHitbox();
		Player p = Global.Player;
		p.Swinging = false;
		p.AnimPlayer.Stop();
	}

	List<Enemy> AlreadyHit = new List<Enemy>();
	int HitCount;
	public override void _PhysicsProcess(double delta) {
		base._PhysicsProcess(delta);
		if (Hitbox.Monitoring) {
			return;
		}
		if (HitCount == MultiHit) {
			return;
		}
		var overlap = Hitbox.GetOverlappingBodies();
		List<Enemy> potentialTargets = new List<Enemy>();
		foreach (Node3D body in overlap) {
			if (body is Enemy) {
				Enemy e = body as Enemy;
				if (AlreadyHit.Contains(e)) {
					continue;
				}
				potentialTargets.Add(e);
			}
		}

		while (potentialTargets.Any() && HitCount != MultiHit)
		{

			HitEnemy(potentialTargets[^1]);
			potentialTargets.RemoveAt(potentialTargets.Count - 1);
		}
	}

	void HitEnemy(Enemy enemy) {
		HitCount += 1;
		float swordMomentum = 7;
		Vector3 impulseDirection = (Global.Player.LookDirection().Slide(Vector3.Up) + Vector3.Up / 2).Normalized();
		Vector3 swordImpulse = impulseDirection * swordMomentum;
		ApplyDamage(enemy, swordImpulse);
		AlreadyHit.Add(enemy);
	}

	static bool ByDistanceDescending(Enemy enemy1, Enemy enemy2) {
		float dist1 = (enemy1.GlobalPosition - Global.Player.GlobalPosition).LengthSquared();
		float dist2 = (enemy2.GlobalPosition - Global.Player.GlobalPosition).LengthSquared();
		return dist1 > dist2;
	}
}
