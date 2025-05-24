using Godot;
using System;

public partial class Terrain : Node
{
	public override void _Ready()
	{
		Global.Terrain = this;
	}
	public float GetHeight(Vector3 pos)
	{
		GodotObject data = Get("data").AsGodotObject();
		float height = (float)data.Call("get_height", pos);
		return height;
	}
}
