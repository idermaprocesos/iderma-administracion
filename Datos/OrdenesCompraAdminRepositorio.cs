using System.Text.Json;
using IdermaAdministracion.Models;

namespace IdermaAdministracion.Datos;

public static class OrdenesCompraAdminRepositorio
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly List<OrdenCompraAdminRegistrada> Ordenes = [];
    private static bool _cargado;

    public static string RutaArchivo { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "IdermaCapilar",
        "ordenes-compra-administracion.json");

    public static IReadOnlyList<OrdenCompraAdminRegistrada> Listar()
    {
        Asegurar();
        return Ordenes.OrderByDescending(o => o.FechaRegistro).ThenByDescending(o => o.Id).ToList();
    }

    public static OrdenCompraAdminRegistrada? Obtener(Guid id)
    {
        Asegurar();
        return Ordenes.FirstOrDefault(o => o.Id == id);
    }

    public static void Guardar(OrdenCompraAdminRegistrada orden)
    {
        Asegurar();
        if (orden.Id == Guid.Empty)
            orden.Id = Guid.NewGuid();
        if (orden.FechaRegistro == default)
            orden.FechaRegistro = DateTimeOffset.Now;
        orden.Documento ??= new OrdenCompraAdminDocumento();

        var indice = Ordenes.FindIndex(o => o.Id == orden.Id);
        if (indice >= 0)
            Ordenes[indice] = orden;
        else
            Ordenes.Add(orden);

        Directory.CreateDirectory(Path.GetDirectoryName(RutaArchivo)!);
        File.WriteAllText(RutaArchivo, JsonSerializer.Serialize(Ordenes, Json));
    }

    public static bool Eliminar(Guid id)
    {
        Asegurar();
        if (Ordenes.RemoveAll(o => o.Id == id) == 0)
            return false;

        File.WriteAllText(RutaArchivo, JsonSerializer.Serialize(Ordenes, Json));
        return true;
    }

    public static LineaOrdenCompraAdmin DesdeGuardada(LineaOrdenCompraAdminExportacion linea) => new()
    {
        Descripcion = linea.Descripcion,
        Cantidad = linea.Cantidad,
        Unidad = linea.Unidad,
        PrecioConIgv = linea.PrecioConIgv,
        TipoPrecio = linea.TipoPrecio,
    };

    private static void Asegurar()
    {
        if (_cargado)
            return;

        _cargado = true;
        if (!File.Exists(RutaArchivo))
            return;

        try
        {
            var cargadas = JsonSerializer.Deserialize<List<OrdenCompraAdminRegistrada>>(File.ReadAllText(RutaArchivo), Json);
            if (cargadas is not null)
                Ordenes.AddRange(cargadas.Where(o => o.Documento is not null));
        }
        catch
        {
            Ordenes.Clear();
        }
    }
}
