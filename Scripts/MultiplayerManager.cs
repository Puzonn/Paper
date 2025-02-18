using Godot;
using NewGameProject.Scripts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class MultiplayerManager : Node
{
	public static List<Client> Clients = new List<Client>();

	private bool _allow = true;

	public override async void _Ready()
	{
		await WaitSeconds(Random.Shared.Next(2 ) + (float)Random.Shared.NextDouble());

		var scene = ResourceLoader.Load<PackedScene>("res://Game.tscn");
		GetTree().ChangeSceneToPacked(scene);
	}

	private async Task WaitSeconds(float seconds)
	{
		await ToSignal(GetTree().CreateTimer(seconds), "timeout");
	}

	public override void _Process(double delta)
	{
		if (Input.IsKeyPressed(Key.F1) && _allow)
		{
			_allow = false;

			var scene = ResourceLoader.Load<PackedScene>("res://Game.tscn");
			GetTree().ChangeSceneToPacked(scene);
		}
	}
}
