namespace Powergrid2.Utilities;

public class SineSquaredCurve
{
    private double currentAngle;
    private readonly double stepAngle;

    public SineSquaredCurve(int repeatAfterSteps)
    {
        this.currentAngle = 0;
        this.stepAngle = Math.PI / repeatAfterSteps;
    }

    public double NextValue()
    {
        double value = Math.Pow(Math.Sin(this.currentAngle), 2);
        this.currentAngle = (this.currentAngle + this.stepAngle) % (2 * Math.PI);
        return value;
    }
}

public class SunCycle
{
    private SineSquaredCurve sineSquaredCurve;
    private Random random;
    private List<double> cycleValues = new List<double>();

    public SunCycle(int repeatAfterSteps)
    {
        this.sineSquaredCurve = new SineSquaredCurve(repeatAfterSteps);
        this.random = new Random();
        GenerateValues();
    }

    private double SampleRandomDouble(double minVal, double maxVal)
    {
        return this.random.NextDouble() * (maxVal - minVal) + minVal;
    }

    private void GenerateValues()
    {
        for (int i = 0; i < 24 * 60; i++)
        {
            cycleValues.Add(GetNextNoisyPvValue(10));
        }
    }

    private double GetNextNoisyPvValue(double peakPower)
    {
        double noise = this.SampleRandomDouble(0, 1) > 0.85 ? this.SampleRandomDouble(-0.8, 0.0) : 0;
        double sineSquaredValue = this.sineSquaredCurve.NextValue();
        double noisySineSquaredValue = Math.Max(sineSquaredValue + noise, 0);
        double noisyPvValue = peakPower * noisySineSquaredValue;

        return noisyPvValue;
    }

    public double GetValueOfTime(int minutesPassed)
    {
        return cycleValues[minutesPassed];
    }
}