using Powergrid.PowerGrid;

namespace Powergrid.Utilities;

public class Sectors
{
    private int ReactionTime { get; set; } = 1;
    public int ActiveMinutes { get; set; }
    private double Frequency { get; set; }
    private int regulativeType = 1;
    private Random rnd = new();
    private Grid pg;

    public Sectors(Grid pg)
    {
        this.pg = pg;
    }




 
}