using System.Collections.Generic;

public class Player
{
	public readonly List<Card> Cards = new List<Card>();
    public bool IsReady = false;
	public bool IsLocal = false;
	public string ConnectionId { get; set; }
}
