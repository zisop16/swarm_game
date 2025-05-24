using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;

public partial class Map : Node3D
{
	double SpawnRate = 3;
	int NumSpawns = 0;
	double WaveStartTime;
	

	public override void _Ready()
	{
		Global.Map = this;
		WaveStartTime = 0;
		for (int i = 0; i < NumNavThreads; i++)
		{
			Task curr = HandleNavigation(i);
			NavThreads[i] = curr;
		}
	}

	public static int NumNavThreads = 4;
	public int NavUpdatesThisFrame = 0;
	public static int MaxNavUpdates = 40;
	Task[] NavThreads = new Task[NumNavThreads];
	async Task HandleNavigation(int threadNum)
	{
		await Task.Run(() => Enemy.MassUpdateNavigation(threadNum));
	}

	public override void _PhysicsProcess(double delta)
	{
		NavUpdatesThisFrame = 0;
		for (int i = 0; i < NumNavThreads; i++)
		{
			Task curr = NavThreads[i];
			if (curr == null || curr.IsCompleted)
			{
				NavThreads[i] = HandleNavigation(i);
			}
		}
	}


	int TargetSpawns() {
		return (int)((Global.Time - WaveStartTime) * SpawnRate);
	}
}
