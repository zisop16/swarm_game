using Godot;
using System;
using System.Collections.Generic;

public partial class EnemySpawnPoint : Node3D
{
	public static List<EnemySpawnPoint> EnemySpawnPoints;
	public static void Spawn(Enemy.ID Id) {
		int ind = (int)GD.Randi() % EnemySpawnPoints.Count;
		EnemySpawnPoint selectedSpawn = EnemySpawnPoints[ind];
		selectedSpawn.SpawnEnemy(Id);
	}
	[Export]
	public Godot.Collections.Dictionary<Enemy.ID, PackedScene> Enemies;

	public override void _Ready() {
		EnemySpawnPoints.Add(this);
	}

	public override void _ExitTree() {
		EnemySpawnPoints.Remove(this);
	}

	Vector3 DetermineSpawnLocation() {
		float theta = (float)GD.RandRange(0, 2 * MathF.PI);
		float spawnRadius = (float)GD.RandRange(0, 7);
		Vector3 randomXZOffset = Vector3.Right.Rotated(Vector3.Up, theta) * spawnRadius;
		var spawnPos = GlobalPosition + randomXZOffset;
		float yLevel = Global.Terrain.GetHeight(spawnPos) + .5f;
		spawnPos = Utils.SetY(spawnPos, yLevel);
		return spawnPos;
	}

	void SpawnEnemy(Enemy.ID Id) {
		Vector3 pos = DetermineSpawnLocation();
		PackedScene scene = Enemies[Id];
		Enemy instance = (Enemy)scene.Instantiate();
		Global.Map.AddChild(instance);
		instance.GlobalPosition = pos;
	}
}
