using Godot;
using System;

public partial class Camera : Camera3D
{
	bool MouseLocked;
	Vector3 LocalPos;
	public override void _Ready() {
		SetMouseLock(true);
		LocalPos = Position;
		TopLevel = true;
	}

	void SetMouseLock(bool flag) {
		if (flag)
		{
			Input.SetMouseMode(Input.MouseModeEnum.Captured);
		}
		else
		{
			Input.SetMouseMode(Input.MouseModeEnum.Visible);
		}
		MouseLocked = flag;
	}

	public override void _Process(double delta) {
		if (Input.IsActionJustPressed("MouseLock")) {
			SetMouseLock(!MouseLocked);
		}
		GlobalPosition = ((Node3D)GetParent()).GlobalPosition + LocalPos;
	}

	float MouseSensitivity = .0006f;
	public override void _UnhandledInput(InputEvent ev) {
		InputEventMouseMotion mEv = ev as InputEventMouseMotion;
		if (mEv != null)
		{
			float newYRot = Rotation.Y - mEv.Relative.X * MouseSensitivity;
			newYRot = Mathf.Wrap(newYRot, 0, 2 * MathF.PI);
			float newXRot = Rotation.X - mEv.Relative.Y * MouseSensitivity;
			newXRot = Mathf.Clamp(newXRot, -MathF.PI / 2, MathF.PI / 2);
			Rotation = new Vector3(newXRot, newYRot, Rotation.Z);
		}
	}
}
