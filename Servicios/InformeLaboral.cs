using IdermaAdministracion.Datos;
using IdermaAdministracion.Models;

namespace IdermaAdministracion.Servicios;

public sealed class SeccionInforme
{
    public required string Titulo { get; init; }
    public List<string> Parrafos { get; } = [];
}

public sealed class InformeLaboral
{
    public required string Titulo { get; init; }
    public required string NombreArchivo { get; init; }
    public List<SeccionInforme> Secciones { get; } = [];

    public static InformeLaboral Crear(Colaborador persona)
    {
        DocumentosPorFirmar.Asegurar(persona.Contrato, conEjemplos: false);
        HorarioSemana.Asegurar(persona.Contrato);
        var informe = new InformeLaboral
        {
            Titulo = $"Situación laboral de {persona.NombreCompleto}",
            NombreArchivo = NombreSeguro(persona.NombreCompleto),
        };
        informe.Secciones.Add(Presentacion(persona));
        informe.Secciones.Add(Vinculo(persona));
        informe.Secciones.Add(Remuneracion(persona));
        informe.Secciones.Add(Documentos(persona));
        informe.Secciones.Add(Permisos(persona));
        var postulacion = RepositorioEjemplo.Postulaciones.FirstOrDefault(p => p.PersonalId == persona.Id);
        if (postulacion is not null)
            informe.Secciones.Add(Seleccion(postulacion));
        informe.Secciones.Add(Cierre(persona));
        return informe;
    }

    private static SeccionInforme Presentacion(Colaborador persona)
    {
        var contrato = persona.Contrato;
        var seccion = new SeccionInforme { Titulo = "I. Identificación" };
        var condicion = persona.Rol == RolClinica.Doctor
            ? "integra el cuerpo médico de la clínica"
            : "presta servicios como personal de la clínica";
        seccion.Parrafos.Add(
            $"El presente informe deja constancia de la situación laboral de {persona.NombreCompleto} en Iderma Capilar. " +
            $"Se elabora en Lima, el {Formato.FechaLarga(DateTime.Today)}, a partir de la información que obra en su expediente de administración, " +
            "y tiene por objeto explicar, en un solo texto, quién es, en qué condiciones trabaja y qué documentos ha suscrito con la empresa.");
        seccion.Parrafos.Add(
            $"{persona.NombreCompleto}, identificado con {Texto(persona.Documento, "un documento de identidad que aún no ha sido consignado")}, " +
            $"{condicion}. Desempeña el cargo de {Texto(contrato.Cargo, "un cargo que todavía no ha sido precisado")} " +
            $"en el área de {Texto(contrato.Area, "un área no precisada")}. " +
            $"Su lugar de trabajo es {DescribirLugar(contrato.Sede)}. " +
            $"Para cualquier comunicación, la clínica cuenta con el correo {Texto(persona.Correo, "que aún no ha sido registrado")} " +
            $"y con el teléfono {Texto(persona.Telefono, "que aún no ha sido registrado")}.");
        if (persona.Rol == RolClinica.Doctor)
        {
            seccion.Parrafos.Add(
                $"Por tratarse de un profesional de la salud, su habilitación se acredita con la colegiatura {Texto(persona.Colegiatura, "aún no consignada")}. " +
                $"Ejerce la especialidad de {Texto(persona.Especialidad, "una especialidad que no ha sido consignada")}, " +
                "y es en ese marco que atiende a los pacientes de Iderma Capilar.");
        }

        return seccion;
    }

