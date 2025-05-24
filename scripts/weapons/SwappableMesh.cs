using Godot;
using System;
using System.Diagnostics;

public partial class SwappableMesh : MeshInstance3D
{

	public double ErosionTime;
	public double ErosionDelay;
	[Export]
	Mesh SwappedMesh = null;
	Mesh StandardMesh;

	public override void _Ready() {
		StandardMesh = Mesh;
		Debug.Assert(SwappedMesh != null, "Swappable Mesh wasn't assigned an alternate mesh");
	}

	public void SetSwap(bool flag) {
		if (flag) {
			Mesh = SwappedMesh;
		}
		else {
			Mesh = StandardMesh;
		}
	}

	ShaderMaterial ErosionShader;
	bool Eroding = false;
	double ErosionStartTime;
	public void Erode() {
		Mesh erodingMesh = (Mesh)SwappedMesh.Duplicate();
		ErosionShader = (ShaderMaterial)erodingMesh.SurfaceGetMaterial(0).Duplicate();
		erodingMesh.SurfaceSetMaterial(0, ErosionShader);
		Eroding = true;
		ErosionStartTime = Global.Time + ErosionDelay;
		Mesh = erodingMesh;
	}

	public bool FullyEroded() {
		return ErosionProgress() >= 1;
	}

	double ErosionProgress() {
		return (Global.Time - ErosionStartTime) / ErosionTime;
	}

	public override void _Process(double delta) {
		if (Eroding) {
			ErosionShader.SetShaderParameter("erosion_amount", ErosionProgress());
		}
	}

}
