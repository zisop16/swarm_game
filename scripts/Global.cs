using Godot;
using System;

public partial class Global : Node
{
	public const int RAGDOLL_LAYER = 0b10000;
	public const int WEAPON_LAYER = 0b01000;
	public const int ENEMY_LAYER = 0b00100;
	public const int PLAYER_LAYER = 0b00010;
	public const int MAP_LAYER = 0b00001;

	public static double Time { get => Godot.Time.GetTicksUsec() / 1000000f; }
	public static Weapon InteractionTarget = null;
	public static Player Player;
	public static Terrain Terrain;
	public static Map Map;
}
