class_name SwappableMesh
extends MeshInstance3D

@export_range(0., 10.) var erosion_time: float = 2.
@export_range(0., 10.) var erosion_delay: float = 1.5
@export var swapped_mesh: Mesh = null
@onready var standard_mesh: Mesh = mesh

func _ready() -> void:
	assert(swapped_mesh != null, "Swappable Mesh wasn't assigned an alternate mesh")

func set_swap(flag: bool):
	if flag:
		mesh = swapped_mesh
	else:
		mesh = standard_mesh

var erosion_shader: ShaderMaterial
var eroding := false
var erosion_start_time: float
func erode():
	var eroding_mesh: Mesh = swapped_mesh.duplicate()
	erosion_shader = eroding_mesh.surface_get_material(0).duplicate()
	eroding_mesh.surface_set_material(0, erosion_shader)
	eroding = true
	erosion_start_time = Global.time + erosion_delay
	mesh = eroding_mesh

func fully_eroded() -> bool:
	return erosion_progress() >= 1

func erosion_progress() -> float:
	return (Global.time - erosion_start_time) / erosion_time

func _process(delta: float) -> void:
	if eroding:
		erosion_shader.set_shader_parameter("erosion_amount", erosion_progress())