    private static SeccionInforme Vinculo(Colaborador persona)
    {
        var contrato = persona.Contrato;
        var seccion = new SeccionInforme { Titulo = "II. Relación con la clínica" };
        seccion.Parrafos.Add(
            $"{persona.Nombres} se incorporó a Iderma Capilar el {contrato.IngresoTexto}. " +
            $"Ese día comenzó a prestar servicios bajo el contrato número {Texto(contrato.NumeroContrato, "que aún no tiene un número asignado")}, " +
            $"celebrado en la modalidad de {Texto(contrato.TipoContrato, "un tipo que no ha sido precisado")}. " +
            $"Desde entonces cumple una jornada de {Texto(contrato.Jornada, "jornada no precisada").ToLowerInvariant()} " +
            $"en {Lugar(contrato.Sede)}.");
        seccion.Parrafos.Add(HorarioEnProsa(persona));

        if (contrato.TienePlazo && contrato.FechaFinContrato is DateTime fin)
        {
            var alerta = string.IsNullOrEmpty(contrato.AlertaVencimiento)
                ? "A la fecha de este informe el plazo se encuentra vigente y no requiere, por ahora, una decisión de renovación."
                : $"Conviene tener presente que {contrato.AlertaVencimiento.ToLowerInvariant()}, de modo que administración debe decidir si el vínculo se renueva.";
            seccion.Parrafos.Add(
                $"Las partes no pactaron un vínculo indefinido. Acordaron una duración de {contrato.PlazoTexto}, " +
                $"contados desde la fecha de ingreso, de manera que el contrato llega a su término el {Formato.FechaLarga(fin)}. {alerta}");
        }
        else
        {
            seccion.Parrafos.Add(
                "Las partes no fijaron un plazo de tres, seis o doce meses. El vínculo se entiende vigente de manera continua " +
                "y se mantiene mientras la persona continúe prestando servicios y no se deje constancia de su retiro.");
        }

        if (persona.EstaActivo)
        {
            seccion.Parrafos.Add(
                $"En consecuencia, {persona.Nombres} se encuentra en actividad. No existe en su expediente una fecha de cese " +
                "ni un motivo que haya puesto fin a la relación con Iderma Capilar.");
        }
        else
        {
            seccion.Parrafos.Add(
                $"La relación concluyó el {Formato.FechaLarga(contrato.FechaRetiro!.Value)}. " +
                $"El motivo que obra en el expediente es el siguiente: {Texto(contrato.MotivoRetiro, "no se dejó constancia del motivo")}. " +
                "A partir de esa fecha no corresponde programarle funciones, turnos ni nuevas piezas en las que aparezca su imagen.");
        }

        if (!string.IsNullOrWhiteSpace(contrato.Observaciones))
        {
            seccion.Parrafos.Add(
                $"Quien llevó el expediente anotó, además, la siguiente observación, que forma parte de la historia laboral de la persona: {contrato.Observaciones.Trim()}");
        }

        return seccion;
    }

    private static SeccionInforme Remuneracion(Colaborador persona)
    {
        var contrato = persona.Contrato;
        var seccion = new SeccionInforme { Titulo = "III. Contraprestación" };
        if (contrato.Modalidad == ModalidadPago.Planilla)
        {
            seccion.Parrafos.Add(
                $"Como contraprestación por su trabajo, {persona.Nombres} percibe {contrato.SueldoTexto} al mes. " +
                $"Ese monto se atiende por planilla, bajo el código {Texto(contrato.CodigoPago, "que todavía no ha sido asignado")}, " +
                "lo que significa que la clínica lo incorpora a la nómina y asume las obligaciones propias de esa modalidad.");
        }
        else
        {
            seccion.Parrafos.Add(
                $"Como contraprestación por sus servicios, {persona.Nombres} percibe {contrato.SueldoTexto} al mes. " +
                $"El pago no se atiende por planilla, sino mediante recibo por honorarios, " +
                $"identificado con el código {Texto(contrato.CodigoPago, "que todavía no ha sido asignado")}. " +
                "En esa modalidad, la persona emite el comprobante correspondiente y la clínica cancela el honorario pactado.");
        }

        if (contrato.CuentaPendiente)
        {
            seccion.Parrafos.Add(
                "Hasta la fecha no se ha dejado constancia de una cuenta bancaria a nombre de la persona. " +
                "Mientras esa información no conste en el expediente, administración no debe cursar transferencias a su favor.");
        }
        else
        {
            seccion.Parrafos.Add(
                $"El abono se realiza en el {contrato.BancoTexto}, en la cuenta {contrato.CuentaTexto}, " +
                "que es la que la persona indicó para recibir su contraprestación.");
        }

        return seccion;
    }

