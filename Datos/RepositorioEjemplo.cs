using IdermaAdministracion.Models;

namespace IdermaAdministracion.Datos;

public static class RepositorioEjemplo
{
    private static readonly List<Colaborador> _colaboradores = CrearColaboradores();
    private static readonly List<Postulacion> _postulaciones = CrearPostulaciones();
    private static List<PuestoTrabajo>? _puestos;

    public static IReadOnlyList<Colaborador> Colaboradores => _colaboradores;
    public static IReadOnlyList<Postulacion> Postulaciones => _postulaciones;
    public static IReadOnlyList<PuestoTrabajo> Puestos
    {
        get
        {
            AsegurarPuestos();
            return _puestos!;
        }
    }

    public static IReadOnlyList<string> Areas => OrganigramaDatos.Titulos;

    public static Colaborador? Buscar(string? id) =>
        _colaboradores.FirstOrDefault(c => c.Id == id);

    public static Postulacion? BuscarPostulacion(string? id) =>
        _postulaciones.FirstOrDefault(p => p.Id == id);

    public static Colaborador CrearBorrador(RolClinica rol)
    {
        var esDoctor = rol == RolClinica.Doctor;
        return new Colaborador
        {
            Id = $"{(esDoctor ? "d" : "t")}-{Guid.NewGuid():N}"[..14],
            Nombres = "",
            Apellidos = "",
            Documento = "",
            Correo = "",
            Telefono = "",
            Rol = rol,
            Contrato = new ContratoLaboral
            {
                FechaIngreso = DateTime.Today,
                NumeroContrato = "",
                TipoContrato = esDoctor ? "Locación de servicios" : "Plazo indeterminado",
                Cargo = "",
                Area = esDoctor ? "Médico dermatólogo y estético" : "",
                Sede = esDoctor ? "Clínica" : "Oficina administrativa 1904",
                Jornada = esDoctor ? "Por procedimiento" : "Tiempo completo",
                Horario = "",
                SueldoMensual = 0,
                Modalidad = esDoctor ? ModalidadPago.ReciboPorHonorarios : ModalidadPago.Planilla,
                CodigoPago = "",
                Banco = "",
                NumeroCuenta = "",
                AceptaUsoImagen = false,
            },
        };
    }

    public static void GuardarColaborador(Colaborador persona, bool nueva)
    {
        HorarioSemana.Asegurar(persona.Contrato);
        DocumentosPorFirmar.Asegurar(persona.Contrato, conEjemplos: false);
        DocumentosPorFirmar.SincronizarImagen(persona.Contrato);
        if (nueva)
            _colaboradores.Add(persona);
        else if (_colaboradores.All(c => !ReferenceEquals(c, persona)))
        {
            var indice = _colaboradores.FindIndex(c => c.Id == persona.Id);
            if (indice >= 0)
                _colaboradores[indice] = persona;
            else
                _colaboradores.Add(persona);
        }
    }

    public static Colaborador AgregarEnArea(string nombres, string apellidos, string cargo, string area, RolClinica rol)
    {
        var persona = EjemploArea($"org-{Guid.NewGuid():N}"[..12], nombres, apellidos, area, cargo, rol == RolClinica.Doctor);
        _colaboradores.Add(persona);
        return persona;
    }

    public static void ReemplazarArea(string area, IReadOnlyList<PersonaOrganigrama> personas)
    {
        var conservados = personas.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
        _colaboradores.RemoveAll(c =>
            string.Equals(c.Contrato.Area, area, StringComparison.OrdinalIgnoreCase) &&
            !conservados.Contains(c.Id));

        foreach (var persona in personas)
        {
            var partes = persona.Nombre.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var nombres = partes.Length > 0 ? partes[0] : persona.Nombre;
            var apellidos = partes.Length > 1 ? partes[1] : "";
            var existente = _colaboradores.FirstOrDefault(c => c.Id == persona.Id);
            if (existente is null)
            {
                var nuevo = EjemploArea(
                    string.IsNullOrWhiteSpace(persona.Id) ? $"org-{Guid.NewGuid():N}"[..12] : persona.Id,
                    nombres,
                    apellidos,
                    area,
                    persona.Cargo,
                    OrganigramaDatos.EsMedica(area));
                persona.Id = nuevo.Id;
                _colaboradores.Add(nuevo);
                continue;
            }

            existente.Nombres = nombres;
            existente.Apellidos = apellidos;
            existente.Contrato.Cargo = persona.Cargo;
            existente.Contrato.Area = area;
        }
    }

    public static Postulacion CrearPostulacion() => new()
    {
        Id = $"p-{Guid.NewGuid():N}"[..12],
        Nombres = "",
        Apellidos = "",
        Documento = "",
        Telefono = "",
        Correo = "",
        Puesto = "",
        Area = Areas[0],
        Rol = RolClinica.Trabajador,
    };

