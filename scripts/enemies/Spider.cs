using Godot;
using System;

public partial class Spider : Enemy
{
	public override void _Ready()
	{
		base._Ready();
		Id = ID.SPIDER;
	}

}
