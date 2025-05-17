class_name Weapon
extends RigidBody3D

var carrier: Node3D = null
enum ID {
	axe, wind_sword
}
var id: ID
var all_meshes: Array[SwappableMesh] = []
var damage: float = 0
func _ready() -> void:
	find_meshes_recursive($Model)
	collision_layer = Global.WEAPON_LAYER
	collision_mask = Global.WEAPON_LAYER | Global.PLAYER_LAYER | Global.MAP_LAYER

func find_meshes_recursive(obj: Node3D):
	if obj is MeshInstance3D:
		assert(obj is SwappableMesh, "Weapon had standard mesh instead of outlineable mesh")
	if obj is SwappableMesh:
		all_meshes.push_back(obj)
	for child in obj.get_children():
		find_meshes_recursive(child)

var outlined: bool = false
func set_outline(flag: bool):
	if outlined == flag:
		return
	for mesh in all_meshes:
		mesh.set_swap(flag)
	outlined = flag

func apply_damage(target: Enemy, impulse: Vector3):
	target.receive_damage(damage, impulse)

func attach(target: Node3D):
	carrier = target
	collision_layer = 0
	collision_mask = 0
	gravity_scale = 0
	set_outline(false)

func detach():
	collision_layer = 0b1000
	collision_mask = 0b1101
	gravity_scale = 1
	carrier = null

func _process(delta: float) -> void:
	if carrier != null:
		global_position = carrier.global_position
		global_rotation = carrier.global_rotation

func _physics_process(delta: float) -> void:
	if carrier == null:
		set_outline(Global.interaction_target == self)
	var terrain_height = Global.terrain.data.get_height(global_position)
	if global_position.y + 2. < terrain_height:
		global_position.y = terrain_height + 1.
		linear_velocity = Vector3.ZERO

func attack():
	pass
