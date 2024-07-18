public class Powerplant : IMember
{
    public double Energy { get; } = 100;
    public string Name { get; set; }

    public double Produced { get; set; }

    public Powerplant(string name)
    {
        this.Name = name;
    }
}