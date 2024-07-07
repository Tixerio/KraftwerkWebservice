
using Powergrid2.Utilities;

public class Environment
{
    private int timePassed;
    private int day;
    public int MinutesPerTick { get; set; }
    public SunCycle SunCycle { get; set; } = new SunCycle(24 * 60);

    public Environment(int timePassed, int minutesPerTick)
    {
        this.timePassed = timePassed;
        this.MinutesPerTick = minutesPerTick;
        Time = DateTime.UnixEpoch.AddHours(timePassed);
        day = Time.Day;
        WindStrength = new Random().Next(1, 11);
        SunIntensity = SunCycle.GetValueOfTime(12*60 + Time.TimeOfDay.Minutes);

    }

    public int WindStrength { get; set; }
    public double SunIntensity { get; set; }
    public int Day { get; set; }

    public int TimePassed
    {
        get => timePassed;
        set
        {
            timePassed += value;
            Time = DateTime.UnixEpoch.AddMinutes(timePassed);
            day = Time.Day;
        }
    }


    public DateTime Time { get; set; }

    public void IncrementTime()
    {
        TimePassed = MinutesPerTick;
      
    }

    private void UpdateSunAndWind()
    {
        if (timePassed % 10 == 0)
        {
            Console.WriteLine("Wind: " + WindStrength + " | Sun: " + SunIntensity);
        }
        var newWindStrength = WindStrength + new Random().Next(-1, 2);
        WindStrength = newWindStrength <= 10 & newWindStrength >= 1 ? newWindStrength : newWindStrength < 1 ? 1 : 10;
        var newSunIntensity = WindStrength + new Random().Next(-1, 2);
        SunIntensity = Math.Round(
            SunCycle.GetValueOfTime(Time.TimeOfDay.Hours * 60 + Time.TimeOfDay.Minutes), 3);
    }

    public string ShowTime()
    {
        return (Time.TimeOfDay + " an Tag " + day);
    }

    public async Task DayCycle()
    {
        while (true)
        {
            await Task.Delay(1000);
          
            UpdateSunAndWind();
            TimePassed = 5;
        }
    }

    public TimeSpan GetTimeInTimeSpan()
    {
        return (Time.TimeOfDay);
    }
}