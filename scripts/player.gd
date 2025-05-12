class_name Player
extends RigidBody3D

@onready var cam: Camera3D = $Camera
@onready var ground_ray: RayCast3D = $GroundRay
@onready var interaction_ray: RayCast3D = %InteractionRay
@onready var model: Node3D = $Model
var speed_control: float = 25
var jump_speed: float = 8
var max_ground_speed: float = 10
var max_air_speed: float = 20
var base_control = .3
var equipped_slot: int = 0
var weapons: Array[Weapon] = []
const inventory_size: int = 3

@onready var attachments: Dictionary[Weapon.ID, Node3D] = {
	Weapon.ID.axe: $Camera/Axe
}

func _ready() -> void:
	Global.player = self
	for i in inventory_size:
		weapons.append(null)

func equipped_weapon() -> Weapon:
	return weapons[equipped_slot]

func look_direction() -> Vector3:
	return -cam.get_global_transform_interpolated().basis.z

var movement_force_direc: Vector3
func calculate_movement_force() -> Vector3:
	var movement_direc = Input.get_vector("Left", "Right", "Back", "Forward")
	var cam_basis := cam.get_global_transform_interpolated().basis
	var camera_forward = -cam_basis.z.slide(Vector3.UP)
	var rot_angle = Vector3.MODEL_FRONT.signed_angle_to(camera_forward, Vector3.UP)
	movement_force_direc = (movement_direc.x * Vector3.MODEL_RIGHT + movement_direc.y * Vector3.MODEL_FRONT).normalized()
	movement_force_direc = movement_force_direc.rotated(Vector3.UP, rot_angle)
	var target_speed := max_ground_speed if on_floor() else max_air_speed
	var target_horiz_velocity = target_speed * movement_force_direc
	var curr_horiz_velocity = linear_velocity.slide(Vector3.UP)
	var velocity_diff = target_horiz_velocity - curr_horiz_velocity
	var vel_adjustment_magnitude := (velocity_diff.length() / target_speed)
	var movement_accel = (vel_adjustment_magnitude + base_control) * speed_control
	var movement_force = velocity_diff.normalized() * movement_accel * mass
	return movement_force

const coyote_time: float = .17
var last_time_on_floor: float = 0
const jump_cooldown: float = .1
var last_time_jumped: float = 0
var used_jump := false
func handle_jump():
	if jump_on_cooldown():
		return
	if on_floor():
		last_time_on_floor = Global.time
	else:
		if not can_coyote_jump():
			return
	if used_jump:
		if on_floor():
			used_jump = false
		else:
			return
	var jump_input = Input.is_action_just_pressed("Jump")
	if not jump_input:
		return
	used_jump = true
	last_time_jumped = Global.time
	var jump_impulse = Vector3.UP * jump_speed * mass
	if linear_velocity.y < 0:
		linear_velocity.y = 0
	apply_impulse(jump_impulse)
	if not on_floor() and can_coyote_jump():
		print("Coyote jump")

func jump_on_cooldown() -> bool:
	return (Global.time - last_time_jumped) < jump_cooldown

func can_coyote_jump() -> bool:
	return (Global.time - last_time_on_floor) < coyote_time

func on_floor() -> bool:
	return ground_ray.is_colliding()

func handle_fastfall():
	const fast_fall_scale = .4
	if linear_velocity.y < -1:
		apply_central_force(fast_fall_scale * mass * get_gravity())

func determine_ground_friction() -> Vector3:
	const fric_exponent: float = .3
	const fric_strength: float = 3
	const moving_fric_multiplier: float = .1
	var actual_fric_strength: float = fric_strength * (1. if movement_force_direc == Vector3.ZERO else moving_fric_multiplier)
	var fric_magnitude = (linear_velocity.length_squared() ** (fric_exponent / 2)) * actual_fric_strength * mass
	return fric_magnitude * -linear_velocity.normalized()

func determine_air_friction() -> Vector3:
	const fric_exponent: float = 1.2
	const fric_strength: float = .004
	var fric_magnitude: float = (linear_velocity.length_squared() ** (fric_exponent / 2.)) * mass * fric_strength
	return fric_magnitude * -linear_velocity.normalized()

func handle_friction():
	var friction: Vector3
	if on_floor():
		friction = determine_ground_friction()
	else:
		friction = determine_air_friction()
	apply_central_force(friction)

func handle_ground_snap():
	if used_jump:
		return
	if not ground_ray.is_colliding():
		return
	var snap_distance = global_position.y - ground_ray.get_collision_point().y
	global_position.y -= snap_distance * .5

func determine_interaction_target():
	if not interaction_ray.is_colliding():
		Global.interaction_target = null
	var target = interaction_ray.get_collider()
	if target is Weapon:
		Global.interaction_target = target
	else:
		Global.interaction_target = null

func handle_interaction():
	if Global.interaction_target == null:
		return
	if Input.is_action_just_pressed("Equip"):
		equip_weapon(Global.interaction_target)

func handle_attack():
	var should_attack := Input.is_action_just_pressed("Attack")
	if not should_attack:
		return
	if equipped_weapon() == null:
		return
	equipped_weapon().attack()

func handle_inventory_inputs():
	var slot0 = Input.is_action_just_pressed("Item1")
	var slot1 = Input.is_action_just_pressed("Item2")
	var slot2 = Input.is_action_just_pressed("Item3")
	var scroll_up = Input.is_action_just_pressed("ScrollItemUp")
	var scroll_down = Input.is_action_just_pressed("ScrollItemDown")
	if equipped_weapon() != null:
		equipped_weapon().visible = false
	if slot0:
		equipped_slot = 0
	elif slot1:
		equipped_slot = 1
	elif slot2:
		equipped_slot = 2
	elif scroll_up:
		equipped_slot += 1
	elif scroll_down:
		equipped_slot -= 1
	equipped_slot = equipped_slot % inventory_size
	if equipped_weapon() != null:
		equipped_weapon().visible = true

func _physics_process(delta: float) -> void:
	var movement_force := calculate_movement_force()
	apply_central_force(movement_force)
	handle_jump()
	handle_fastfall()
	handle_friction()
	handle_ground_snap()
	determine_interaction_target()
	handle_interaction()
	handle_attack()

func _process(delta: float) -> void:
	handle_inventory_inputs()
	
func equip_weapon(weapon: Weapon):
	var attach_node := attachments[weapon.id]
	weapon.attach(attach_node)
	if equipped_weapon() == null:
		weapons[equipped_slot] = weapon
	else:
		var found_slot := false
		for i in inventory_size:
			var curr_weapon := weapons[i]
			if curr_weapon == null:
				weapons[i] = weapon
				weapon.visible = false
				found_slot = true
				break
		if not found_slot:
			equipped_weapon().detach()
			weapons[equipped_slot] = weapon

func dequip_weapon():
	if equipped_weapon() == null:
		return
	equipped_weapon().detach()
	weapons[equipped_slot] = null