    public static PuestoTrabajo? BuscarPuesto(string nombre) =>
        Puestos.FirstOrDefault(p => string.Equals(p.Nombre, nombre.Trim(), StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<PuestoTrabajo> PuestosAbiertos =>
        Puestos.Where(p => !p.Archivado).ToList();

    public static void RenombrarPuestoEnPostulaciones(string nombreAnterior, PuestoTrabajo puesto)
    {
        foreach (var postulacion in _postulaciones)
        {
            if (!string.Equals(postulacion.Puesto.Trim(), nombreAnterior.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            postulacion.Puesto = puesto.Nombre;
            postulacion.Area = puesto.Area;
            postulacion.Rol = puesto.Rol;
        }
    }

    public static void CerrarPuesto(PuestoTrabajo puesto)
    {
        puesto.Archivado = true;
        puesto.FechaCierre = DateTime.Now;
    }

    public static PuestoTrabajo RegistrarPuesto(string nombre, string area, RolClinica rol)
    {
        if (BuscarPuesto(nombre) is PuestoTrabajo existente)
            return existente;

        var puesto = new PuestoTrabajo
        {
            Id = $"puesto-{Guid.NewGuid():N}"[..16],
            Nombre = nombre.Trim(),
            Area = area,
            Rol = rol,
        };
        _puestos!.Add(puesto);
        return puesto;
    }

    private static void AsegurarPuestos()
    {
        if (_puestos is not null)
            return;

        _puestos = [];
        foreach (var postulacion in _postulaciones)
            RegistrarPuesto(postulacion.Puesto, postulacion.Area, postulacion.Rol);

        foreach (var puesto in _puestos)
            CompletarDetalleEjemplo(puesto);
    }

    private static void CompletarDetalleEjemplo(PuestoTrabajo puesto)
    {
        if (puesto.TieneDetalle)
            return;

        (string funciones, string estudios, string responsabilidades, decimal desde, decimal hasta) detalle = puesto.Nombre switch
        {
            "Asistente de recepción" => (
                "Recibe a los pacientes, confirma citas y orienta el ingreso a la clínica.",
                "Estudios técnicos o universitarios en administración, salud o carreras afines.",
                "Cuida la agenda del día, el registro de llegadas y la atención en recepción.",
                1500, 1800),
            "Auxiliar contable" => (
                "Registra comprobantes, apoya conciliaciones y ordena la documentación contable.",
                "Estudios técnicos o universitarios en contabilidad.",
                "Responde por el orden de los registros y la entrega oportuna de la información al área.",
                2200, 2800),
            "Chofer de reparto" => (
                "Traslada insumos y documentos entre la clínica y la oficina administrativa.",
                "Licencia de conducir vigente y secundaria completa.",
                "Responde por el vehículo, los tiempos de entrega y el cuidado de la carga.",
                1600, 2000),
            "Community manager" => (
                "Prepara y publica el contenido de las redes de Iderma Capilar y responde mensajes.",
                "Estudios en comunicación, marketing o experiencia equivalente.",
                "Cuida la imagen de la marca y el calendario de publicaciones.",
                2000, 2800),
            "Editora" => (
                "Edita videos e imágenes de los tratamientos y del material de la clínica.",
                "Estudios en comunicación audiovisual, diseño o experiencia comprobable en edición.",
                "Entrega las piezas en el plazo acordado y respeta la imagen de la clínica.",
                2200, 3200),
            "Enfermera" or "Enfermero" => (
                "Asiste al médico en consulta y prepara el consultorio entre pacientes.",
                "Título de enfermería y habilitación vigente.",
                "Responde por la preparación del paciente, el material y el registro de la atención.",
                2500, 3500),
            "Médico estético" => (
                "Evalúa y realiza los tratamientos estéticos indicados en la clínica.",
                "Título de medicina, especialidad o capacitación en medicina estética y colegiatura vigente.",
                "Responde por la indicación, la ejecución del tratamiento y el seguimiento del paciente.",
                6000, 9000),
            _ => ("", "", "", 0, 0),
        };

        if (detalle.funciones.Length == 0)
            return;

        puesto.Funciones = detalle.funciones;
        puesto.Estudios = detalle.estudios;
        puesto.Responsabilidades = detalle.responsabilidades;
        puesto.SueldoDesde = detalle.desde;
        puesto.SueldoHasta = detalle.hasta;
    }

    public static void GuardarPostulacion(Postulacion postulacion, bool nueva)
    {
        RegistrarPuesto(postulacion.Puesto, postulacion.Area, postulacion.Rol);
        if (nueva)
            _postulaciones.Insert(0, postulacion);
    }

    public static void EliminarPostulacion(string id) =>
        _postulaciones.RemoveAll(p => p.Id == id);

    public static void GuardarResultado(Postulacion postulacion, RegistroPaso paso)
    {
        postulacion.Pasos.RemoveAll(p => p.Etapa == paso.Etapa);
        if (!paso.Avanza)
            postulacion.Pasos.RemoveAll(p => p.Etapa > paso.Etapa);

        postulacion.Pasos.Add(paso);
        postulacion.Pasos.Sort((a, b) => a.Etapa.CompareTo(b.Etapa));
    }

    public static void UbicarProceso(Postulacion postulacion, int indice)
    {
        if (indice >= PasosContratacion.Orden.Count)
        {
            foreach (var etapa in PasosContratacion.Orden)
                AsegurarAvance(postulacion, etapa);
            return;
        }

        postulacion.Pasos.RemoveAll(p => (int)p.Etapa >= indice);
        for (var i = 0; i < indice; i++)
            AsegurarAvance(postulacion, PasosContratacion.Orden[i]);
        postulacion.Pasos.Sort((a, b) => a.Etapa.CompareTo(b.Etapa));
    }

    private static void AsegurarAvance(Postulacion postulacion, EtapaContratacion etapa)
    {
        if (postulacion.De(etapa) is { Avanza: true })
            return;

        var opcion = PasosContratacion.Opciones(etapa).First(o => o.Avanza);
        postulacion.Pasos.RemoveAll(p => p.Etapa == etapa);
        postulacion.Pasos.Add(new RegistroPaso
        {
            Etapa = etapa,
            Codigo = opcion.Codigo,
            Texto = opcion.Texto,
            Avanza = true,
            Cuando = DateTime.Today,
        });
    }

    private static List<Colaborador> CrearColaboradores()
    {
        var lista = new List<Colaborador>
        {
        EjemploArea("t-maribel", "Maribel", "Dávila Zamolloa", "Gerente general", "Gerente general", false),
        EjemploArea("t-renato", "Renato", "Salazar Vidal", "Administrador general", "Administrador general", false),
        Persona(
            "t-lucia", "Lucía", "Fernández Quispe", "DNI 70111221", "lucia.fernandez@iderma.ejemplo", "999 100 201",
            RolClinica.Trabajador, "", "",
            Contrato(new(2026, 4, 14), 2200, "Recepcionista", "Recepcionista", "Clínica", ModalidadPago.Planilla, "PL-2026-01",
                true, "Instagram, web y sala de espera", "Renovar antes de usarla en la campaña de cierre de año.",
                "Plazo fijo", "Tiempo completo", "Lun a sáb · 8:00 a 17:00", "BCP", "**** 4412", "CT-2022-014",
                "Contrato de 6 meses. Revisar la renovación al vencer.",
                consentimiento: new(2026, 4, 14), venceImagen: new(2026, 10, 14), plazoMeses: 6)),
        Persona(
            "t-marco", "Marco Antonio", "Ruiz Delgado", "DNI 70883402", "marco.ruiz@iderma.ejemplo", "999 100 202",
            RolClinica.Trabajador, "", "",
            Contrato(new(2026, 6, 1), 2800, "Enfermera", "Enfermero asistencial", "Clínica", ModalidadPago.Planilla, "PL-2026-02",
                false, "—", "No desea aparecer en redes, web ni material de sala.",
                "Plazo fijo", "Por turnos", "Turnos rotativos · 8 horas", "Interbank", "**** 9021", "CT-2023-003",
                "Contrato de 3 meses ya vencido. Falta decidir la renovación.",
                plazoMeses: 3)),
        Persona(
            "t-camila", "Camila", "Torres Vega", "DNI 71229018", "camila.torres@iderma.ejemplo", "999 100 203",
            RolClinica.Trabajador, "", "",
            Contrato(new(2026, 7, 20), 3400, "Content manager", "Content manager", "Oficina administrativa 1904", ModalidadPago.ReciboPorHonorarios, "RH-E001-184",
                true, "Redes, web, pauta y vocería", "Puede aparecer como vocera de la clínica.",
                "Plazo fijo", "Medio tiempo", "Lun a vie · 10:00 a 15:00", "BBVA", "**** 1180", "CT-2024-031",
                "Contrato de 3 meses próximo a vencer.",
                consentimiento: new(2026, 7, 20), venceImagen: new(2026, 10, 20), plazoMeses: 3)),
        Persona(
            "t-jose", "José Luis", "Paredes Huamán", "DNI 44512098", "jose.paredes@iderma.ejemplo", "999 100 204",
            RolClinica.Trabajador, "", "",
            Contrato(new(2021, 2, 2), 1900, "Encargado de logística y mantenimiento", "Auxiliar de mantenimiento", "Oficina administrativa 1904", ModalidadPago.Planilla, "PL-2025-06",
                false, "—", "No autorizó el uso de imagen.",
                "Plazo indeterminado", "Tiempo completo", "Lun a sáb · 7:00 a 16:00", "BCP", "**** 3308", "CT-2021-009",
                "Renuncia voluntaria. La liquidación de ejemplo figura cerrada en junio de 2025.",
                retiro: new(2025, 6, 30), motivoRetiro: "Renuncia voluntaria")),
        Persona(
            "t-andrea", "Andrea", "Salazar Mendoza", "DNI 46770112", "andrea.salazar@iderma.ejemplo", "999 100 205",
            RolClinica.Trabajador, "", "",
            Contrato(new(2020, 11, 9), 4100, "Contabilidad", "Contadora", "Oficina administrativa 1904", ModalidadPago.Planilla, "PL-2026-03",
                true, "Memoria institucional y web interna", "No cubre pauta pagada ni anuncios en redes.",
                "Plazo indeterminado", "Tiempo completo", "Lun a vie · 9:00 a 18:00", "BCP", "**** 2204", "CT-2020-002",
                "Lleva el control de planilla y de recibos por honorarios de la sede.",
                consentimiento: new(2024, 1, 15), venceImagen: new(2027, 1, 15))),
        Persona(
            "t-diego", "Diego", "Navarro Castillo", "DNI 72330045", "diego.navarro@iderma.ejemplo", "999 100 206",
            RolClinica.Trabajador, "", "",
            Contrato(new(2026, 1, 15), 2600, "Supervisor CRM", "Supervisor CRM", "Oficina administrativa 1904", ModalidadPago.ReciboPorHonorarios, "RH-E001-203",
                true, "Web y material interno de la sede", "Solo con identificación de la oficina.",
                "Plazo fijo", "Tiempo completo", "Lun a vie · 9:00 a 18:00", "Pendiente", "Pendiente", "CT-2026-004",
                "Contrato de 12 meses. La cuenta bancaria sigue pendiente.",
                consentimiento: new(2026, 1, 15), venceImagen: new(2027, 1, 15), plazoMeses: 12)),
        Persona(
            "t-rosa", "Rosa Elena", "Gutiérrez León", "DNI 40199820", "rosa.gutierrez@iderma.ejemplo", "999 100 207",
            RolClinica.Trabajador, "", "",
            Contrato(new(2019, 5, 21), 1700, "Personal de limpieza y mantenimiento", "Auxiliar de limpieza", "Oficina administrativa 1904", ModalidadPago.Planilla, "PL-2026-04",
                false, "—", "No desea aparecer en redes ni en material público.",
                "Plazo indeterminado", "Tiempo completo", "Lun a sáb · 7:00 a 15:00", "BanBif", "**** 7730", "CT-2019-021",
                "Personal de limpieza clínica y bioseguridad de cabinas.")),
        Persona(
            "d-valeria", "Valeria", "Campos Ríos", "DNI 41992010", "valeria.campos@iderma.ejemplo", "999 200 301",
            RolClinica.Doctor, "CMP 45821 · RNE 31880", "Tricología médica",
            Contrato(new(2026, 8, 1), 12500, "Médico dermatólogo y estético", "Médica tricóloga", "Clínica", ModalidadPago.ReciboPorHonorarios, "RH-E001-010",
                true, "Web, charlas, redes y sala de espera", "Vocera médica de la clínica.",
                "Plazo fijo", "Por procedimiento", "Agenda médica · lun a vie", "BCP", "**** 8801", "CT-2018-001",
                "Contrato de 6 meses.",
                consentimiento: new(2026, 8, 1), venceImagen: new(2027, 2, 1), plazoMeses: 6)),
        Persona(
            "d-hernan", "Hernán", "Delgado Soto", "DNI 42881033", "hernan.delgado@iderma.ejemplo", "999 200 302",
            RolClinica.Doctor, "CMP 39210 · RNE 27741", "Cirugía capilar",
            Contrato(new(2019, 9, 11), 18000, "Médico de trasplante capilar", "Cirujano capilar", "Clínica", ModalidadPago.ReciboPorHonorarios, "RH-E001-011",
                true, "Casos clínicos, web y redes", "El rostro del paciente solo se muestra si el paciente también autoriza.",
                "Locación de servicios", "Por procedimiento", "Quirófano · mar, jue y sáb", "Interbank", "**** 6619", "CT-2019-011",
                "Uso de imagen permitido en casos clínicos con el consentimiento del paciente.",
                consentimiento: new(2025, 9, 1), venceImagen: new(2027, 9, 1))),
        Persona(
            "d-mariana", "Mariana", "Ortiz León", "DNI 45501928", "mariana.ortiz@iderma.ejemplo", "999 200 303",
            RolClinica.Doctor, "CMP 51002 · RNE 40112", "Dermatología",
            Contrato(new(2025, 11, 10), 9800, "Médico dermatólogo y estético", "Dermatóloga", "Clínica", ModalidadPago.Planilla, "PL-2026-09",
                false, "Credencial interna", "No autoriza redes ni pauta. Solo la credencial de la sede.",
                "Plazo fijo", "Tiempo completo", "Lun a vie · 9:00 a 18:00", "BBVA", "**** 5520", "CT-2021-018",
                "Contrato de 12 meses próximo a vencer.",
                plazoMeses: 12)),
        Persona(
            "d-pablo", "Pablo", "Iglesias Marín", "DNI 40221176", "pablo.iglesias@iderma.ejemplo", "999 200 304",
            RolClinica.Doctor, "CMP 28744", "Medicina estética",
            Contrato(new(2020, 3, 1), 8000, "Médico dermatólogo y estético", "Médico estético", "Clínica", ModalidadPago.ReciboPorHonorarios, "RH-E001-044",
                true, "Web y redes, solo durante el vínculo", "La autorización no continúa después del retiro.",
                "Locación de servicios", "Por procedimiento", "Agenda cerrada", "BCP", "**** 1044", "CT-2020-016",
                "Vínculo concluido. No programar agenda ni piezas nuevas con su imagen.",
                retiro: new(2024, 12, 31), motivoRetiro: "Fin de vínculo",
                consentimiento: new(2023, 3, 1), venceImagen: new(2024, 12, 31))),
        };
        lista.AddRange(
        [
            EjemploArea("t-beatriz", "Beatriz", "López Sandoval", "Administrador general", "Jefa de administración", false),
            EjemploArea("t-gonzalo", "Gonzalo", "Peña Ruiz", "Administrador general", "Asistente de administración", false),
            EjemploArea("t-adriana", "Adriana", "Vega Morales", "Coordinador operativo", "Coordinadora operativa", false),
            EjemploArea("t-martin", "Martín", "Castro León", "Encargado de enfermería", "Encargado de enfermería", false),
            EjemploArea("t-carmen", "Carmen", "Aguilar Rojas", "Confirmadora CRM", "Confirmadora de citas", false),
            EjemploArea("t-renzo", "Renzo", "Paredes Díaz", "Asistente de analista de datos", "Analista de datos", false),
            EjemploArea("t-fiorella", "Fiorella", "Campos Núñez", "Recepcionista", "Recepcionista", false),
            EjemploArea("t-jorge-s", "Jorge", "Salinas Ortiz", "Recepcionista", "Anfitrión de recepción", false),
            EjemploArea("t-milagros", "Milagros", "Quispe León", "Asesora call center", "Asesora de call center", false),
            EjemploArea("t-bruno", "Bruno", "Vidal Sosa", "Community manager", "Community manager", false),
            EjemploArea("t-natalia", "Natalia", "Herrera Cano", "Diseñador gráfico", "Diseñadora gráfica", false),
            EjemploArea("t-patricia", "Patricia", "Molina Reyes", "Encargado de procesos y calidad", "Analista de calidad", false),
            EjemploArea("t-andres", "Andrés", "Cueva Ramos", "Encargado de logística y mantenimiento", "Coordinador de almacén", false),
            EjemploArea("t-silvia", "Silvia", "Benavides Luna", "Encargado de logística y mantenimiento", "Encargada de compras", false),
            EjemploArea("t-kevin", "Kevin", "Torres Mejía", "Encargado de logística y mantenimiento", "Encargado de distribución", false),
            EjemploArea("t-ricardo", "Ricardo", "Palma Iglesias", "Asistente contable", "Asistente contable", false),
            EjemploArea("t-mateo", "Mateo", "Ríos Delgado", "Content manager", "Productor de contenidos", false),
            EjemploArea("t-valentina", "Valentina", "Cruz Salazar", "Videógrafo", "Editora de video", false),
            EjemploArea("t-oscar", "Óscar", "Mendoza Pari", "Videógrafo", "Camarógrafo", false),
            EjemploArea("t-jimena", "Jimena", "Aguilar Soto", "Content creator", "Creadora de contenido", false),
            EjemploArea("d-elena-v", "Elena", "Vásquez Prado", "Director médico", "Directora médica", false),
            EjemploArea("t-rodrigo", "Rodrigo", "Campos León", "Director médico", "Coordinador médico de quirófano", false),
            EjemploArea("t-claudia", "Claudia", "Núñez Herrera", "Encargado de enfermería", "Supervisora de enfermería", false),
            EjemploArea("t-fernando", "Fernando", "Díaz Quispe", "Supervisor de call center", "Supervisor de call center", false),
            EjemploArea("d-gabriela", "Gabriela", "Fuentes Ríos", "Médico dermatólogo y estético", "Médica tricóloga", true),
            EjemploArea("d-alonso", "Alonso", "Pérez Camacho", "Médico de trasplante capilar", "Médico de implante capilar", true),
            EjemploArea("d-irene", "Irene", "Salazar Vidal", "Médico de trasplante capilar", "Médica de seguimiento capilar", true),
            EjemploArea("t-liliana", "Liliana", "Paredes Gómez", "Enfermera", "Enfermera de quirófano", false),
            EjemploArea("t-carmen-s", "Carmen", "Soto Villanueva", "Enfermera", "Enfermera de recuperación", false),
            EjemploArea("t-julio", "Julio", "Ramírez Cruz", "Técnico en enfermería", "Técnico en enfermería", false),
            EjemploArea("t-nadia", "Nadia", "Flores Benavides", "Técnico en enfermería", "Técnica instrumentista", false),
            EjemploArea("t-sofia-call", "Sofía", "Ramos León", "Supervisor de call center", "Supervisora de turnos", false),
            EjemploArea("t-hugo-op", "Hugo", "Salazar Pinto", "Coordinador operativo", "Coordinador de sede", false),
            EjemploArea("t-paola-cos", "Paola", "Ríos Mendoza", "Cosmiatra", "Cosmiatra", false),
            EjemploArea("t-daniela-cos", "Daniela", "Flores Quispe", "Cosmiatra", "Cosmiatra facial", false),
            EjemploArea("t-raul-lim", "Raúl", "Benavides Cruz", "Personal de limpieza y mantenimiento", "Auxiliar de mantenimiento", false),
            EjemploArea("t-noelia-rrhh", "Noelia", "Paredes Soto", "Encargado de RRHH", "Analista de recursos humanos", false),
            EjemploArea("t-hector-rrhh", "Héctor", "Campos Díaz", "Encargado de RRHH", "Asistente de planilla", false),
            EjemploArea("t-lucia-cont", "Lucía", "Vega Ramos", "Asistente contable", "Auxiliar contable", false),
            EjemploArea("t-miguel-cal", "Miguel", "Salazar Pinto", "Encargado de procesos y calidad", "Analista de procesos", false),
            EjemploArea("t-carla-conta", "Carla", "Mendoza Ortiz", "Contabilidad", "Asistente de contabilidad", false),
            EjemploArea("t-andres-mkt", "Andrés", "Quispe León", "Jefe de marketing y ventas", "Jefe de marketing", false),
            EjemploArea("t-diana-mkt", "Diana", "Flores Quispe", "Jefe de marketing y ventas", "Jefa de ventas", false),
            EjemploArea("t-karen-mkt", "Karen", "Ortiz León", "Coordinador de marketing", "Coordinadora de campañas", false),
            EjemploArea("t-cesar-mkt", "César", "Vargas Ríos", "Coordinador de marketing", "Coordinador de pauta", false),
            EjemploArea("t-valeria-st", "Valeria", "Iglesias Campos", "Streamer videógrafo", "Streamer", false),
            EjemploArea("t-diego-st", "Diego", "Marín Soto", "Streamer videógrafo", "Videógrafo en vivo", false),
            EjemploArea("t-oscar-dis", "Óscar", "Delgado Vega", "Diseñador gráfico", "Diseñador de piezas", false),
            EjemploArea("t-rosa-cc", "Rosa", "Huamán Delgado", "Content creator", "Creadora de reels", false),
            EjemploArea("t-ana-cm", "Ana", "Pérez León", "Community manager", "Community manager de redes", false),
            EjemploArea("t-pedro-tr", "Pedro", "Díaz Huamán", "Trafficker", "Trafficker de pauta", false),
            EjemploArea("t-luis-tr", "Luis", "Gómez Salas", "Trafficker", "Analista de medios", false),
            EjemploArea("t-elena-crm", "Elena", "Cruz Vargas", "Supervisor CRM", "Supervisora de seguimiento", false),
            EjemploArea("t-mario-datos", "Mario", "Silva Ríos", "Asistente de analista de datos", "Analista de datos comerciales", false),
            EjemploArea("t-jorge-data", "Jorge", "Aliaga Pinto", "Gestor de data comercial", "Gestor de base comercial", false),
            EjemploArea("t-sofia-data", "Sofía", "Núñez Herrera", "Gestor de data comercial", "Gestora de campañas de data", false),
            EjemploArea("t-ines-call", "Inés", "Torres Mejía", "Asesora call center", "Asesora de citas", false),
            EjemploArea("t-gloria-caj", "Gloria", "Peña Ruiz", "Cajera", "Cajera", false),
            EjemploArea("t-samuel-caj", "Samuel", "Herrera Cano", "Cajera", "Cajero de sede", false),
            EjemploArea("t-rocio-conf", "Rocío", "Molina Reyes", "Confirmadora CRM", "Confirmadora de citas", false),
        ]);
        return lista;
    }

    private static Colaborador EjemploArea(
        string id,
        string nombres,
        string apellidos,
        string area,
        string cargo,
        bool doctor)
    {
        var semilla = 0;
        foreach (var caracter in id)
            semilla = semilla * 31 + caracter;
        semilla = Math.Abs(semilla);
        var dni = 40_000_000 + semilla % 30_000_000;
        var correo = $"{Slug(nombres)}.{Slug(apellidos.Split(' ')[0])}@iderma.ejemplo";
        var telefono = $"999 {(semilla % 800) + 100} {(semilla % 700) + 200}";
        var colegiatura = doctor ? $"CMP {30_000 + semilla % 25_000}" : "";
        var especialidad = doctor ? cargo : "";
        return Persona(
            id,
            nombres,
            apellidos,
            $"DNI {dni}",
            correo,
            telefono,
            doctor ? RolClinica.Doctor : RolClinica.Trabajador,
            colegiatura,
            especialidad,
            Contrato(
                new DateTime(2023, 1, 1).AddDays(semilla % 700),
                doctor ? 9000 + semilla % 4000 : 2200 + semilla % 1800,
                area,
                cargo,
                SedeDe(area, doctor),
                doctor ? ModalidadPago.ReciboPorHonorarios : ModalidadPago.Planilla,
                doctor ? $"RH-{dni % 1000:000}" : $"PL-2026-{(semilla % 80) + 10:00}",
                semilla % 3 != 0,
                semilla % 3 != 0 ? "Web y material interno" : "—",
                semilla % 3 != 0 ? "Solo con identificación de la sede." : "No autorizó el uso de imagen.",
                doctor ? "Locación de servicios" : "Plazo indeterminado",
                doctor ? "Por procedimiento" : "Tiempo completo",
                doctor ? "Agenda médica · lun a vie" : "Lun a vie · 9:00 a 18:00",
                semilla % 2 == 0 ? "BCP" : "Interbank",
                $"**** {(semilla % 9000) + 1000}",
                $"CT-{dni % 10000:0000}",
                "Ficha de ejemplo con datos de contacto, pago y vínculo.",
                consentimiento: semilla % 3 != 0 ? new DateTime(2025, 3, 1) : null,
                venceImagen: semilla % 3 != 0 ? new DateTime(2027, 3, 1) : null));
    }

    private static string SedeDe(string area, bool doctor) =>
        doctor || OrganigramaDatos.EsClinica(area)
            ? "Clínica"
            : "Oficina administrativa 1904";

    private static string Slug(string texto)
    {
        var plano = texto.ToLowerInvariant()
            .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
            .Replace("ñ", "n");
        return new string(plano.Where(char.IsLetterOrDigit).ToArray());
    }

    private static Postulacion Postulante(
        string id,
        string nombres,
        string apellidos,
        string documento,
        string telefono,
        string correo,
        string puesto,
        string area,
        RolClinica rol,
        params RegistroPaso[] pasos)
    {
        var postulacion = new Postulacion
        {
            Id = id,
            Nombres = nombres,
            Apellidos = apellidos,
            Documento = documento,
            Telefono = telefono,
            Correo = correo,
            Puesto = puesto,
            Area = area,
            Rol = rol,
        };
        postulacion.Pasos.AddRange(pasos);
        return postulacion;
    }

    private static RegistroPaso Paso(EtapaContratacion etapa, string codigo, string texto, bool avanza, DateTime cuando) =>
        new()
        {
            Etapa = etapa,
            Codigo = codigo,
            Texto = texto,
            Avanza = avanza,
            Cuando = cuando,
        };

    private static List<Postulacion> CrearPostulaciones() =>
    [
        Postulante(
            "p-sofia", "Sofía", "Ramos Quispe", "DNI 73441002", "999 300 101", "sofia.ramos@correo.ejemplo",
            "Asistente de recepción", "Recepcionista", RolClinica.Trabajador,
            Paso(EtapaContratacion.Contacto, "contactado", "Contestó y coincide con la oferta", true, new(2026, 10, 1)),
            Paso(EtapaContratacion.Reunion, "se-presento", "Se presentó", true, new(2026, 10, 3)),
            Paso(EtapaContratacion.Conocimientos, "sobresaliente", "Sobresaliente", true, new(2026, 10, 6))),
        Postulante(
            "p-ana", "Ana", "Pérez León", "DNI 70123990", "999 300 102", "ana.perez@correo.ejemplo",
            "Community manager", "Community manager", RolClinica.Trabajador),
        Postulante(
            "p-pedro", "Pedro", "Díaz Huamán", "DNI 44519021", "999 300 103", "pedro.diaz@correo.ejemplo",
            "Auxiliar contable", "Contabilidad", RolClinica.Trabajador,
            Paso(EtapaContratacion.Contacto, "no-contesto", "No contestó", false, new(2026, 9, 28))),
        Postulante(
            "p-luis", "Luis", "Gómez Salas", "DNI 71220014", "999 300 104", "luis.gomez@correo.ejemplo",
            "Chofer de reparto", "Encargado de logística y mantenimiento", RolClinica.Trabajador,
            Paso(EtapaContratacion.Contacto, "contactado", "Contestó y coincide con la oferta", true, new(2026, 9, 20)),
            Paso(EtapaContratacion.Reunion, "no-se-presento", "No se presentó", false, new(2026, 9, 24))),
        Postulante(
            "p-elena", "Elena", "Cruz Vargas", "DNI 46881230", "999 300 105", "elena.cruz@correo.ejemplo",
            "Enfermera", "Enfermera", RolClinica.Trabajador,
            Paso(EtapaContratacion.Contacto, "contactado", "Contestó y coincide con la oferta", true, new(2026, 9, 12)),
            Paso(EtapaContratacion.Reunion, "se-presento", "Se presentó", true, new(2026, 9, 16)),
            Paso(EtapaContratacion.Conocimientos, "no-aprobo", "No aprobó", false, new(2026, 9, 18))),
        Postulante(
            "p-mario", "Mario", "Silva Ríos", "DNI 41990077", "999 300 106", "mario.silva@correo.ejemplo",
            "Médico estético", "Médico dermatólogo y estético", RolClinica.Doctor,
            Paso(EtapaContratacion.Contacto, "contactado", "Contestó y coincide con la oferta", true, new(2026, 8, 4)),
            Paso(EtapaContratacion.Reunion, "se-presento", "Se presentó", true, new(2026, 8, 8)),
            Paso(EtapaContratacion.Conocimientos, "aprobo", "Aprobó", true, new(2026, 8, 11)),
            Paso(EtapaContratacion.PrimerDia, "asistio", "Se presentó", true, new(2026, 9, 1))),
        Postulante(
            "p-carla", "Carla", "Mendoza Ortiz", "DNI 72331880", "999 300 107", "carla.mendoza@correo.ejemplo",
            "Editora", "Videógrafo", RolClinica.Trabajador,
            Paso(EtapaContratacion.Contacto, "no-coincide", "No coincide con la oferta", false, new(2026, 10, 2))),
        Postulante(
            "p-jorge", "Jorge", "Aliaga Pinto", "DNI 40228811", "999 300 108", "jorge.aliaga@correo.ejemplo",
            "Enfermero", "Enfermera", RolClinica.Trabajador,
            Paso(EtapaContratacion.Contacto, "contactado", "Contestó y coincide con la oferta", true, new(2026, 10, 7)),
            Paso(EtapaContratacion.Reunion, "se-presento", "Se presentó", true, new(2026, 10, 9)),
            Paso(EtapaContratacion.Conocimientos, "aprobo", "Aprobó", true, new(2026, 10, 10)),
            Paso(EtapaContratacion.PrimerDia, "no-asistio", "No se presentó", false, new(2026, 10, 13))),
        Postulante(
            "p-rosa-r", "Rosa", "Huamán Delgado", "DNI 73441088", "999 300 111", "rosa.huaman@correo.ejemplo",
            "Asistente de recepción", "Recepcionista", RolClinica.Trabajador),
        Postulante(
            "p-miguel-r", "Miguel", "Salazar Pinto", "DNI 70124510", "999 300 112", "miguel.salazar@correo.ejemplo",
            "Asistente de recepción", "Recepcionista", RolClinica.Trabajador,
            Paso(EtapaContratacion.Contacto, "contactado", "Contestó y coincide con la oferta", true, new(2026, 10, 8))),
        Postulante(
            "p-lucia-m", "Lucía", "Vega Ramos", "DNI 71228840", "999 300 121", "lucia.vega@correo.ejemplo",
            "Community manager", "Community manager", RolClinica.Trabajador,
            Paso(EtapaContratacion.Contacto, "contactado", "Contestó y coincide con la oferta", true, new(2026, 10, 4)),
            Paso(EtapaContratacion.Reunion, "se-presento", "Se presentó", true, new(2026, 10, 7))),
        Postulante(
            "p-andres-m", "Andrés", "Quispe León", "DNI 44520119", "999 300 122", "andres.quispe@correo.ejemplo",
            "Community manager", "Community manager", RolClinica.Trabajador,
            Paso(EtapaContratacion.Contacto, "no-coincide", "No coincide con la oferta", false, new(2026, 10, 5))),
        Postulante(
            "p-paola-c", "Paola", "Ríos Mendoza", "DNI 46880021", "999 300 131", "paola.rios@correo.ejemplo",
            "Auxiliar contable", "Contabilidad", RolClinica.Trabajador,
            Paso(EtapaContratacion.Contacto, "contactado", "Contestó y coincide con la oferta", true, new(2026, 10, 2)),
            Paso(EtapaContratacion.Reunion, "se-presento", "Se presentó", true, new(2026, 10, 6)),
            Paso(EtapaContratacion.Conocimientos, "aprobo", "Aprobó", true, new(2026, 10, 9))),
        Postulante(
            "p-hector-c", "Héctor", "Campos Díaz", "DNI 40220018", "999 300 132", "hector.campos@correo.ejemplo",
            "Auxiliar contable", "Contabilidad", RolClinica.Trabajador),
        Postulante(
            "p-noelia-l", "Noelia", "Paredes Soto", "DNI 72330041", "999 300 141", "noelia.paredes@correo.ejemplo",
            "Chofer de reparto", "Encargado de logística y mantenimiento", RolClinica.Trabajador,
            Paso(EtapaContratacion.Contacto, "contactado", "Contestó y coincide con la oferta", true, new(2026, 10, 1))),
        Postulante(
            "p-raul-l", "Raúl", "Benavides Cruz", "DNI 41991102", "999 300 142", "raul.benavides@correo.ejemplo",
            "Chofer de reparto", "Encargado de logística y mantenimiento", RolClinica.Trabajador,
            Paso(EtapaContratacion.Contacto, "no-contesto", "No contestó", false, new(2026, 9, 30))),
        Postulante(
            "p-diana-e", "Diana", "Flores Quispe", "DNI 70126670", "999 300 151", "diana.flores@correo.ejemplo",
            "Enfermera", "Enfermera", RolClinica.Trabajador,
            Paso(EtapaContratacion.Contacto, "contactado", "Contestó y coincide con la oferta", true, new(2026, 10, 3)),
            Paso(EtapaContratacion.Reunion, "se-presento", "Se presentó", true, new(2026, 10, 8))),
        Postulante(
            "p-karen-e", "Karen", "Ortiz León", "DNI 73442210", "999 300 152", "karen.ortiz@correo.ejemplo",
            "Enfermera", "Enfermera", RolClinica.Trabajador),
        Postulante(
            "p-sofia-n", "Sofía", "Núñez Herrera", "DNI 45501990", "999 300 161", "sofia.nunez@correo.ejemplo",
            "Enfermero", "Enfermera", RolClinica.Trabajador,
            Paso(EtapaContratacion.Contacto, "contactado", "Contestó y coincide con la oferta", true, new(2026, 10, 11))),
        Postulante(
            "p-cesar-n", "César", "Vargas Ríos", "DNI 42881090", "999 300 162", "cesar.vargas@correo.ejemplo",
            "Enfermero", "Enfermera", RolClinica.Trabajador,
            Paso(EtapaContratacion.Contacto, "contactado", "Contestó y coincide con la oferta", true, new(2026, 9, 22)),
            Paso(EtapaContratacion.Reunion, "no-se-presento", "No se presentó", false, new(2026, 9, 26))),
        Postulante(
            "p-valeria-d", "Valeria", "Iglesias Campos", "DNI 41992088", "999 300 171", "valeria.iglesias@correo.ejemplo",
            "Médico estético", "Médico dermatólogo y estético", RolClinica.Doctor,
            Paso(EtapaContratacion.Contacto, "contactado", "Contestó y coincide con la oferta", true, new(2026, 10, 1)),
            Paso(EtapaContratacion.Reunion, "se-presento", "Se presentó", true, new(2026, 10, 4)),
            Paso(EtapaContratacion.Conocimientos, "sobresaliente", "Sobresaliente", true, new(2026, 10, 8))),
        Postulante(
            "p-diego-d", "Diego", "Marín Soto", "DNI 40221190", "999 300 172", "diego.marin@correo.ejemplo",
            "Médico estético", "Médico dermatólogo y estético", RolClinica.Doctor),
        Postulante(
            "p-jimena-a", "Jimena", "Cruz Aguilar", "DNI 71229040", "999 300 181", "jimena.cruz@correo.ejemplo",
            "Editora", "Videógrafo", RolClinica.Trabajador,
            Paso(EtapaContratacion.Contacto, "contactado", "Contestó y coincide con la oferta", true, new(2026, 10, 6)),
            Paso(EtapaContratacion.Reunion, "se-presento", "Se presentó", true, new(2026, 10, 9)),
            Paso(EtapaContratacion.Conocimientos, "aprobo", "Aprobó", true, new(2026, 10, 12))),
        Postulante(
            "p-oscar-a", "Óscar", "Delgado Vega", "DNI 44518820", "999 300 182", "oscar.delgado@correo.ejemplo",
            "Editora", "Videógrafo", RolClinica.Trabajador),
    ];

    private static Colaborador Persona(
        string id,
        string nombres,
        string apellidos,
        string documento,
        string correo,
        string telefono,
        RolClinica rol,
        string colegiatura,
        string especialidad,
        ContratoLaboral contrato) => new()
    {
        Id = id,
        Nombres = nombres,
        Apellidos = apellidos,
        Documento = documento,
        Correo = correo,
        Telefono = telefono,
        Rol = rol,
        Colegiatura = colegiatura,
        Especialidad = especialidad,
        Contrato = contrato,
    };

    private static ContratoLaboral Contrato(
        DateTime ingreso,
        decimal sueldo,
        string area,
        string cargo,
        string sede,
        ModalidadPago modalidad,
        string codigoPago,
        bool imagen,
        string canales,
        string restricciones,
        string tipo,
        string jornada,
        string horario,
        string banco,
        string cuenta,
        string numero,
        string observaciones,
        DateTime? retiro = null,
        string motivoRetiro = "",
        DateTime? finContrato = null,
        DateTime? consentimiento = null,
        DateTime? venceImagen = null,
        int plazoMeses = 0)
    {
        var contrato = new ContratoLaboral
        {
            FechaIngreso = ingreso,
            FechaRetiro = retiro,
            MotivoRetiro = motivoRetiro,
            NumeroContrato = numero,
            TipoContrato = tipo,
            FechaFinContrato = plazoMeses > 0 ? ingreso.AddMonths(plazoMeses) : finContrato,
            PlazoMeses = plazoMeses,
            Cargo = cargo,
            Area = area,
            Sede = sede,
            Jornada = jornada,
            Horario = horario,
            SueldoMensual = sueldo,
            Modalidad = modalidad,
            CodigoPago = codigoPago,
            Banco = banco,
            NumeroCuenta = cuenta,
            AceptaUsoImagen = imagen,
            FechaConsentimientoImagen = consentimiento,
            VencimientoImagen = venceImagen,
            CanalesImagen = canales,
            RestriccionesImagen = restricciones,
            Observaciones = observaciones,
        };
        DocumentosPorFirmar.Asegurar(contrato, conEjemplos: true);
        HorarioSemana.Asegurar(contrato);
        return contrato;
    }
}
