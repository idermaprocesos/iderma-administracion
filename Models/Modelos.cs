using System.Globalization;

namespace IdermaAdministracion.Models;

public static class Formato
{
    private static readonly CultureInfo Pe = CultureInfo.GetCultureInfo("es-PE");

    public static string FechaLarga(DateTime fecha) =>
        fecha.ToString("dd 'de' MMMM 'de' yyyy", Pe);

    public static string FechaCorta(DateTime fecha) =>
        fecha.ToString("dd MMM yyyy", Pe);

    public static string Dinero(decimal monto) => monto.ToString("C", Pe);
}

public enum RolClinica
{
    Trabajador,
    Doctor,
}

public enum ModalidadPago
{
    Planilla,
    ReciboPorHonorarios,
}

public sealed class ContratoLaboral
{
    public required DateTime FechaIngreso { get; set; }
    public DateTime? FechaRetiro { get; set; }
    public string MotivoRetiro { get; set; } = "";
    public required string NumeroContrato { get; set; }
    public required string TipoContrato { get; set; }
    public DateTime? FechaFinContrato { get; set; }
    public int PlazoMeses { get; set; }
    public required string Cargo { get; set; }
    public required string Area { get; set; }
    public required string Sede { get; set; }
    public required string Jornada { get; set; }
    public required string Horario { get; set; }
    public List<HorarioDia> Dias { get; } = [];
    public required decimal SueldoMensual { get; set; }
    public required ModalidadPago Modalidad { get; set; }
    public required string CodigoPago { get; set; }
    public required string Banco { get; set; }
    public required string NumeroCuenta { get; set; }
    public required bool AceptaUsoImagen { get; set; }
    public DateTime? FechaConsentimientoImagen { get; set; }
    public DateTime? VencimientoImagen { get; set; }
    public string CanalesImagen { get; set; } = "—";
    public string RestriccionesImagen { get; set; } = "—";
    public string Observaciones { get; set; } = "";
    public List<DocumentoFirma> Documentos { get; } = [];

    public int FirmasHechas => Documentos.Count(d => d.Firmado);
    public string FirmasTexto => Documentos.Count == 0
        ? "Sin documentos"
        : $"Firmas {FirmasHechas} de {Documentos.Count}";
    public bool FirmasCompletas => Documentos.Count > 0 && FirmasHechas == Documentos.Count;

    public bool CuentaPendiente =>
        string.IsNullOrWhiteSpace(NumeroCuenta) ||
        NumeroCuenta.Contains("Pendiente", StringComparison.OrdinalIgnoreCase);

    public string SueldoTexto => Formato.Dinero(SueldoMensual);
    public string IngresoTexto => Formato.FechaLarga(FechaIngreso);
    public string IngresoCorto => Formato.FechaCorta(FechaIngreso);
    public string SeRetiroTexto => FechaRetiro is null
        ? "No"
        : $"Sí, el {Formato.FechaLarga(FechaRetiro.Value)}";
    public string MotivoRetiroTexto => string.IsNullOrWhiteSpace(MotivoRetiro) ? "No aplica" : MotivoRetiro;
    public string PlazoTexto => PlazoMeses switch
    {
        <= 0 => "Sin plazo fijo",
        1 => "1 mes",
        _ => $"{PlazoMeses} meses",
    };

    public bool TienePlazo => PlazoMeses > 0 && FechaFinContrato is not null;

    public int? DiasParaVencer =>
        !TienePlazo || FechaFinContrato is null || FechaRetiro is not null
            ? null
            : (FechaFinContrato.Value.Date - DateTime.Today).Days;

    public bool ProximoAVencer => DiasParaVencer is >= 0 and <= 30;
    public bool ContratoVencido => DiasParaVencer is < 0;

    public Microsoft.UI.Xaml.Visibility VisibilidadAlerta =>
        string.IsNullOrEmpty(AlertaVencimiento)
            ? Microsoft.UI.Xaml.Visibility.Collapsed
            : Microsoft.UI.Xaml.Visibility.Visible;

    public string AlertaVencimiento => DiasParaVencer switch
    {
        null => "",
        < 0 => DiasParaVencer == -1
            ? "El contrato venció ayer"
            : $"El contrato venció hace {-DiasParaVencer} días",
        0 => "El contrato vence hoy",
        1 => "El contrato vence mañana",
        <= 30 => $"El contrato vence en {DiasParaVencer} días",
        _ => "",
    };

