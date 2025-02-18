using Godot;
using NewGameProject.Scripts.Enums;
using System.Collections.Generic;
using Godot.Collections;
using System;
using System.Linq;

public partial class CardManager : Node2D
{
	private const int _positionOffset = 200;
	public readonly List<Card> Cards = new List<Card>();
	public readonly List<Card> PlacedCards = new List<Card>();

	MultiplayerStateManager _state;

	public GameState _gameState = GameState.SELECT_CARDS;

	private Card _selectedCard = null;
	private Node2D _selectedPlace = null;

	[Export]
	public PackedScene CardTemplate;

	[Export]
	public PackedScene CardPicture;

	[Export]	
	public Array<Node2D> CardPlaces = new Array<Node2D>();

	public override void _Ready()
	{
		_state = GetTree().Root.GetNode<MultiplayerStateManager>($"Node2D/{nameof(MultiplayerStateManager)}");

		foreach (var cardPlace in CardPlaces)
		{
			cardPlace.Visible = false;

			cardPlace.GetNode<Area2D>("Area2D").InputEvent += (caller, @event, shape) =>
			{
				if (!(bool)cardPlace.GetMeta("Clickable"))
				{
					return;
				}

				if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
				{
					var tween = GetTree().CreateTween();

					cardPlace.Modulate = new Color(2, 2, 2);

					if (_selectedPlace is not null)
					{
						_selectedPlace.Modulate = new Color(1, 1, 1);
					}

					_selectedPlace = cardPlace;
				}
			};
		}
	}

	public void CreateSelect()
	{
		for (int i = 0; i < 3; i++)
		{
			var card = CreateCard();

			card.GetNode<Sprite2D>("Shadow").Visible = false;
			card.Position = new Vector2(300 + (_positionOffset * i), 350);

			card.GetNode<Area2D>("Area2D").InputEvent += (caller, @event, shape) =>
			{
				if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
				{	
					if (_gameState == GameState.SELECT_CARDS)
					{
						Cards.Remove(card);
						card.QueueFree();

						_state.SetReady();

						_gameState = GameState.SELECTED_CARDS_WAITING_FOR_PLAYERS;
					}
					else
					{
						if (!_state.HasTurn())
						{
							return;
						}

						_selectedCard = card;

						if (GetCardPlaceIndex() != -1)
						{
							_state.PlaceCard(GetCardPlaceIndex());

							_selectedCard.QueueFree();

							_selectedCard = null;
							_selectedPlace = null;
						}
					}
				}
			};

			AddChild(card);

			Cards.Add(card);
		}
	}

	public void StartGame()
	{
		_gameState = GameState.GAME_STARTED;

		for(int i = 0; i < Cards.Count; i++)
		{
			Card card = Cards[i];
			Vector2 target = new Vector2(230 + (_positionOffset * i), 600);

			var targetColor = new Color(2, 2, 2);

			var backgroundTexture = GetTree().Root.GetNode<Sprite2D>("Node2D/Background");

			var tween = GetTree().CreateTween();

			tween.TweenProperty(backgroundTexture, "modulate", targetColor, 0.5f)
			  .SetTrans(Tween.TransitionType.Sine)
			  .SetEase(Tween.EaseType.InOut);

			tween.Parallel();

			tween.TweenProperty(card, "position", target, 0.5f).SetTrans(Tween.TransitionType.Sine).Finished += () =>
			{
				card.GetNode<Sprite2D>("Shadow").Visible = true;

				foreach (var cardPlace in CardPlaces)
				{
					cardPlace.Visible = true;
				}
			};
		}
	}

	public Card GetValidTarget(Card card, bool useEnemyOffset)
	{
		int cardIndex = CardPlaces.ToList().FindIndex(x => (x as CardPlace).PlacedCard == card);

		if (!useEnemyOffset)
		{
			cardIndex -= 3;
		}
		else
		{
			cardIndex += 3;
		}

		return (CardPlaces[cardIndex] as CardPlace).PlacedCard;
	}

	public int GetEnemyPlaceIndex(int index)
	{
		return index switch
		{
			0 => 3,
			1 => 4,
			2 => 5,
			_ => -1
		};
	}

	public void PlaceCard(int index, string ownerId)
	{
		Card card = CreateCard();
		CardPlace place = CardPlaces[index] as CardPlace;

		place.PlacedCard = card;
		card.Place = place;

		card.OwnerId = ownerId;
		card.Position = place.GlobalPosition;

		PlacedCards.Add(card);
	}
	
	public int GetCardPlaceIndex()
	{
		return CardPlaces.IndexOf(_selectedPlace);
	}

	public Card CreateCard()
	{
		var card = CardTemplate.Instantiate<Card>();
		
		card.Name = $"{Guid.NewGuid()}";

		var marker = card.GetNode<Marker2D>("pic");

		var picture = CardPicture.Instantiate<Sprite2D>();
		picture.Position = marker.Position + new Vector2(310, 315);
		
		card.AddChild(picture);
		GetTree().Root.GetNode("Node2D/Hand").AddChild(card);

		return card;
	}
}
