class_name OutlineableMesh
extends MeshInstance3D

@export var outline_mesh: Mesh = null
@onready var standard_mesh: Mesh = mesh

func _ready() -> void:
	assert(outline_mesh != null, "Outlineable Mesh wasn't assigned an outline mesh")

func set_outline(flag: bool):
	if flag:
		mesh = outline_mesh
	else:
		mesh = standard_mesh