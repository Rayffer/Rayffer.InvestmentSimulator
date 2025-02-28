using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Rayffer.InvestmentSimulator.DTO;
using Rayffer.InvestmentSimulator.Tools;

namespace Rayffer.InvestmentSimulator.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string FileName = "simulaciones.json";
    private bool isUpdating = false;

    [ObservableProperty]
    private double valorVivienda = 215500;

    [ObservableProperty]
    private double principalBase = 140000;

    [ObservableProperty]
    private double principalBasePorcentaje = (140000 / 215500.0) * 100;

    [ObservableProperty]
    private double tasaInteresHipoteca = 2.2; // % anual

    [ObservableProperty]
    private int plazoHipoteca = 20; // años

    [ObservableProperty]
    private double tasaInteresInversion = 5.0; // % anual

    [ObservableProperty]
    private bool opcionAdelanto = true;

    [ObservableProperty]
    private bool opcionInvertir;

    [ObservableProperty]
    private bool opcionNada;

    [ObservableProperty]
    private double aporteExtraValor = 6000;

    [ObservableProperty]
    private string resultadosSimulacion;

    public ObservableCollection<Scenario> EscenariosGuardados { get; } = new();

    [ObservableProperty]
    private Scenario escenarioSeleccionado;

    // Colección de opciones para el plazo (años)
    public ObservableCollection<int> PlazoOptions { get; } = new ObservableCollection<int> { 20, 30 };

    // Propiedad calculada para la tierlist: escenarios ordenados por ValorInversion descendente
    public IEnumerable<Scenario> Tierlist => this.EscenariosGuardados.OrderByDescending(s => s.ProporcionRendimiento);

    public MainViewModel()
    {
        this.LoadSimulacionesFromJson();
    }

    [RelayCommand]
    private void GuardarEscenario()
    {
        Scenario escenario = new Scenario
        {
            ValorVivienda = this.ValorVivienda,
            PrincipalBase = this.PrincipalBase,
            TasaInteresHipoteca = this.TasaInteresHipoteca,
            PlazoHipoteca = this.PlazoHipoteca,
            TasaInteresInversion = this.TasaInteresInversion,
            OpcionAdelanto = this.OpcionAdelanto,
            OpcionInvertir = this.OpcionInvertir,
            OpcionNada = this.OpcionNada,
            AporteExtraValor = this.AporteExtraValor
        };

        escenario.CalcularSimulacion();
        this.ResultadosSimulacion = escenario.Resultado;
        this.EscenariosGuardados.Add(escenario);
        this.OnPropertyChanged(nameof(this.Tierlist));
        this.SaveSimulacionesToJson();
    }

    [RelayCommand]
    private void EliminarEscenario()
    {
        if (this.EscenarioSeleccionado != null)
        {
            this.EscenariosGuardados.Remove(this.EscenarioSeleccionado);
            this.EscenarioSeleccionado = null;
            this.OnPropertyChanged(nameof(this.Tierlist));
            this.SaveSimulacionesToJson();
            // Opcional: Limpiar el resultado si se elimina el escenario mostrado.
            this.ResultadosSimulacion = string.Empty;
        }
    }

    partial void OnEscenarioSeleccionadoChanged(Scenario value)
    {
        if (value != null)
        {
            ResultadosSimulacion = value.Resultado;
        }
    }

    partial void OnPrincipalBaseChanged(double value)
    {
        if (isUpdating)
        {
            return;
        }

        try
        {
            isUpdating = true;
            PrincipalBasePorcentaje = (ValorVivienda != 0) ? value / ValorVivienda * 100 : 0;
        }
        finally
        {
            isUpdating = false;
        }
    }

    partial void OnPrincipalBasePorcentajeChanged(double value)
    {
        if (isUpdating)
        {
            return;
        }

        try
        {
            isUpdating = true;
            PrincipalBase = ValorVivienda * value / 100;
        }
        finally
        {
            isUpdating = false;
        }
    }

    private void SaveSimulacionesToJson()
    {
        JsonTools.WriteToJsonFile(FileName, this.EscenariosGuardados.ToList());
    }

    private void LoadSimulacionesFromJson()
    {
        if (File.Exists(FileName))
        {
            var escenarios = JsonTools.ReadFromJsonFile<List<Scenario>>(FileName);
            if (escenarios is not null)
            {
                this.EscenariosGuardados.Clear();
                foreach (var escenario in escenarios)
                {
                    this.EscenariosGuardados.Add(escenario);
                }
            }
        }
    }
}