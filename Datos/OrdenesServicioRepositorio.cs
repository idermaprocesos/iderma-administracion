using System.Text.Json;
using IdermaAdministracion.Models;

namespace IdermaAdministracion.Datos;

public static class OrdenesServicioRepositorio
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly List<OrdenServicioRegistrada> Ordenes = [];
    private static bool _cargado;

    public static string RutaArchivo { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "IdermaCapilar",
        "ordenes-servicio.json");

    public static IReadOnlyList<OrdenServicioRegistrada> Listar()
    {
        Asegurar();
        return Ordenes
            .OrderByDescending(o => o.FechaRegistro)
            .ThenByDescending(o => o.Id)
            .ToList();
    }

    public static OrdenServicioRegistrada? Obtener(Guid id)
    {
        Asegurar();
        return Ordenes.FirstOrDefault(o => o.Id == id);
    }

    public static void Guardar(OrdenServicioRegistrada orden)
    {
        Asegurar();
        if (orden.Id == Guid.Empty)
            orden.Id = Guid.NewGuid();
        if (orden.FechaRegistro == default)
            orden.FechaRegistro = DateTimeOffset.Now;
        orden.Documento ??= new OrdenServicioDocumento();

        var indice = Ordenes.FindIndex(o => o.Id == orden.Id);
        if (indice >= 0)
            Ordenes[indice] = orden;
        else
            Ordenes.Add(orden);

        Persistir();
    }

    public static bool Eliminar(Guid id)
    {
        Asegurar();
        var quitadas = Ordenes.RemoveAll(o => o.Id == id);
        if (quitadas == 0)
            return false;

        Persistir();
        return true;
    }

    public static LineaOrdenServicio DesdeColaborador(Colaborador persona, int meses = 1) => new()
    {
        ColaboradorId = persona.Id,
        Nombre = persona.NombreCompleto,
        Documento = persona.Documento,
        Cargo = persona.Contrato.Cargo,
        Area = persona.Contrato.Area,
        NumeroContrato = persona.Contrato.NumeroContrato,
        TipoContrato = persona.Contrato.TipoContrato,
        Modalidad = persona.Contrato.ModalidadTexto,
        FechaFinActual = persona.Contrato.FechaFinContrato,
        SueldoMensual = persona.Contrato.SueldoMensual,
        MesesAmpliar = Math.Clamp(meses, 1, 36),
    };

    public static LineaOrdenServicio DesdeGuardada(LineaOrdenServicioExportacion linea) => new()
    {
        ColaboradorId = linea.ColaboradorId,
        Nombre = linea.Nombre,
        Documento = linea.Documento,
        Cargo = linea.Cargo,
        Area = linea.Area,
        NumeroContrato = linea.NumeroContrato,
        TipoContrato = linea.TipoContrato,
        Modalidad = linea.Modalidad,
        FechaFinActual = linea.FechaFinActual,
        SueldoMensual = (decimal)linea.SueldoMensual,
        MesesAmpliar = Math.Clamp(linea.Meses, 1, 36),
    };

    public static int AplicarAmpliacion(OrdenServicioDocumento documento)
    {
        var aplicadas = 0;
        foreach (var linea in documento.Lineas)
        {
            var persona = RepositorioEjemplo.Buscar(linea.ColaboradorId);
            if (persona is null || linea.Meses <= 0)
                continue;

            var inicio = persona.Contrato.FechaFinContrato?.Date ?? DateTime.Today;
            if (inicio < DateTime.Today)
                inicio = DateTime.Today;

            persona.Contrato.FechaFinContrato = inicio.AddMonths(linea.Meses);
            persona.Contrato.PlazoMeses = linea.Meses;
            var nota = $"Ampliado {linea.Meses} mes(es) hasta el {Formato.FechaLarga(persona.Contrato.FechaFinContrato.Value)}.";
            persona.Contrato.Observaciones = string.IsNullOrWhiteSpace(persona.Contrato.Observaciones)
                ? nota
                : $"{persona.Contrato.Observaciones.Trim()} {nota}";
            aplicadas++;
        }

        return aplicadas;
    }

    private static void Asegurar()
    {
        if (_cargado)
            return;

        _cargado = true;
        Directory.CreateDirectory(Path.GetDirectoryName(RutaArchivo)!);
        if (!File.Exists(RutaArchivo))
            return;

        try
        {
            var cargadas = JsonSerializer.Deserialize<List<OrdenServicioRegistrada>>(File.ReadAllText(RutaArchivo), Json);
            if (cargadas is not null)
                Ordenes.AddRange(cargadas.Where(o => o.Documento is not null));
        }
        catch
        {
            Ordenes.Clear();
        }
    }

    private static void Persistir()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RutaArchivo)!);
        File.WriteAllText(RutaArchivo, JsonSerializer.Serialize(Ordenes, Json));
    }
}
