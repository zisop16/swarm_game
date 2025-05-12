class_name Axe
extends Weapon

func _ready() -> void:
	super._ready()
	id = ID.axe

var z_rot: float
var x_rot: float
var curve_rotate_velocity: float
func _physics_process(delta: float) -> void:
	super._physics_process(delta)
	if not thrown:
		return
	var in_air := get_colliding_bodies().is_empty()
	if in_air:
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
	else:
		set_thrown(false)


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
	var throw_velocity: Vector3 = throw_direction * throw_speed + Global.player.linear_velocity
	apply_impulse(throw_velocity * mass)
	set_thrown(true)
	z_rot = randf_range(-PI/5, PI/5)
	x_rot = 0
	curve_rotate_velocity = randf_range(-PI, PI)
	global_position = Global.player.cam.global_position + Global.player.look_direction() * .5
	
