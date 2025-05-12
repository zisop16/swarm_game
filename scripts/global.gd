extends Node

var time: float
var interaction_target: Weapon = null
var player: Player
var terrain: Terrain3D
func _process(delta: float) -> void:
	time = Time.get_ticks_usec() / (10.**6)