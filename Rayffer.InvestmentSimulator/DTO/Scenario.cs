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
    // Dinero total invertido
    public double DineroInvertido { get; set; }
    // Rendimiento financiero (interés neto)
    public double RendimientoFinanciero { get; set; }
    // Proporción del rendimiento respecto al dinero invertido (en %)
    public double ProporcionRendimiento => this.DineroInvertido != 0 ? (this.RendimientoFinanciero / this.DineroInvertido * 100) : 0;

    public string Resultado { get; set; }

    public void CalcularSimulacion()
    {
        var tasaHipotecaMensual = this.TasaInteresHipoteca / 100 / 12;
        var tasaInversionMensual = this.TasaInteresInversion / 100 / 12;
        var plazoMeses = this.PlazoHipoteca * 12;
        var cuota = this.CalcularCuotaMensual(this.PrincipalBase, tasaHipotecaMensual, plazoMeses);

        var opcion = this.OpcionAdelanto ? "Adelanto Hipoteca" :
                        this.OpcionInvertir ? "Invertir en Indexados" : "No hacer nada";

        if (this.OpcionAdelanto)
        {
            var sim = this.SimularHipotecaConExtra(this.PrincipalBase, tasaHipotecaMensual, plazoMeses, this.AporteExtraValor, cuota);
            var mesesPagados = sim.meses;
            var intereses = sim.intereses;
            var mesesInversion = (30 * 12) - mesesPagados;
            var fv = this.ValorFuturoMensual(cuota, tasaInversionMensual, mesesInversion);

            // Suponemos que el dinero invertido es la suma de la cuota invertida durante los meses de inversión
            this.DineroInvertido = cuota * mesesInversion;
            this.RendimientoFinanciero = fv - this.DineroInvertido;
            this.ValorInversion = fv;

            this.Resultado = $"Opción: {opcion}\n" +
                        $"Plazo efectivo: {mesesPagados / 12.0:F1} años\n" +
                        $"Intereses pagados: {intereses:F2} €\n" +
                        $"Valor acumulado invirtiendo la cuota liberada: {fv:F2} €\n" +
                        $"Dinero total invertido: {this.DineroInvertido:F2} €\n" +
                        $"Rendimiento financiero: {this.RendimientoFinanciero:F2} €\n" +
                        $"Proporción: {this.ProporcionRendimiento:F2}%";
        }
        else if (this.OpcionInvertir)
        {
            var totalPagado = cuota * plazoMeses;
            var intereses = totalPagado - this.PrincipalBase;
            var fvAnual = this.ValorFuturoAnual(this.AporteExtraValor, this.TasaInteresInversion / 100, this.PlazoHipoteca);
            var mesesRestantes = (30 - this.PlazoHipoteca) * 12;
            var fvMensual = this.ValorFuturoMensual(cuota, tasaInversionMensual, mesesRestantes);
            var totalFV = fvAnual * Math.Pow(1 + this.TasaInteresInversion / 100, 30 - this.PlazoHipoteca) + fvMensual;

            // El dinero invertido es la suma de los aportes extra durante el plazo de la hipoteca y la cuota invertida durante el resto
            this.DineroInvertido = (this.AporteExtraValor * this.PlazoHipoteca) + (cuota * mesesRestantes);
            this.RendimientoFinanciero = totalFV - this.DineroInvertido;
            this.ValorInversion = totalFV;

            this.Resultado = $"Opción: {opcion}\n" +
                        $"Plazo hipoteca: {this.PlazoHipoteca} años\n" +
                        $"Intereses pagados: {intereses:F2} €\n" +
                        $"Valor acumulado invirtiendo aportes extra y cuota liberada: {totalFV:F2} €\n" +
                        $"Dinero total invertido: {this.DineroInvertido:F2} €\n" +
                        $"Rendimiento financiero: {this.RendimientoFinanciero:F2} €\n" +
                        $"Proporción: {this.ProporcionRendimiento:F2}%";
        }
        else // OpcionNada
        {
            var totalPagado = cuota * plazoMeses;
            var intereses = totalPagado - this.PrincipalBase;
            this.DineroInvertido = 0;
            this.RendimientoFinanciero = 0;
            this.ValorInversion = 0;
            this.Resultado = $"Opción: {opcion}\n" +
                        $"Plazo hipoteca: {this.PlazoHipoteca} años\n" +
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
        var saldo = principal;
        double totalIntereses = 0;
        var mes = 0;
        while (saldo > 0 && mes < plazoMeses * 2)
        {
            mes++;
            var interes = saldo * tasaMensual;
            totalIntereses += interes;
            var abonoPrincipal = cuotaMensual - interes;
            if (abonoPrincipal > saldo)
                abonoPrincipal = saldo;
            saldo -= abonoPrincipal;
            if (mes % 12 == 0 && saldo > 0)
            {
                var abonoExtra = Math.Min(pagoExtraAnual, saldo);
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
        var opcion = this.OpcionAdelanto ? "Adelanto" : this.OpcionInvertir ? "Invertir" : "Nada";
        return $"Hipoteca: {this.PlazoHipoteca} años, Principal: {this.PrincipalBase}€, Opción: {opcion} (Inversión: {this.ValorInversion:F2} €)";
    }
}