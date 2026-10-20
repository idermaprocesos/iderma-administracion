namespace IdermaAdministracion.Models;

public enum EtapaContratacion
{
    Contacto = 0,
    Reunion = 1,
    Conocimientos = 2,
    PrimerDia = 3,
}

public sealed class OpcionResultado
{
    public required string Codigo { get; init; }
    public required string Texto { get; init; }
    public required bool Avanza { get; init; }
}

public sealed class RegistroPaso
{
    public required EtapaContratacion Etapa { get; init; }
    public required string Codigo { get; init; }
    public required string Texto { get; init; }
    public required bool Avanza { get; init; }
    public required DateTime Cuando { get; init; }
}

public static class PasosContratacion
{
    public static IReadOnlyList<EtapaContratacion> Orden { get; } =
    [
        EtapaContratacion.Contacto,
        EtapaContratacion.Reunion,
        EtapaContratacion.Conocimientos,
        EtapaContratacion.PrimerDia,
    ];

    public static string Titulo(EtapaContratacion etapa) => etapa switch
    {
        EtapaContratacion.Contacto => "Contacto",
        EtapaContratacion.Reunion => "Reunión",
        EtapaContratacion.Conocimientos => "Conocimientos",
        _ => "Primer día",
    };

    public static string Detalle(EtapaContratacion etapa) => etapa switch
    {
        EtapaContratacion.Contacto => "Contactarlo",
        EtapaContratacion.Reunion => "Agendar reunión",
        EtapaContratacion.Conocimientos => "Evaluación de conocimientos",
        _ => "Primer día laboral",
    };

    public static IReadOnlyList<OpcionResultado> Opciones(EtapaContratacion etapa) => etapa switch
    {
        EtapaContratacion.Contacto =>
        [
            new() { Codigo = "contactado", Texto = "Contestó y coincide con la oferta", Avanza = true },
            new() { Codigo = "no-contesto", Texto = "No contestó", Avanza = false },
            new() { Codigo = "no-coincide", Texto = "No coincide con la oferta", Avanza = false },
        ],
        EtapaContratacion.Reunion =>
        [
            new() { Codigo = "se-presento", Texto = "Se presentó", Avanza = true },
            new() { Codigo = "no-se-presento", Texto = "No se presentó", Avanza = false },
        ],
        EtapaContratacion.Conocimientos =>
        [
            new() { Codigo = "sobresaliente", Texto = "Sobresaliente", Avanza = true },
            new() { Codigo = "aprobo", Texto = "Aprobó", Avanza = true },
            new() { Codigo = "no-aprobo", Texto = "No aprobó", Avanza = false },
        ],
        _ =>
        [
            new() { Codigo = "asistio", Texto = "Se presentó", Avanza = true },
            new() { Codigo = "no-asistio", Texto = "No se presentó", Avanza = false },
        ],
    };
}

public sealed class Postulacion
{
    public required string Id { get; set; }
    public required string Nombres { get; set; }
    public required string Apellidos { get; set; }
    public required string Documento { get; set; }
    public required string Telefono { get; set; }
    public required string Correo { get; set; }
    public required string Puesto { get; set; }
    public required string Area { get; set; }
    public required RolClinica Rol { get; set; }
    public string Nota { get; set; } = "";
    public string? PersonalId { get; set; }
    public List<RegistroPaso> Pasos { get; } = [];

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
    public string RolTexto => Rol == RolClinica.Doctor ? "Doctor" : "Trabajador";
    public string PuestoArea => $"{Puesto} · {Area}";

    public RegistroPaso? De(EtapaContratacion etapa) => Pasos.FirstOrDefault(p => p.Etapa == etapa);

    public bool TieneCierreNegativo => Pasos.Any(p => !p.Avanza);

    public bool Contratado => Pasos.Any(p => p.Etapa == EtapaContratacion.PrimerDia && p.Avanza);

    public bool Cerrada => TieneCierreNegativo || Contratado;

    public EtapaContratacion? EtapaActual
    {
        get
        {
            if (Cerrada)
                return null;

            foreach (var etapa in PasosContratacion.Orden)
            {
                if (De(etapa) is null)
                    return etapa;
            }

            return null;
        }
    }

    public int PasosAvanzados => Pasos.Count(p => p.Avanza);

    public string EstadoTexto
    {
        get
        {
            if (Contratado)
                return "Contratado";

            var cierre = Pasos.LastOrDefault(p => !p.Avanza);
            if (cierre is not null)
                return cierre.Texto;

            if (Pasos.Count == 0)
                return "Por contactar";

            return "En proceso";
        }
    }

    public string ResumenLista
    {
        get
        {
            if (Contratado)
                return "Contratado";

            if (Pasos.LastOrDefault(p => !p.Avanza) is RegistroPaso cierre)
                return $"{PasosContratacion.Titulo(cierre.Etapa)} · {cierre.Texto}";

            if (EtapaActual is EtapaContratacion etapa)
                return Pasos.Count == 0
                    ? "Por contactar"
                    : $"En {PasosContratacion.Detalle(etapa).ToLowerInvariant()}";

            return EstadoTexto;
        }
    }

    public int IndiceProceso
    {
        get
        {
            if (Contratado)
                return 4;

            if (Pasos.LastOrDefault(p => !p.Avanza) is RegistroPaso cierre)
                return (int)cierre.Etapa;

            if (EtapaActual is EtapaContratacion etapa)
                return (int)etapa;

            return 0;
        }
    }
}
