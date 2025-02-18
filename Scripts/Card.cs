using Godot;

public partial class Card : Node2D
{
	[Export]
	public Sprite2D AsleepSprite;

	[Export]
	private RichTextLabel _healthText;

	private int _health = 0;
	public int Health 
	{
		get => _health;
		set => HealthChanged(value);
	}

	public bool _isAsleep = true;
	public bool IsAsleep
	{
		get => _isAsleep;
		set => AsleepChanged(value);	
	}

	public CardPlace Place;
	public string OwnerId = string.Empty;

	public override void _Ready()
	{
		
	}

	public void HealthChanged(int value)
	{
		_health = value;
		_healthText.Text = value.ToString();
	}
	
	public void AsleepChanged(bool value)
	{
		AsleepSprite.Visible = value;
		_isAsleep = value;
	}
}