    private static SeccionInforme Documentos(Colaborador persona)
    {
        var contrato = persona.Contrato;
        var seccion = new SeccionInforme { Titulo = "IV. Documentos suscritos con la empresa" };
        var firmados = contrato.Documentos.Count(d => d.Firmado);
        var pendientes = contrato.Documentos.Count - firmados;
        seccion.Parrafos.Add(
            $"Además del acuerdo principal, Iderma Capilar acompaña el ingreso de su personal con un conjunto de documentos " +
            $"que precisan cómo debe conducirse dentro de la clínica, qué puede hacer la empresa con su imagen y qué bienes recibe. " +
            $"De esos documentos, {persona.Nombres} ya suscribió {firmados}" +
            (pendientes == 0
                ? ", de modo que el expediente de firmas está completo."
                : $" y {pendientes} {(pendientes == 1 ? "permanece" : "permanecen")} aún sin su firma."));

        foreach (var documento in contrato.Documentos)
            seccion.Parrafos.Add(ExplicarDocumento(persona, documento));

        return seccion;
    }

    private static string ExplicarDocumento(Colaborador persona, DocumentoFirma documento)
    {
        var funcion = documento.Codigo switch
        {
            "laboral" =>
                $"El contrato laboral es el documento central de la relación. Por medio de él, {persona.Nombres} y Iderma Capilar " +
                $"acuerdan el cargo, el lugar de trabajo ({Texto(persona.Contrato.Sede, "aún no precisado")}), la jornada, el horario de ingreso y de salida y la remuneración, " +
                "y quedan obligados el uno a prestar el servicio y la otra a pagarlo y a brindar las condiciones para ello.",
            "imagen" =>
                "La autorización de uso de imagen permite a la clínica decidir si puede mostrar el rostro o la voz de la persona en sus comunicaciones. " +
                "Sirve para campañas, la página web, las redes, la sala de espera y cualquier material en el que Iderma Capilar se presente ante sus pacientes.",
            "confidencialidad" =>
                "El acuerdo de confidencialidad obliga a guardar reserva sobre lo que la persona conoce por su trabajo. " +
                "Protege la información de los pacientes, los procesos clínicos, los precios, la plana médica y cualquier dato que no deba salir de la empresa.",
            "equipos" =>
                "El documento de manejo de equipos fija cómo deben usarse, cuidarse y reportarse los aparatos de la clínica. " +
                "Con él, la persona asume que un equipo de la Clínica o de la Oficina administrativa 1904 no es de uso particular y que cualquier falla o daño debe comunicarse de inmediato.",
            "reglamento" =>
                "El reglamento interno recoge las normas de convivencia de Iderma Capilar: horarios de ingreso y salida, presentación, trato a los pacientes y conducta tanto en la Clínica como en la Oficina administrativa 1904. " +
                "Al suscribirlo, la persona acepta que esas reglas forman parte de su trabajo cotidiano.",
            "datos" =>
                "El compromiso de protección de datos de pacientes recuerda que las historias clínicas, las fotografías clínicas y los datos de contacto no pueden copiarse ni compartirse fuera de la atención. " +
                "Es la base con la que la clínica cumple su deber de reserva frente a quienes se atienden en ella.",
            "bienes" =>
                "La constancia de entrega de bienes deja escrito qué le entregó la clínica al ingresar, por ejemplo uniforme, llaves, credencial o equipos portátiles. " +
                "Sirve para saber qué debe devolver la persona si el vínculo termina.",
            _ =>
                $"{documento.Nombre} forma parte de los documentos con los que Iderma Capilar ordena el trabajo de su personal.",
        };

        var firma = documento.Firmado && documento.FechaFirma is DateTime fecha
            ? $"{persona.Nombres} lo firmó el {Formato.FechaLarga(fecha)}."
            : documento.Firmado
                ? $"{persona.Nombres} ya lo firmó, aunque en el expediente no quedó escrita la fecha de esa firma."
                : $"A la fecha de este informe, {persona.Nombres} todavía no lo ha firmado, por lo que administración debe recabarlo antes de dar por cerrado el ingreso.";

        if (documento.Codigo != "imagen")
            return $"{funcion} {firma}";

        var contrato = persona.Contrato;
        var alcance = contrato.AceptaUsoImagen
            ? $"En su caso, la autorización está concedida. Se aceptó el {contrato.FechaConsentimientoTexto.ToLowerInvariant()} y vence el {contrato.VencimientoImagenTexto.ToLowerInvariant()}. " +
              $"Puede usarse en estos canales: {contrato.CanalesTexto}. La clínica debe respetar, además, esta limitación: {contrato.RestriccionesTexto}."
            : $"{persona.Nombres} no ha autorizado que su imagen se use en marketing. Por eso no corresponde publicarla en redes, en la web, en pauta ni en el material de sala.";

        return $"{funcion} {firma} {alcance}";
    }

