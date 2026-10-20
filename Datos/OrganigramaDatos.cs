using IdermaAdministracion.Models;

namespace IdermaAdministracion.Datos;

public static class OrganigramaDatos
{
    public static NodoOrganigrama Raiz { get; } = Armar();

    public static IReadOnlyList<string> Titulos { get; } = Aplanar(Raiz).Select(n => n.Titulo).ToList();

    public static IEnumerable<NodoOrganigrama> ConPersonas() => Aplanar(Raiz);

    public static bool EsClinica(string area) =>
        Buscar(area)?.EsClinica == true;

    public static bool EsMedica(string area) =>
        Buscar(area)?.EsMedica == true;

    public static void CargarDesdePersonal()
    {
        foreach (var nodo in ConPersonas())
        {
            nodo.Personas.Clear();
            foreach (var colaborador in RepositorioEjemplo.Colaboradores
                         .Where(c => c.EstaActivo && string.Equals(c.Contrato.Area, nodo.Titulo, StringComparison.OrdinalIgnoreCase))
                         .OrderBy(c => c.Apellidos)
                         .ThenBy(c => c.Nombres))
            {
                nodo.Personas.Add(new PersonaOrganigrama
                {
                    Id = colaborador.Id,
                    Nombre = colaborador.NombreCompleto,
                    Cargo = colaborador.Contrato.Cargo,
                });
            }
        }
    }

    public static void GuardarPersonas(NodoOrganigrama nodo, IEnumerable<PersonaOrganigrama> personas)
    {
        nodo.Personas.Clear();
        nodo.Personas.AddRange(personas.Where(p => !string.IsNullOrWhiteSpace(p.Nombre)));
    }

    private static NodoOrganigrama? Buscar(string area) =>
        Aplanar(Raiz).FirstOrDefault(n => string.Equals(n.Titulo, area, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<NodoOrganigrama> Aplanar(NodoOrganigrama nodo)
    {
        if (!nodo.Agrupa)
            yield return nodo;
        foreach (var hijo in nodo.Hijos)
        {
            foreach (var descendiente in Aplanar(hijo))
                yield return descendiente;
        }

        foreach (var compartido in nodo.Compartidos)
        {
            foreach (var descendiente in Aplanar(compartido))
                yield return descendiente;
        }
    }

    private static NodoOrganigrama Armar() =>
        Nodo("gerente", "Gerente general",
            Nodo("administrador", "Administrador general",
                Clinica("director", "Director médico"),
                Clinica("coordinador-operativo", "Coordinador operativo",
                    Clinica("supervisor-call", "Supervisor de call center"),
                    Medica("dermatologo", "Médico dermatólogo y estético"),
                    Medica("trasplante", "Médico de trasplante capilar"),
                    Clinica("enfermeria", "Encargado de enfermería",
                        Clinica("enfermera", "Enfermera"),
                        Clinica("tecnico", "Técnico en enfermería"),
                        Clinica("cosmiatra", "Cosmiatra")),
                    Clinica("limpieza", "Personal de limpieza y mantenimiento")),
                Nodo("rrhh", "Encargado de RRHH"),
                Nodo("logistica", "Encargado de logística y mantenimiento"),
                Nodo("asistente-contable", "Asistente contable"),
                Nodo("procesos", "Encargado de procesos y calidad")),
            Asesoria("contabilidad", "Contabilidad"),
            Nodo("marketing-ventas", "Jefe de marketing y ventas",
                Nodo("coordinador-marketing", "Coordinador de marketing",
                    Grupo("equipo-contenido",
                        [
                            Nodo("content-manager", "Content manager"),
                            Nodo("community", "Community manager"),
                        ],
                        Nodo("streamer", "Streamer videógrafo"),
                        Nodo("videografo", "Videógrafo"),
                        Nodo("disenador", "Diseñador gráfico"),
                        Nodo("content-creator", "Content creator")),
                    Nodo("trafficker", "Trafficker")),
                Nodo("supervisor-crm", "Supervisor CRM",
                    Nodo("analista-datos", "Asistente de analista de datos"),
                    Nodo("gestor-data", "Gestor de data comercial"),
                    Nodo("asesora-call", "Asesora call center"),
                    Nodo("cajera", "Cajera"),
                    Nodo("confirmadora", "Confirmadora CRM"),
                    Clinica("recepcionista", "Recepcionista"))));

    private static NodoOrganigrama Nodo(string id, string titulo, params NodoOrganigrama[] hijos) =>
        Crear(id, titulo, hijos);

    private static NodoOrganigrama Clinica(string id, string titulo, params NodoOrganigrama[] hijos) =>
        Crear(id, titulo, hijos, clinica: true);

    private static NodoOrganigrama Medica(string id, string titulo, params NodoOrganigrama[] hijos) =>
        Crear(id, titulo, hijos, clinica: true, medica: true);

    private static NodoOrganigrama Pila(string id, string titulo, params NodoOrganigrama[] hijos) =>
        Crear(id, titulo, hijos, clinica: true, apilar: true);

    private static NodoOrganigrama Asesoria(string id, string titulo) =>
        Crear(id, titulo, [], asesoria: true);

    private static NodoOrganigrama Grupo(string id, NodoOrganigrama[] jefes, params NodoOrganigrama[] compartidos)
    {
        var nodo = new NodoOrganigrama
        {
            Id = id,
            Titulo = "",
            Agrupa = true,
        };
        nodo.Hijos.AddRange(jefes);
        nodo.Compartidos.AddRange(compartidos);
        return nodo;
    }

    private static NodoOrganigrama Crear(
        string id,
        string titulo,
        NodoOrganigrama[] hijos,
        bool clinica = false,
        bool asesoria = false,
        bool medica = false,
        bool apilar = false)
    {
        var nodo = new NodoOrganigrama
        {
            Id = id,
            Titulo = titulo,
            EsClinica = clinica,
            Asesoria = asesoria,
            EsMedica = medica,
            ApilarHijos = apilar,
        };
        nodo.Hijos.AddRange(hijos);
        return nodo;
    }
}
