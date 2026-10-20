namespace IdermaAdministracion.Models;

public sealed class PuestoTrabajo
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Area { get; set; } = "";
    public RolClinica Rol { get; set; } = RolClinica.Trabajador;
    public bool Archivado { get; set; }
    public DateTime? FechaCierre { get; set; }
    public string Funciones { get; set; } = "";
    public string Estudios { get; set; } = "";
    public string Responsabilidades { get; set; } = "";
    public decimal? SueldoDesde { get; set; }
    public decimal? SueldoHasta { get; set; }

    public string RolTexto => Rol == RolClinica.Doctor ? "Doctor" : "Trabajador";

    public bool TieneDetalle =>
        Funciones.Length > 0 ||
        Estudios.Length > 0 ||
        Responsabilidades.Length > 0 ||
        SueldoDesde is not null ||
        SueldoHasta is not null;

    public string FranjaSueldo
    {
        get
        {
            if (SueldoDesde is decimal desde && SueldoHasta is decimal hasta)
                return $"S/ {desde:N0} – S/ {hasta:N0}";
            if (SueldoDesde is decimal soloDesde)
                return $"Desde S/ {soloDesde:N0}";
            if (SueldoHasta is decimal soloHasta)
                return $"Hasta S/ {soloHasta:N0}";
            return "";
        }
    }
}
