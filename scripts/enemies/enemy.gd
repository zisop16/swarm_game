class_name Enemy
extends CharacterBody3D

@onready var bone_sim: PhysicalBoneSimulator3D = %PhysicalBoneSimulator3D
@onready var physical_skeleton: Skeleton3D = $Skeleton3D
@onready var anim_tree: AnimationTree = $AnimationTree
@onready var nav_agent: NavigationAgent3D = $NavigationAgent3D
@onready var blood_splatter: PackedScene = load("res://scenes/particle_effects/blood_splatter.tscn")
@onready var blood_spawn: Node3D = %BloodSpawn
@onready var model: SwappableMesh = %Model

var mass: float = 1
enum ID {mutant, spider}
var physics_bones: Array[PhysicalBone3D] = []
var id: ID
var dead: bool = false
enum STATE {IDLE, WALK, RUN, ATTACK_0, ATTACK_1}
var anim_state: STATE = STATE.IDLE
var max_health: float = 100
var health := max_health

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	for child in bone_sim.get_children():
		if child is PhysicalBone3D:
			var bone := child as PhysicalBone3D
			physics_bones.append(bone)
			bone.collision_layer = 0
			bone.collision_mask = 0
	collision_layer = Global.ENEMY_LAYER
	collision_mask = Global.ENEMY_LAYER | Global.PLAYER_LAYER | Global.MAP_LAYER
	nav_agent.velocity_computed.connect(on_velocity_computed)

func splatter_blood(direction: Vector3, amount: float):
	var blood: Node3D = blood_splatter.instantiate()
	var rotation_basis := Transform3D.IDENTITY.basis
	var y_rot = Vector3.MODEL_FRONT.signed_angle_to(direction, Vector3.UP)
	rotation_basis = rotation_basis.rotated(Vector3.UP, y_rot)
	var x_axis = rotation_basis * Vector3.MODEL_RIGHT
	var z_axis = rotation_basis * Vector3.MODEL_FRONT
	var z_rot = z_axis.signed_angle_to(direction, x_axis)
	rotation_basis = rotation_basis.rotated(x_axis, z_rot)
	Global.map.add_child(blood)
	blood.transform.basis = rotation_basis
	blood.global_position = blood_spawn.global_position
	var blood_particles: GPUParticles3D = blood.find_child("blood")
	blood_particles.amount_ratio = amount

func apply_impulse(impulse: Vector3):
	velocity += impulse / mass

func receive_damage(damage: float, impulse: Vector3):
	health -= damage
	velocity = Vector3.ZERO
	apply_impulse(impulse)
	last_time_hit = Global.time
	var blood_direction := impulse.slide(Vector3.UP).rotated(Vector3.UP, PI).normalized() + Vector3.UP / 2
	var blood_amount := damage / max_health
	splatter_blood(blood_direction, blood_amount)
	model.set_swap(true)
	if health <= 0:
		die()

func die():
	bone_sim.physical_bones_start_simulation()
	for bone in physics_bones:
		bone.collision_layer = Global.RAGDOLL_LAYER
		bone.collision_mask = Global.RAGDOLL_LAYER | Global.MAP_LAYER
		bone.linear_velocity = velocity
	collision_layer = 0
	collision_mask = 0
	dead = true
	nav_agent.velocity_computed.disconnect(on_velocity_computed)
	model.erode()

func _physics_process(delta: float) -> void:
	if not dead:
		animated_body_phys()
	else:
		if model.fully_eroded():
			queue_free()

var accel: float = 10
var target_direction := Vector3.ZERO
var max_horiz_speed: float = 2
var hit_movement_delay: float = .4
var last_time_hit: float = -1000
func determine_movement_accel():
	nav_agent.target_position = Global.player.global_position
	var next_pos := nav_agent.get_next_path_position()
	var movement_delta := next_pos - global_position
	var target_velocity := max_horiz_speed * movement_delta.slide(Vector3.UP)
	nav_agent.velocity = target_velocity + velocity.y * Vector3.UP

func recently_damaged() -> bool:
	return (Global.time - last_time_hit) < hit_movement_delay

func on_velocity_computed(safe_velocity: Vector3):
	var delta := get_physics_process_delta_time()
	var velocity_diff := safe_velocity - velocity
	var accel_direc := velocity_diff.slide(Vector3.UP).normalized()
	## If recently damaged, the enemy will not accelerate in the horizontal direction
	if not recently_damaged():
		velocity = velocity + accel_direc * delta * accel
	target_direction = safe_velocity.slide(Vector3.UP).normalized()
	handle_friction()
	var player_direc = Global.player.global_position - global_position
	global_rotation.y = Vector3.MODEL_FRONT.signed_angle_to(player_direc, Vector3.UP)
	move_and_slide()

func determine_ground_friction() -> Vector3:
	const fric_exponent: float = .3
	const fric_strength: float = 3
	const moving_fric_multiplier: float = .1
	var actual_fric_strength: float = fric_strength * (1. if target_direction == Vector3.ZERO else moving_fric_multiplier)
	var fric_magnitude = (velocity.length_squared() ** (fric_exponent / 2)) * actual_fric_strength * mass
	return fric_magnitude * -velocity.normalized()

func determine_air_friction() -> Vector3:
	const fric_exponent: float = 1.2
	const fric_strength: float = .004
	var fric_magnitude: float = (velocity.length_squared() ** (fric_exponent / 2.)) * mass * fric_strength
	return fric_magnitude * -velocity.normalized()

func handle_friction():
	var delta := get_physics_process_delta_time()
	var friction: Vector3
	if is_on_floor():
		friction = determine_ground_friction()
	else:
		friction = determine_air_friction()
	velocity += friction * delta / mass

func handle_gravity():
	var delta := get_physics_process_delta_time()
	if not is_on_floor():
		velocity += get_gravity() * delta

func determine_anim_state():
	if recently_damaged():
		anim_state = STATE.IDLE
	else:
		anim_state = STATE.RUN
	model.set_swap(recently_damaged())

func animated_body_phys():
	handle_gravity()
	determine_movement_accel()
	determine_anim_state()
