extends Camera3D


var mouse_locked: bool
var local_pos := position
func _ready() -> void:
	set_mouse_lock(true)
	top_level = true

func set_mouse_lock(flag: bool):
	if flag:
		Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
	else:
		Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
	mouse_locked = flag

func _process(delta: float) -> void:
	if Input.is_action_just_pressed("MouseLock"):
		set_mouse_lock(not mouse_locked)
	global_position = get_parent().global_position + local_pos

const mouse_sensitivity = .0006
func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		rotation.y -= event.relative.x * mouse_sensitivity
		rotation.x -= event.relative.y * mouse_sensitivity
		rotation.y = wrapf(rotation.y, 0, 2*PI)
		rotation.x = clampf(rotation.x, -PI/2, PI/2)