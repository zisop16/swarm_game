using Godot;
using System;
using System.Collections.Generic;

public partial class MeshContainer : Node3D
{
	[Export(PropertyHint.Range, "0, 10, .01")]
	double ErosionTime = 2;
	[Export(PropertyHint.Range, "0, 10, .01")]
	double ErosionDelay = 1.5f;
	List<SwappableMesh> Meshes = new List<SwappableMesh>();
	public override void _Ready() {
		foreach (Node child in GetChildren()) {
			SwappableMesh mesh = child as SwappableMesh;
			Meshes.Add(mesh);
			mesh.ErosionTime = ErosionTime;
			mesh.ErosionDelay = ErosionDelay;
		}
	}

	public void Erode() {
		foreach (SwappableMesh mesh in Meshes) {
			mesh.Erode();
		}
	}

	public void SetSwap(bool flag) {
		foreach (SwappableMesh mesh in Meshes) {
			mesh.SetSwap(flag);
		}
	}

	public bool FullyEroded() {
		return Meshes[0].FullyEroded();
	}
}
