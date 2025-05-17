class_name Axe
extends Weapon

func _ready() -> void:
	super._ready()
	id = ID.axe
	damage = 100

var z_rot: float
var x_rot: float
var curve_rotate_velocity: float
func handle_throw_physics():
	if not thrown:
		return
	var delta := get_physics_process_delta_time()
	var throw_rotate_velocity := linear_velocity.slide(Vector3.UP).length() * 2.
	var added_x_rot = delta * throw_rotate_velocity
	var added_z_rot = delta * curve_rotate_velocity
	z_rot += added_z_rot
	x_rot += added_x_rot
	var rotation_basis := Transform3D.IDENTITY.basis
	var y_rot = Vector3.MODEL_FRONT.signed_angle_to(linear_velocity.slide(Vector3.UP), Vector3.UP)
	rotation_basis = rotation_basis.rotated(Vector3.UP, y_rot)
	var z_axis = rotation_basis * Vector3.MODEL_FRONT
	rotation_basis = rotation_basis.rotated(z_axis, z_rot) 
	var x_axis = rotation_basis * Vector3.MODEL_LEFT
	rotation_basis = rotation_basis.rotated(x_axis, x_rot)
	transform.basis = rotation_basis

func _physics_process(delta: float) -> void:
	super._physics_process(delta)
	handle_throw_physics()
	# Have to store velocity last frame because this will be 0 on the frame when the object collides
	# But we need its impulse direction in order to apply impulse to the enemy
	velocity_last_frame = linear_velocity


func set_thrown(flag: bool):
	thrown = flag
	lock_rotation = flag
	if thrown:
		gravity_scale = .8
	else:
		gravity_scale = 1

var thrown: bool = false
func attack():
	Global.player.dequip_weapon()
	var throw_direction = Global.player.look_direction()
	var throw_speed = 30
	var throw_velocity: Vector3 = throw_direction * (throw_speed + Global.player.linear_velocity.dot(throw_direction))
	z_rot = randf_range(-PI/3, PI/3)
	x_rot = 0
	curve_rotate_velocity = randf_range(-PI, PI)
	global_position = Global.player.cam.global_position + Global.player.look_direction() * .5
	linear_velocity = Vector3.ZERO
	apply_impulse(throw_velocity * mass)
	set_thrown(true)
	
	
var velocity_last_frame: Vector3
func _on_body_entered(body:Node) -> void:
	if not thrown:
		return
	if body is Enemy:
		var enemy = body as Enemy
		var impulse_direction := velocity_last_frame.slide(Vector3.UP).normalized()
		impulse_direction = (impulse_direction + Vector3.UP * .5).normalized()
		var impulse_magnitude := velocity_last_frame.length() / 3
		apply_damage(enemy, impulse_direction * impulse_magnitude * mass)
	set_thrown(false)