    public string FinContratoTexto => FechaFinContrato is null
        ? "Sin fecha de término"
        : $"{PlazoTexto} · hasta el {Formato.FechaLarga(FechaFinContrato.Value)}";
    public string ModalidadTexto => Modalidad == ModalidadPago.Planilla ? "Planilla" : "Recibo por honorarios";
    public string PlanillaValor => Modalidad == ModalidadPago.Planilla ? $"Sí · {CodigoPago}" : "No";
    public string HonorariosValor => Modalidad == ModalidadPago.ReciboPorHonorarios ? $"Sí · {CodigoPago}" : "No";
    public string PlanillaEstado => DescribirModalidad(ModalidadPago.Planilla);
    public string HonorariosEstado => DescribirModalidad(ModalidadPago.ReciboPorHonorarios);
    public double OpacidadPlanilla => Modalidad == ModalidadPago.Planilla ? 1 : 0.48;
    public double OpacidadHonorarios => Modalidad == ModalidadPago.ReciboPorHonorarios ? 1 : 0.48;
    public string AceptaImagenTexto => AceptaUsoImagen
        ? "Sí, acepta el uso de su imagen"
        : "No acepta el uso de su imagen";
    public string AceptaImagenCorto => AceptaUsoImagen ? "Imagen sí" : "Imagen no";
    public string FechaConsentimientoTexto => FechaConsentimientoImagen is null
        ? "Sin registro"
        : Formato.FechaLarga(FechaConsentimientoImagen.Value);
    public string VencimientoImagenTexto => !AceptaUsoImagen
        ? "No aplica"
        : VencimientoImagen is null
            ? "Sin vencimiento registrado"
            : Formato.FechaLarga(VencimientoImagen.Value);
    public string CanalesTexto => !AceptaUsoImagen
        ? "No aplica"
        : string.IsNullOrWhiteSpace(CanalesImagen) ? "Sin canales registrados" : CanalesImagen;
    public string RestriccionesTexto => string.IsNullOrWhiteSpace(RestriccionesImagen)
        ? "Ninguna registrada"
        : RestriccionesImagen;
    public string CuentaTexto => CuentaPendiente ? "Pendiente de registrar" : NumeroCuenta;
    public string BancoTexto => CuentaPendiente ? "Pendiente" : Banco;

    private string DescribirModalidad(ModalidadPago modalidad)
    {
        if (Modalidad != modalidad)
            return "No aplica";

        return FechaRetiro is null ? "Modalidad vigente" : "Modalidad al momento del retiro";
    }
}

public sealed class Colaborador
{
    public required string Id { get; set; }
    public required string Nombres { get; set; }
    public required string Apellidos { get; set; }
    public required string Documento { get; set; }
    public required string Correo { get; set; }
    public required string Telefono { get; set; }
    public required RolClinica Rol { get; set; }
    public string Colegiatura { get; set; } = "";
    public string Especialidad { get; set; } = "";
    public required ContratoLaboral Contrato { get; set; }

    public string NombreCompleto => $"{Nombres} {Apellidos}";
    public bool EstaActivo => Contrato.FechaRetiro is null;
    public string EstadoTexto => EstaActivo ? "Activo" : "Retirado";
    public string RolTexto => Rol == RolClinica.Doctor ? "Doctor" : "Trabajador";
    public string AreaCargo => $"{Contrato.Cargo} · {Contrato.Area}";
    public string ColegiaturaTexto => string.IsNullOrWhiteSpace(Colegiatura) ? "—" : Colegiatura;
    public string EspecialidadTexto => string.IsNullOrWhiteSpace(Especialidad) ? "—" : Especialidad;

    public string SubtituloFicha => Rol == RolClinica.Doctor
        ? $"{Especialidad} · {Contrato.Sede}"
        : $"{Contrato.Cargo} · {Contrato.Area} · {Contrato.Sede}";

    public string FirmasTexto => Contrato.FirmasTexto;

    public string ResumenLista => EstaActivo
        ? $"Ingresó el {Contrato.IngresoCorto} · {Contrato.Sede} · {Contrato.AceptaImagenCorto}"
        : $"Retirado el {Formato.FechaCorta(Contrato.FechaRetiro!.Value)} · {Contrato.Sede} · {Contrato.AceptaImagenCorto}";

    public string ResumenContrato
    {
        get
        {
            var vinculo = EstaActivo
                ? $"Está activo desde el {Contrato.IngresoTexto}"
                : $"Se retiró el {Formato.FechaLarga(Contrato.FechaRetiro!.Value)}. Ingresó el {Contrato.IngresoTexto}";
            var pago = Contrato.Modalidad == ModalidadPago.Planilla
                ? $"Figura en planilla ({Contrato.CodigoPago}) con un sueldo de {Contrato.SueldoTexto}"
                : $"Cobra con recibo por honorarios ({Contrato.CodigoPago}) por {Contrato.SueldoTexto}";
            var imagen = Contrato.AceptaUsoImagen
                ? "Sí acepta el uso de su imagen para marketing"
                : "No acepta el uso de su imagen para marketing";
            return $"{vinculo}. {pago}. {imagen}.";
        }
    }
}

