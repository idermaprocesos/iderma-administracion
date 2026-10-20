using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace IdermaAdministracion.Models;

public static class CalculoOrdenServicio
{
    public const double TasaIgv = 0.18;
    public const string CodigoFormato = "FO0ADM016";
    public static readonly string[] EstadosDocumento = ["Borrador", "En revisión", "Aprobada"];

    public static double Redondear(double monto) =>
        Math.Round(monto, 2, MidpointRounding.AwayFromZero);

    public static (double SinIgv, double Igv) Desglosar(double montoConIgv)
    {
        if (montoConIgv <= 0 || double.IsNaN(montoConIgv) || double.IsInfinity(montoConIgv))
            return (0, 0);

        var igv = Redondear(montoConIgv * TasaIgv);
        return (Redondear(montoConIgv - igv), igv);
    }
}

public sealed class LineaOrdenServicio : INotifyPropertyChanged
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-PE");
    private double _mesesAmpliar = 1;

    public Guid Id { get; init; } = Guid.NewGuid();
    public string ColaboradorId { get; init; } = "";
    public string Nombre { get; init; } = "";
    public string Documento { get; init; } = "";
    public string Cargo { get; init; } = "";
    public string Area { get; init; } = "";
    public string NumeroContrato { get; init; } = "";
    public string TipoContrato { get; init; } = "";
    public string Modalidad { get; init; } = "";
    public DateTime? FechaFinActual { get; init; }
    public decimal SueldoMensual { get; init; }

    public double MesesAmpliar
    {
        get => _mesesAmpliar;
        set
        {
            var meses = double.IsNaN(value) || double.IsInfinity(value) ? 1 : Math.Clamp(value, 1, 36);
            meses = Math.Round(meses, 0, MidpointRounding.AwayFromZero);
            if (Math.Abs(_mesesAmpliar - meses) < 0.0001)
                return;

            _mesesAmpliar = meses;
            Avisar(nameof(MesesAmpliar));
            Avisar(nameof(TextoNuevaVigencia));
            Avisar(nameof(TextoSinIgv));
            Avisar(nameof(TextoIgv));
            Avisar(nameof(TextoTotal));
        }
    }

    public int Meses => (int)MesesAmpliar;

    public DateTime NuevaVigencia
    {
        get
        {
            var inicio = FechaFinActual?.Date ?? DateTime.Today;
            if (inicio < DateTime.Today)
                inicio = DateTime.Today;
            return inicio.AddMonths(Meses);
        }
    }

    public double ImporteConIgv => CalculoOrdenServicio.Redondear((double)SueldoMensual * Meses);
    public double ImporteIgv => CalculoOrdenServicio.Desglosar(ImporteConIgv).Igv;
    public double ImporteSinIgv => CalculoOrdenServicio.Desglosar(ImporteConIgv).SinIgv;

    public string TextoContrato => string.IsNullOrWhiteSpace(NumeroContrato) ? "Sin número" : NumeroContrato;
    public string TextoVence => FechaFinActual is null ? "Sin término" : Formato.FechaCorta(FechaFinActual.Value);
    public string TextoNuevaVigencia => Formato.FechaCorta(NuevaVigencia);
    public string TextoMensual => Formato.Dinero(SueldoMensual);
    public string TextoSinIgv => ImporteSinIgv.ToString("C", Cultura);
    public string TextoIgv => ImporteIgv.ToString("C", Cultura);
    public string TextoTotal => ImporteConIgv.ToString("C", Cultura);
    public string Resumen => $"{Cargo} · {Area} · {Modalidad}";

    public event PropertyChangedEventHandler? PropertyChanged;

    public LineaOrdenServicioExportacion Exportar() => new(
        Nombre,
        Documento,
        Cargo,
        Area,
        TextoContrato,
        TipoContrato,
        Modalidad,
        FechaFinActual,
        Meses,
        NuevaVigencia,
        (double)SueldoMensual,
        ImporteSinIgv,
        ImporteIgv,
        ImporteConIgv,
        ColaboradorId);

    private void Avisar([CallerMemberName] string? nombre = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}

public sealed record LineaOrdenServicioExportacion(
    string Nombre,
    string Documento,
    string Cargo,
    string Area,
    string NumeroContrato,
    string TipoContrato,
    string Modalidad,
    DateTime? FechaFinActual,
    int Meses,
    DateTime NuevaVigencia,
    double SueldoMensual,
    double SinIgv,
    double Igv,
    double TotalConIgv,
    string ColaboradorId);

public sealed class OrdenServicioDocumento
{
    public string CodigoFormato { get; set; } = CalculoOrdenServicio.CodigoFormato;
    public string MesAnio { get; set; } = "";
    public string FechaElaboracion { get; set; } = "";
    public string ElaboradoPor { get; set; } = "";
    public string RevisadoPor { get; set; } = "";
    public string PeriodoCobertura { get; set; } = "";
    public string Estado { get; set; } = "Borrador";
    public string Objeto { get; set; } = "Ampliación de contratos por meses";
    public List<LineaOrdenServicioExportacion> Lineas { get; set; } = [];
}

public sealed class OrdenServicioRegistrada
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset FechaRegistro { get; set; } = DateTimeOffset.Now;
    public bool AmpliacionAplicada { get; set; }
    public OrdenServicioDocumento Documento { get; set; } = new();
}

public sealed class OrdenServicioEdicion
{
    public Guid Id { get; init; }
    public DateTimeOffset FechaRegistro { get; init; }
    public bool AmpliacionAplicada { get; init; }
    public OrdenServicioDocumento? Documento { get; init; }
    public required IReadOnlyList<LineaOrdenServicio> Lineas { get; init; }
}
