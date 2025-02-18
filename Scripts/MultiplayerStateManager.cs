using Godot;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MultiplayerStateManager : Node
{
	public static MultiplayerStateManager Instance;

	[Export]
	private RichTextLabel DebugText;

	[Export]
	private Sprite2D TurnIndicatorSprite;

	[Export]
	private Sprite2D EndTurnSprite;

	public readonly List<Player> Players = new List<Player>();

	private Player _playerTurn;
	public Player PlayerTurn
	{
		get => _playerTurn;
		set => ChangeTurn(value);
	}

	public HubConnection Connection;
	private int _port = 5265;
	private string _address = "http://localhost";

	CardManager _cardManager;

	public override void _Ready()
	{
		TurnIndicatorSprite.Visible = false;
		EndTurnSprite.Visible = false;

		Join();
		_cardManager = GetTree().Root.GetNode<CardManager>("Node2D");

		EndTurnSprite.GetNode<Area2D>("Area2D").InputEvent += (caller, @event, shape) =>
		{
			if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
			{
				EndTurn();
			}
		};
	}

	private void ChangeTurn(Player player)
	{
		_playerTurn = player;

		if (HasTurn())
		{
			TurnIndicatorSprite.Visible = true;
			EndTurnSprite.Visible = true;
		}
		else
		{
			TurnIndicatorSprite.Visible = false;
			EndTurnSprite.Visible = false;
		}
	}

	public Player GetLocalPlayer()
	{
		return Players.Find(x => x.ConnectionId == Connection.ConnectionId);
	}

	public void AddCard(Card card, Player player)
	{
		player.Cards.Add(card);
	}

	public void Join()
	{
		Connection = new HubConnectionBuilder()
			.WithUrl($"{_address}:{_port}/paper")
			.Build();

		Connection.On<List<string>, string>("StartGame", (ids, playerTurn) =>
		{
			GD.Print($"Invoked: StartGame | {ids} , {playerTurn}");

			CallDeferred(nameof(StartGame), ids.ToArray(), playerTurn);
		});

		Connection.On<int, string>("PlaceCardCallback", (index, ownerId) =>
		{
			GD.Print($"Invoked: PlaceCardCallback | {index}");
			
			CallDeferred(nameof(PlaceCardCallback), index, ownerId);
		});

		Connection.On<string>("SetReadyCallback", (networkId) =>
		{
			GD.Print($"Invoked: SetReadyCallback | {networkId}");

			CallDeferred(nameof(SetReadyCallback), networkId);
		});

		Connection.On<string, string, bool>("UpdateTurn", (attacker, playerTurn, attackRound) =>
		{
			GD.Print($"Invoked: UpdateTurn | {playerTurn}");

			CallDeferred(nameof(UpdateTurn), attacker, playerTurn, attackRound);
		});

		Connection.On("WakeUpCards", () =>
		{
			GD.Print($"Invoked: WakeUpCards");

			CallDeferred(nameof(WakeUpCards));
		});

		Connection.StartAsync();
	}

	public void WakeUpCards()
	{
		foreach(var card in _cardManager.PlacedCards)
		{
			card.IsAsleep = false;
		}
	}

	public async void UpdateTurn(string attacker, string playerTurn, bool attackRound)
	{
		DebugText.Text = $"Attacker: {attacker.Substr(0, 5)}. Turn: {playerTurn.Substr(0, 5)}. \n IsAttackRound: {attackRound}";

		if (attackRound)
		{
			List<Card> attackerCards = _cardManager.PlacedCards.FindAll(x => x.OwnerId == attacker && !x.IsAsleep);
			bool useEnemyOffset = attacker == Connection.ConnectionId;

			GD.Print($"Attacker: {attacker} useEnemy: {useEnemyOffset}");

			foreach (var card in attackerCards)
			{
				Card target = _cardManager.GetValidTarget(card, useEnemyOffset);
				if (target == null) continue;

				var tween = GetTree().CreateTween(); 

				tween.TweenProperty(card, "global_position", target.GlobalPosition, 0.3f);
				await ToSignal(tween, "finished");

				target.Health -= 1;

				tween = GetTree().CreateTween();
				tween.TweenProperty(card, "global_position", card.Place.GlobalPosition, 0.3f);
				await ToSignal(tween, "finished");
			}
		}

		PlayerTurn = Players.Find(x => x.ConnectionId == playerTurn);
	}

	public void EndTurn()
	{
		Connection.SendAsync("EndTurn");
	}

	public void SetReady()
	{
		Connection.SendAsync("SetReady");
	}

	public void PlaceCard(int index)
	{
		Connection.SendAsync("PlaceCard", index);
	}

	public void PlaceCardCallback(int index, string ownerId)
	{
		if(ownerId != Connection.ConnectionId)
		{
			index = _cardManager.GetEnemyPlaceIndex(index);
		}

		_cardManager.PlaceCard(index, ownerId);
	}

	public void SetReadyCallback(string connectionId)
	{
		Players.Find(x => x.ConnectionId == connectionId).IsReady = true;

		if(Players.All(x => x.IsReady))
		{
			_cardManager.StartGame();
		}
	}

	public bool HasTurn()
	{
		return GetLocalPlayer() == PlayerTurn;
	}

	public void StartGame(string[] ids, string playerTurn)
	{
		foreach(var id in ids)
		{
			Players.Add(new Player()
			{
				ConnectionId = id,
				IsLocal = id == Connection.ConnectionId,
			});
		}

		PlayerTurn = Players.Find(x => x.ConnectionId == playerTurn);

		_cardManager.CreateSelect();
	}
}
