
using Powergrid2.Utilities;

namespace Powergrid.PowerGrid;

public class Scenarios
{
    private readonly Grid pg;

    public Scenarios(Grid pg)
    {
        this.pg = pg;
    }


    private void MockFunction()
    {

    }

    



    //Möglichkeit lineare Gleichung um festzustellen welches Szenario abgespielt werden soll

    /* Mögliche Szenarien
    -Negative Frequenz
        -49,8 Einsatz positiver Regelenergie
        -49,7 Aktivierung von Leistungsreserven
        -49 Lastabwurf...
    -Positive Frequenz
        -50,1 Einsatz negativer Regelenergie
        -51,5 alle Solaranlagen gehen vom Netz

 */



}