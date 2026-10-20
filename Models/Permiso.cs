namespace IdermaAdministracion.Models;

public enum TipoAusencia
{
    Horas,
    Dias,
}

public enum MotivoAusencia
{
    Salud,
    FallecimientoFamiliar,
    Otros,
}

public sealed class PermisoAusencia
{
    public required string Id { get; set; }
    public required string ColaboradorId { get; set; }
    public required TipoAusencia Tipo { get; set; }
    public required DateTime Desde { get; set; }
    public required DateTime Hasta { get; set; }
    public TimeSpan? HoraInicio { get; set; }
    public TimeSpan? HoraFin { get; set; }
    public required MotivoAusencia Motivo { get; set; }
    public string OtroMotivo { get; set; } = "";
    public string Justificacion { get; set; } = "";
    public bool AceptadoJefe { get; set; }
    public bool AceptadoAdministracion { get; set; }
    public bool AceptadoRecursosHumanos { get; set; }
    public bool AceptadoGerencia { get; set; }

    public string MotivoTexto => Motivo switch
    {
        MotivoAusencia.Salud => "Salud",
        MotivoAusencia.FallecimientoFamiliar => "Fallecimiento de familiar directo",
        _ => string.IsNullOrWhiteSpace(OtroMotivo) ? "Otros" : $"Otros · {OtroMotivo}",
    };

    public string CuandoTexto => Tipo == TipoAusencia.Horas
        ? $"{Formato.FechaCorta(Desde)} · {HoraInicio:hh\\:mm} a {HoraFin:hh\\:mm}"
        : Desde.Date == Hasta.Date
            ? Formato.FechaLarga(Desde)
            : $"{Formato.FechaCorta(Desde)} al {Formato.FechaCorta(Hasta)}";

    public string TipoTexto => Tipo == TipoAusencia.Horas ? "Por horas" : "Por días";

    public string AprobacionesTexto
    {
        get
        {
            var partes = new List<string>();
            if (AceptadoJefe) partes.Add("jefe directo");
            if (AceptadoAdministracion) partes.Add("administración");
            if (AceptadoRecursosHumanos) partes.Add("recursos humanos");
            if (AceptadoGerencia) partes.Add("gerencia");
            return partes.Count == 0 ? "Sin aceptaciones" : "Aceptado por " + string.Join(", ", partes);
        }
    }
}