    private static SeccionInforme Permisos(Colaborador persona)
    {
        var seccion = new SeccionInforme { Titulo = "V. Ausencias" };
        var permisos = PermisosDatos.De(persona.Id).OrderBy(p => p.Desde).ToList();
        if (permisos.Count == 0)
        {
            seccion.Parrafos.Add(
                $"Durante el tiempo que lleva en Iderma Capilar no se ha dejado constancia de permisos de ausencia, ni por horas ni por días. " +
                "Su expediente no registra interrupciones de la jornada por salud, por fallecimiento de un familiar directo ni por otro motivo.");
            return seccion;
        }

        seccion.Parrafos.Add(
            $"A lo largo de su permanencia, {persona.Nombres} ha solicitado ausentarse en {permisos.Count} {(permisos.Count == 1 ? "ocasión" : "ocasiones")}. " +
            "Cada permiso forma parte de su historia laboral y queda sujeto a la aceptación de quienes deben autorizarlo.");
        foreach (var permiso in permisos)
        {
            var cuando = permiso.Tipo == TipoAusencia.Horas
                ? $"el {Formato.FechaLarga(permiso.Desde)}, únicamente entre las {permiso.HoraInicio:hh\\:mm} y las {permiso.HoraFin:hh\\:mm}, sin dejar de pertenecer a la jornada de ese día"
                : permiso.Desde.Date == permiso.Hasta.Date
                    ? $"durante toda la jornada del {Formato.FechaLarga(permiso.Desde)}"
                    : $"desde el {Formato.FechaLarga(permiso.Desde)} hasta el {Formato.FechaLarga(permiso.Hasta)}, inclusive";
            var justificacion = string.IsNullOrWhiteSpace(permiso.Justificacion)
                ? "No se acompañó una explicación escrita del pedido."
                : $"Explicó el pedido en estos términos: {permiso.Justificacion.Trim()}";
            seccion.Parrafos.Add(
                $"En una de esas ocasiones pidió ausentarse {cuando}, por {permiso.MotivoTexto.ToLowerInvariant()}. {justificacion} " +
                $"{DescribirAceptacion(permiso)}");
        }

        return seccion;
    }

    private static SeccionInforme Seleccion(Postulacion postulacion)
    {
        var seccion = new SeccionInforme { Titulo = "VI. Cómo ingresó a la clínica" };
        seccion.Parrafos.Add(
            $"Antes de ocupar el cargo, esta persona se presentó al puesto de {postulacion.Puesto}, en el área de {postulacion.Area}. " +
            "Iderma Capilar recorre con cada postulante un camino que empieza por el contacto, sigue con una reunión y una evaluación de conocimientos, " +
            "y termina el primer día de trabajo. Lo que consta de ese camino es lo siguiente.");
        if (postulacion.Pasos.Count == 0)
        {
            seccion.Parrafos.Add("El expediente de selección no conserva todavía el resultado de esas etapas.");
            return seccion;
        }

        foreach (var paso in postulacion.Pasos.OrderBy(p => p.Etapa))
        {
            var consecuencia = paso.Avanza
                ? "Ese resultado permitió continuar con la siguiente etapa."
                : "Ese resultado cerró el proceso y la persona no continuó.";
            seccion.Parrafos.Add(
                $"El {Formato.FechaLarga(paso.Cuando)}, en la etapa destinada a {PasosContratacion.Detalle(paso.Etapa).ToLowerInvariant()}, " +
                $"se dejó constancia de que {paso.Texto.ToLowerInvariant()}. {consecuencia}");
        }

        if (!string.IsNullOrWhiteSpace(postulacion.Nota))
            seccion.Parrafos.Add($"Quien condujo la selección anotó, además: {postulacion.Nota.Trim()}");

        return seccion;
    }

