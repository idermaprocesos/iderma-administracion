using IdermaAdministracion.Models;

namespace IdermaAdministracion.Datos;

public static class PermisosDatos
{
    public static IReadOnlyList<string> AreasTrabajadores =>
        OrganigramaDatos.ConPersonas().Where(n => !n.EsClinica).Select(n => n.Titulo).ToList();

    public static IReadOnlyList<string> AreasClinica =>
        OrganigramaDatos.ConPersonas().Where(n => n.EsClinica).Select(n => n.Titulo).ToList();

    private static readonly List<PermisoAusencia> _permisos = CrearEjemplos();

    public static IReadOnlyList<PermisoAusencia> Todos => _permisos;

    public static IReadOnlyList<PermisoAusencia> De(string colaboradorId) =>
        _permisos.Where(p => p.ColaboradorId == colaboradorId).OrderByDescending(p => p.Desde).ToList();

    public static void Guardar(PermisoAusencia permiso)
    {
        var indice = _permisos.FindIndex(p => p.Id == permiso.Id);
        if (indice >= 0)
            _permisos[indice] = permiso;
        else
            _permisos.Insert(0, permiso);
    }

    public static void Eliminar(string id) => _permisos.RemoveAll(p => p.Id == id);

    private static List<PermisoAusencia> CrearEjemplos()
    {
        var lista = new List<PermisoAusencia>();
        var indice = 0;
        foreach (var persona in RepositorioEjemplo.Colaboradores)
        {
            lista.Add(PermisoDias(persona.Id, indice));
            lista.Add(PermisoHoras(persona.Id, indice));
            indice++;
        }

        return lista;
    }

    private static PermisoAusencia PermisoDias(string colaboradorId, int indice)
    {
        var motivo = (MotivoAusencia)(indice % 3);
        var desde = new DateTime(2026, 8, 3).AddDays(indice * 3);
        return new PermisoAusencia
        {
            Id = $"per-{colaboradorId}-d",
            ColaboradorId = colaboradorId,
            Tipo = TipoAusencia.Dias,
            Desde = desde,
            Hasta = desde.AddDays(indice % 3),
            Motivo = motivo,
            OtroMotivo = motivo == MotivoAusencia.Otros ? "Trámite personal" : "",
            Justificacion = motivo switch
            {
                MotivoAusencia.Salud => "Reposo indicado por consulta médica.",
                MotivoAusencia.FallecimientoFamiliar => "Ausencia por fallecimiento de un familiar directo.",
                _ => "Trámite personal que no puede hacerse fuera del horario.",
            },
            AceptadoJefe = true,
            AceptadoAdministracion = indice % 2 == 0,
            AceptadoRecursosHumanos = indice % 3 == 0,
            AceptadoGerencia = indice % 4 == 0,
        };
    }

    private static PermisoAusencia PermisoHoras(string colaboradorId, int indice)
    {
        var motivo = (MotivoAusencia)((indice + 1) % 3);
        var inicio = new TimeSpan(8 + indice % 4, 0, 0);
        return new PermisoAusencia
        {
            Id = $"per-{colaboradorId}-h",
            ColaboradorId = colaboradorId,
            Tipo = TipoAusencia.Horas,
            Desde = new DateTime(2026, 9, 1).AddDays(indice * 2),
            Hasta = new DateTime(2026, 9, 1).AddDays(indice * 2),
            HoraInicio = inicio,
            HoraFin = inicio.Add(TimeSpan.FromHours(2 + indice % 3)),
            Motivo = motivo,
            OtroMotivo = motivo == MotivoAusencia.Otros ? "Cita bancaria" : "",
            Justificacion = motivo switch
            {
                MotivoAusencia.Salud => "Cita médica en la mañana y retorno a la sede.",
                MotivoAusencia.FallecimientoFamiliar => "Gestión familiar durante parte de la jornada.",
                _ => "Diligencia personal de un par de horas.",
            },
            AceptadoJefe = true,
            AceptadoAdministracion = true,
            AceptadoRecursosHumanos = indice % 2 == 0,
            AceptadoGerencia = indice % 5 == 0,
        };
    }
}
