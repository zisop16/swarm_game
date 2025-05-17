extends Node

const RAGDOLL_LAYER := 0b10000
const WEAPON_LAYER := 0b01000
const ENEMY_LAYER := 0b00100
const PLAYER_LAYER := 0b00010
const MAP_LAYER := 0b00001

var time: float
var interaction_target: Weapon = null
var player: Player
var terrain: Terrain3D
var map: Map
func _process(delta: float) -> void:
	time = Time.get_ticks_usec() / (10.**6)