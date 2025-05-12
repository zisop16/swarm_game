class_name Mutant
extends CharacterBody3D

@onready var bone_sim: PhysicalBoneSimulator3D = %PhysicalBoneSimulator3D
@onready var physical_skeleton: Skeleton3D = bone_sim.get_parent()
@onready var animated_skeleton: Skeleton3D = %AnimatedSkeleton
var physics_bones: Array[PhysicalBone3D] = []
@export_category("Bone Spring Constants")
@export var linear_spring_stiffness: float = 1000
@export var linear_spring_damping: float = 10
@export var angular_spring_stiffness: float = 500
@export var angular_spring_damping: float = 20
# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	bone_sim.physical_bones_start_simulation()
	for child in bone_sim.get_children():
		if child is PhysicalBone3D:
			physics_bones.append(child as PhysicalBone3D)
	physical_skeleton.top_level = true

func apply_bone_forces():
	var delta := get_physics_process_delta_time()
	for bone in physics_bones:
		var id := bone.get_bone_id()
		var target_transform := animated_skeleton.global_transform * animated_skeleton.get_bone_global_pose(id)
		var current_transform := physical_skeleton.global_transform * physical_skeleton.get_bone_global_pose(id)
		var pos_diff := target_transform.origin - current_transform.origin
		var bone_force := hooke_spring_force(pos_diff, bone.linear_velocity, linear_spring_stiffness, linear_spring_damping)
		bone.linear_velocity += bone_force * delta
		var rotation_diff := target_transform.basis * current_transform.basis.inverse()
		var torque = hooke_spring_force(rotation_diff.get_euler(), bone.angular_velocity, angular_spring_stiffness, angular_spring_damping)
		bone.angular_velocity += torque * delta
		print(id, pos_diff)

func _physics_process(delta: float) -> void:
	animated_body_phys()
	# apply_bone_forces()

func animated_body_phys():
	var delta := get_physics_process_delta_time()
	if not is_on_floor():
		velocity += get_gravity() * delta

	move_and_slide()


func hooke_spring_force(displacement: Vector3, vel: Vector3, stiff: float, damp: float) -> Vector3:
	return stiff * displacement - damp * vel