    private static SeccionInforme Cierre(Colaborador persona)
    {
        var seccion = new SeccionInforme { Titulo = "VII. Cierre" };
        seccion.Parrafos.Add(
            $"En suma, este informe reúne la situación laboral de {persona.NombreCompleto} en Iderma Capilar tal como consta el {Formato.FechaLarga(DateTime.Today)}. " +
            "Explica quién es, si trabaja en la Clínica o en la Oficina administrativa 1904, desde cuándo está vinculado, " +
            "qué horario de ingreso y de salida tiene cada día, en qué condiciones se le paga, qué documentos ha firmado y para qué sirve cada uno, " +
            "y qué ausencias se han autorizado. Sirve de lectura interna para administración y no sustituye los originales firmados, que siguen siendo la constancia válida de cada acuerdo.");
        return seccion;
    }

    private static string DescribirAceptacion(PermisoAusencia permiso)
    {
        var quienes = new List<string>();
        if (permiso.AceptadoJefe) quienes.Add("el jefe directo");
        if (permiso.AceptadoAdministracion) quienes.Add("administración");
        if (permiso.AceptadoRecursosHumanos) quienes.Add("recursos humanos");
        if (permiso.AceptadoGerencia) quienes.Add("gerencia");
        if (quienes.Count == 0)
            return "Ninguna de las instancias llamadas a aceptarlo —jefe directo, administración, recursos humanos y gerencia— ha dejado constancia de su conformidad.";

        var lista = quienes.Count == 1
            ? quienes[0]
            : string.Join(", ", quienes.Take(quienes.Count - 1)) + " y " + quienes[^1];
        return $"La conformidad ya fue dada por {lista}.";
    }

    private static string HorarioEnProsa(Colaborador persona)
    {
        var dias = persona.Contrato.Dias;
        if (dias.Count == 0)
            return "El horario de ingreso y de salida no ha sido consignado.";

        if (dias.All(d => !d.Labora))
        {
            return $"{persona.Nombres} no tiene ningún día marcado como laborable. " +
                   "Mientras el expediente siga así, no hay hora de ingreso ni de salida que aplicar.";
        }

        return $"El horario se pacta por día, con hora de ingreso y hora de salida. " +
               $"{persona.Contrato.Horario} " +
               "Esas horas rigen en su lugar de trabajo y pueden modificarse si administración actualiza el expediente.";
    }

    private static string DescribirLugar(string sede) => sede switch
    {
        "Clínica" => "la Clínica, el local en el que Iderma Capilar atiende a sus pacientes",
        "Oficina administrativa 1904" => "la Oficina administrativa 1904, el local desde el cual se lleva la gestión de la clínica",
        _ => string.IsNullOrWhiteSpace(sede) ? "un lugar que todavía no ha sido precisado" : sede,
    };

    private static string Lugar(string sede) => sede switch
    {
        "Clínica" => "la Clínica",
        "Oficina administrativa 1904" => "la Oficina administrativa 1904",
        _ => string.IsNullOrWhiteSpace(sede) ? "un lugar que no ha sido precisado" : sede,
    };

    private static string Texto(string valor, string siVacio) =>
        string.IsNullOrWhiteSpace(valor) || valor.Trim() == "—" ? siVacio : valor.Trim();

    private static string NombreSeguro(string nombre)
    {
        var limpio = string.Concat(nombre.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '-' : c)).Trim();
        return string.IsNullOrWhiteSpace(limpio) ? "situacion-laboral" : $"Situacion laboral - {limpio}";
    }
}
