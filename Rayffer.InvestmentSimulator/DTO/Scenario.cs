using System;

namespace Rayffer.InvestmentSimulator.DTO;

public class Scenario
{
    public double ValorVivienda { get; set; }
    public double PrincipalBase { get; set; }
    public double TasaInteresHipoteca { get; set; } // % anual
    public int PlazoHipoteca { get; set; } // años
    public double TasaInteresInversion { get; set; } // % anual
    public bool OpcionAdelanto { get; set; }
    public bool OpcionInvertir { get; set; }
    public bool OpcionNada { get; set; }
    public double AporteExtraValor { get; set; } // aporte extra en €

    // Valor acumulado de la inversión (para la tierlist)
    public double ValorInversion { get; set; }

    // Para la salida extendida
    public string Resultado { get; set; }

    public void CalcularSimulacion()
    {
        double tasaHipotecaMensual = TasaInteresHipoteca / 100 / 12;
        double tasaInversionMensual = TasaInteresInversion / 100 / 12;
        int plazoMeses = PlazoHipoteca * 12;
        double cuota = CalcularCuotaMensual(PrincipalBase, tasaHipotecaMensual, plazoMeses);

        string opcion = OpcionAdelanto ? "Adelanto Hipoteca" :
                        OpcionInvertir ? "Invertir en Indexados" : "No hacer nada";

        if (OpcionAdelanto)
        {
            var sim = SimularHipotecaConExtra(PrincipalBase, tasaHipotecaMensual, plazoMeses, AporteExtraValor, cuota);
            int mesesPagados = sim.meses;
            double intereses = sim.intereses;
            int mesesInversion = (30 * 12) - mesesPagados;
            double fv = ValorFuturoMensual(cuota, tasaInversionMensual, mesesInversion);
            // El dinero efectivamente invertido es la suma de las aportaciones mensuales durante los meses de inversión.
            double dineroInvertido = cuota * mesesInversion;
            double rendimientoFinanciero = fv - dineroInvertido;

            ValorInversion = fv;

            Resultado = $"Opción: {opcion}\n" +
                        $"Plazo efectivo: {mesesPagados / 12.0:F1} años\n" +
                        $"Intereses pagados: {intereses:F2} €\n" +
                        $"Valor acumulado invirtiendo la cuota liberada: {fv:F2} €\n" +
                        $"Dinero total invertido: {dineroInvertido:F2} €\n" +
                        $"Rendimiento financiero (interés neto): {rendimientoFinanciero:F2} €";
        }
        else if (OpcionInvertir)
        {
            double totalPagado = cuota * plazoMeses;
            double intereses = totalPagado - PrincipalBase;
            double fvAnual = ValorFuturoAnual(AporteExtraValor, TasaInteresInversion / 100, PlazoHipoteca);
            int mesesRestantes = (30 - PlazoHipoteca) * 12;
            double fvMensual = ValorFuturoMensual(cuota, tasaInversionMensual, mesesRestantes);
            double fvAnualFinal = fvAnual * Math.Pow(1 + TasaInteresInversion / 100, 30 - PlazoHipoteca);
            double totalFV = fvAnualFinal + fvMensual;
            // En este caso, se invierte el aporte extra durante el plazo de la hipoteca y además se invierte la cuota liberada.
            double dineroInvertido = (AporteExtraValor * PlazoHipoteca) + (cuota * mesesRestantes);
            double rendimientoFinanciero = totalFV - dineroInvertido;

            ValorInversion = totalFV;

            Resultado = $"Opción: {opcion}\n" +
                        $"Plazo hipoteca: {PlazoHipoteca} años\n" +
                        $"Intereses pagados: {intereses:F2} €\n" +
                        $"Valor acumulado invirtiendo aportes extra y cuota liberada: {totalFV:F2} €\n" +
                        $"Dinero total invertido: {dineroInvertido:F2} €\n" +
                        $"Rendimiento financiero (interés neto): {rendimientoFinanciero:F2} €";
        }
        else // OpcionNada
        {
            double totalPagado = cuota * plazoMeses;
            double intereses = totalPagado - PrincipalBase;
            ValorInversion = 0;
            Resultado = $"Opción: {opcion}\n" +
                        $"Plazo hipoteca: {PlazoHipoteca} años\n" +
                        $"Intereses pagados: {intereses:F2} €\n" +
                        $"Sin inversión realizada, por lo que no hay rendimiento financiero.";
        }
    }

    private double CalcularCuotaMensual(double principal, double tasaMensual, int meses)
    {
        return principal * tasaMensual / (1 - Math.Pow(1 + tasaMensual, -meses));
    }

    private (int meses, double intereses) SimularHipotecaConExtra(double principal, double tasaMensual, int plazoMeses, double pagoExtraAnual, double cuotaMensual)
    {
        double saldo = principal;
        double totalIntereses = 0;
        int mes = 0;
        while (saldo > 0 && mes < plazoMeses * 2)
        {
            mes++;
            double interes = saldo * tasaMensual;
            totalIntereses += interes;
            double abonoPrincipal = cuotaMensual - interes;
            if (abonoPrincipal > saldo)
                abonoPrincipal = saldo;
            saldo -= abonoPrincipal;
            if (mes % 12 == 0 && saldo > 0)
            {
                double abonoExtra = Math.Min(pagoExtraAnual, saldo);
                saldo -= abonoExtra;
            }
        }
        return (mes, totalIntereses);
    }

    private double ValorFuturoMensual(double aporteMensual, double tasaMensual, int meses)
    {
        return aporteMensual * (Math.Pow(1 + tasaMensual, meses) - 1) / tasaMensual;
    }

    private double ValorFuturoAnual(double aporteAnual, double tasaAnual, int años)
    {
        return aporteAnual * (Math.Pow(1 + tasaAnual, años) - 1) / tasaAnual;
    }

    public override string ToString()
    {
        string opcion = OpcionAdelanto ? "Adelanto" : OpcionInvertir ? "Invertir" : "Nada";
        return $"Hipoteca: {PlazoHipoteca} años, Principal: {PrincipalBase}€, Opción: {opcion} (Inversión: {ValorInversion:F2} €)";
    }
}