public sealed class DocumentoFirma
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public bool Firmado { get; set; }
    public DateTime? FechaFirma { get; set; }

    public string EstadoTexto => Firmado
        ? FechaFirma is null ? "Firmado" : $"Firmado el {Formato.FechaCorta(FechaFirma.Value)}"
        : "Pendiente";
}

public static class DocumentosPorFirmar
{
    public static IReadOnlyList<(string Codigo, string Nombre)> Catalogo { get; } =
    [
        ("laboral", "Contrato laboral"),
        ("imagen", "Uso de imagen"),
        ("confidencialidad", "Confidencialidad"),
        ("equipos", "Manejo de equipos"),
        ("reglamento", "Reglamento interno"),
        ("datos", "Protección de datos de pacientes"),
        ("bienes", "Entrega de bienes"),
    ];

    public static void Asegurar(ContratoLaboral contrato, bool conEjemplos)
    {
        if (contrato.Documentos.Count > 0)
            return;

        var semilla = 0;
        foreach (var caracter in contrato.NumeroContrato)
            semilla = semilla * 31 + caracter;
        semilla = Math.Abs(semilla);

        var indice = 0;
        foreach (var (codigo, nombre) in Catalogo)
        {
            var firmado = codigo == "imagen"
                ? contrato.AceptaUsoImagen
                : conEjemplos && (semilla + indice) % 4 != 0;
            contrato.Documentos.Add(new DocumentoFirma
            {
                Codigo = codigo,
                Nombre = nombre,
                Firmado = firmado,
                FechaFirma = firmado ? contrato.FechaIngreso.AddDays((semilla + indice * 11) % 40) : null,
            });
            indice++;
        }
    }

    public static void SincronizarImagen(ContratoLaboral contrato)
    {
        var imagen = contrato.Documentos.FirstOrDefault(d => d.Codigo == "imagen");
        if (imagen is null)
            return;

        imagen.Firmado = contrato.AceptaUsoImagen;
        imagen.FechaFirma = contrato.AceptaUsoImagen
            ? imagen.FechaFirma ?? contrato.FechaConsentimientoImagen ?? DateTime.Today
            : null;
    }
}

public sealed class Alerta
{
    public required string Tipo { get; set; }
    public required string Severidad { get; set; }
    public required string Titulo { get; set; }
    public required string Detalle { get; set; }
    public string? PersonaId { get; set; }
    public string? Seccion { get; set; }
    public string DestinoTexto => string.IsNullOrEmpty(PersonaId) ? "Abrir sección" : "Ver contrato";
}

public sealed class AsistenteReunion
{
    public required string Nombre { get; init; }
}

public sealed class CitaDia
{
    public string Id { get; set; } = "";
    public required DateTime Inicio { get; set; }
    public required string Asunto { get; set; }
    public required string Sala { get; set; }
    public required string Estado { get; set; }
    public List<string> Personas { get; set; } = [];

    public string Hora => Inicio.ToString("HH:mm", CultureInfo.InvariantCulture);
    public string FechaTexto => Formato.FechaCorta(Inicio);
    public string Cuando => $"{FechaTexto} · {Hora}";

    public string PersonasTexto => Personas.Count == 0
        ? "Sin personas"
        : string.Join(" · ", Personas);

    public string ConteoTexto => Personas.Count == 1
        ? "1 persona"
        : $"{Personas.Count} personas";
}

public sealed class Actividad
{
    public required string Momento { get; set; }
    public required string Texto { get; set; }
}

public sealed class Movimiento
{
    public required string Fecha { get; set; }
    public required string Concepto { get; set; }
    public required string Categoria { get; set; }
    public required decimal Monto { get; set; }
    public required bool EsIngreso { get; set; }
    public string MontoTexto => $"{(EsIngreso ? "+" : "−")} {Formato.Dinero(Monto)}";
    public string TipoTexto => EsIngreso ? "Ingreso" : "Egreso";
}

public sealed class Insumo
{
    public required string Nombre { get; set; }
    public required string Unidad { get; set; }
    public required double Stock { get; set; }
    public required double Minimo { get; set; }
    public required double Capacidad { get; set; }
    public string NivelTexto => Stock < Minimo ? "Bajo" : Stock < Minimo * 1.4 ? "Atención" : "Estable";
    public string StockTexto => $"{Stock:0} {Unidad} en stock · mínimo {Minimo:0}";
}

public sealed class Campana
{
    public required string Nombre { get; set; }
    public required string Canal { get; set; }
    public required string Estado { get; set; }
    public required string NotaConsentimiento { get; set; }
}

public sealed class SedeInfo
{
    public required string Nombre { get; set; }
    public required string Enfoque { get; set; }
    public required string Horario { get; set; }
    public required string PersonalTexto { get; set; }
    public required string Estado { get; set; }
}
