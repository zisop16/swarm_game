using Godot;
using System;

public static class Utils
{
	public static Vector3 SetY(Vector3 v, float newY)
	{
		return new Vector3(v.X, newY, v.Z);
	}
}
