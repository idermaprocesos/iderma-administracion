using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace IdermaAdministracion.Models;

public static class CalculoOrdenCompraAdmin
{
    public const double TasaIgv = 0.18;
    public const string CodigoFormato = "FO0ADM015";
    public static readonly string[] EstadosDocumento = ["Borrador", "En revisión", "Aprobada"];
    public static readonly string[] TiposPrecio = ["Contrato", "Promoción", "Estimado"];

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

public sealed class LineaOrdenCompraAdmin : INotifyPropertyChanged
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-PE");
    private string _descripcion = "";
    private string _unidad = "und";
    private string _tipoPrecio = "Estimado";
    private double _cantidad = 1;
    private double _precioConIgv;

    public Guid Id { get; init; } = Guid.NewGuid();

    public string Descripcion
    {
        get => _descripcion;
        set
        {
            _descripcion = value ?? "";
            Avisar();
        }
    }

    public string Unidad
    {
        get => _unidad;
        set
        {
            _unidad = string.IsNullOrWhiteSpace(value) ? "und" : value.Trim();
            Avisar();
        }
    }

    public string TipoPrecio
    {
        get => _tipoPrecio;
        set
        {
            _tipoPrecio = string.IsNullOrWhiteSpace(value) ? "Estimado" : value.Trim();
            Avisar();
        }
    }

    public IReadOnlyList<string> Tipos => CalculoOrdenCompraAdmin.TiposPrecio;

    public double Cantidad
    {
        get => _cantidad;
        set => FijarNumero(ref _cantidad, value, 0, 999999);
    }

    public double PrecioConIgv
    {
        get => _precioConIgv;
        set => FijarNumero(ref _precioConIgv, value, 0, 999999999);
    }

    public double ImporteConIgv => CalculoOrdenCompraAdmin.Redondear(PrecioConIgv * (Cantidad <= 0 ? 1 : Cantidad));
    public double ImporteIgv => CalculoOrdenCompraAdmin.Desglosar(ImporteConIgv).Igv;
    public double ImporteSinIgv => CalculoOrdenCompraAdmin.Desglosar(ImporteConIgv).SinIgv;
    public string TextoSinIgv => ImporteSinIgv.ToString("C", Cultura);
    public string TextoIgv => ImporteIgv.ToString("C", Cultura);
    public string TextoTotal => ImporteConIgv.ToString("C", Cultura);

    public event PropertyChangedEventHandler? PropertyChanged;

    public LineaOrdenCompraAdminExportacion Exportar() => new(
        Descripcion.Trim(),
        Cantidad,
        Unidad,
        PrecioConIgv,
        ImporteSinIgv,
        ImporteIgv,
        ImporteConIgv,
        TipoPrecio);

    private void FijarNumero(ref double campo, double valor, double minimo, double maximo, [CallerMemberName] string? nombre = null)
    {
        var numero = double.IsNaN(valor) || double.IsInfinity(valor) ? minimo : Math.Clamp(valor, minimo, maximo);
        if (Math.Abs(campo - numero) < 0.0000001)
            return;

        campo = numero;
        Avisar(nombre);
        Avisar(nameof(TextoSinIgv));
        Avisar(nameof(TextoIgv));
        Avisar(nameof(TextoTotal));
    }

    private void Avisar([CallerMemberName] string? nombre = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}

public sealed record LineaOrdenCompraAdminExportacion(
    string Descripcion,
    double Cantidad,
    string Unidad,
    double PrecioConIgv,
    double SinIgv,
    double Igv,
    double TotalConIgv,
    string TipoPrecio);

public sealed class OrdenCompraAdminDocumento
{
    public string CodigoFormato { get; set; } = CalculoOrdenCompraAdmin.CodigoFormato;
    public string MesAnio { get; set; } = "";
    public string FechaElaboracion { get; set; } = "";
    public string ElaboradoPor { get; set; } = "";
    public string RevisadoPor { get; set; } = "";
    public string PeriodoCobertura { get; set; } = "";
    public string Estado { get; set; } = "Borrador";
    public string Proveedor { get; set; } = "";
    public List<LineaOrdenCompraAdminExportacion> Lineas { get; set; } = [];
}

public sealed class OrdenCompraAdminRegistrada
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset FechaRegistro { get; set; } = DateTimeOffset.Now;
    public OrdenCompraAdminDocumento Documento { get; set; } = new();
}

public sealed class OrdenCompraAdminEdicion
{
    public Guid Id { get; init; }
    public DateTimeOffset FechaRegistro { get; init; }
    public OrdenCompraAdminDocumento? Documento { get; init; }
    public required IReadOnlyList<LineaOrdenCompraAdmin> Lineas { get; init; }
}
