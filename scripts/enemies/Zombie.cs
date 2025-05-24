using Godot;
using System;

public partial class Zombie : Enemy
{
	public override void _Ready() {
		base._Ready();
		Id = ID.ZOMBIE;
	}

	public override void SetStats() {
		MaxHealth = 100;
		MaxHorizSpeed = 10;
	}
}
