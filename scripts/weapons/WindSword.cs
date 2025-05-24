using Godot;
using System;

public partial class WindSword : Swingable
{
	public override void _Ready()
	{
		base._Ready();
		Id = ID.WIND_SWORD;
		Damage = 35;
	}
}
