public class Consumer : IMember
{
    public readonly double[] consumePercentDuringDayNight =
    [
        0.125, 0.1875, 0.25, 0.1875, 0.375, 0.5, 0.75, 0.875, 0.8125, 0.875, 1, 1, 0.625, 0.6875, 0.5, 0.375, 0.375, 0.75, 0.5,
        0.5, 0.625, 0.3125, 0.1875, 0.1875
    ];

    public int Hour { get; set; } = 0;
    public double energy;

    public virtual double Energy
    {
        get
        {
            Console.WriteLine(this.energy * consumePercentDuringDayNight[Hour]);
            return this.energy * consumePercentDuringDayNight[Hour];
        }
        set
        {
            this.energy = value;
        }
    }

    public string Name { get; set; }

    public Consumer(string name)
    {
        this.Energy = -15;
        this.Name = name;
    }


}