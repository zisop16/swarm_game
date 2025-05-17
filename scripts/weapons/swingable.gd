class_name Swingable
extends Weapon

@onready var hitbox: Area3D = $Hitbox
@onready var trail: GPUTrail3D = $GPUTrail3D

var trail_pos: Vector3
var trail_length: int
var multi_hit: int = 1
func _ready() -> void:
	super._ready()
	hitbox.collision_layer = 0
	# These weapons collide with monsters on layer 3
	hitbox.collision_mask = Global.ENEMY_LAYER
	trail_pos = trail.position
	trail_length = trail.length
	deactivate_hitbox()

func attack():
	if Global.player.swinging:
		return
	Global.player.swing()
	trail.top_level = false
	trail.position = trail_pos
	trail.length = 0
	already_hit.clear()

func activate_hitbox():
	hitbox.monitoring = true
	trail.length = trail_length
	
func deactivate_hitbox():
	hitbox.monitoring = false
	trail.top_level = true

var already_hit: Array[Enemy] = []
func _physics_process(delta: float) -> void:
	super._physics_process(delta)
	if not hitbox.monitoring:
		return
	var overlap = hitbox.get_overlapping_bodies()
	var hit_count: int = 0
	for body in overlap:
		if body is Enemy:
			if body in already_hit:
				continue
			hit_count += 1
			var enemy := body as Enemy
			const sword_momentum: float = 7
			var impulse_direction := (Global.player.look_direction().slide(Vector3.UP) + Vector3.UP / 2).normalized()
			var sword_impulse := impulse_direction * sword_momentum
			apply_damage(enemy, sword_impulse)
			already_hit.append(body)
			if hit_count == multi_hit:
				break