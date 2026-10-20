namespace IdermaAdministracion.Models;

public sealed class PersonaOrganigrama
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Cargo { get; set; } = "";
}

public sealed class NodoOrganigrama
{
    public required string Id { get; init; }
    public required string Titulo { get; set; }
    public bool Asesoria { get; init; }
    public bool EsClinica { get; init; }
    public bool EsMedica { get; init; }
    public bool ApilarHijos { get; init; }
    public bool Agrupa { get; init; }
    public List<NodoOrganigrama> Hijos { get; } = [];
    public List<NodoOrganigrama> Compartidos { get; } = [];
    public List<PersonaOrganigrama> Personas { get; } = [];
}
