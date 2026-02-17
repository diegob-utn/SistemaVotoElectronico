using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemaVoto.Modelos.Engine
{
    // ==========================================
    // DTOs de Entrada y Salida
    // ==========================================
    public enum EngineMetodo { DHondt, Webster }
    public enum EngineTipoEleccion { Nominal, Plancha, Mixta }

    public class EngineInput
    {
        public EngineTipoEleccion Tipo { get; set; }
        public EngineMetodo Metodo { get; set; }
        public int EscanosTotales { get; set; }
        
        // Para Mixta
        public int EscanosNominales { get; set; }
        public int EscanosLista { get; set; }

        public List<EngineLista> Listas { get; set; } = new();
        public List<EngineCandidato> CandidatosNominales { get; set; } = new();
    }

    public class EngineLista
    {
        public string Id { get; set; } = "";
        public string Nombre { get; set; } = "";
        public int Votos { get; set; }
        public List<EngineCandidato> Candidatos { get; set; } = new();
    }

    public class EngineCandidato
    {
        public string Id { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Partido { get; set; } = "";
        public int Votos { get; set; }
    }

    public class EngineResult
    {
        public List<EngineCandidatoElecto> GanadoresNominales { get; set; } = new();
        public List<EngineEscanoAsignado> DistribucionProporcional { get; set; } = new();
        public List<EngineCandidatoElecto> GanadoresLista { get; set; } = new();
        public List<EngineCandidatoElecto> TotalElectos { get; set; } = new();
        
        public EngineDetalleCalculo? DetalleProporcional { get; set; }
    }

    public class EngineCandidatoElecto
    {
        public string Nombre { get; set; } = "";
        public string Partido { get; set; } = "";
        public string Tipo { get; set; } = ""; // "Nominal" o "Lista"
        public int Votos { get; set; } // Votos directos si es nominal, o 0 si es lista
        public int Orden { get; set; }
    }

    public class EngineEscanoAsignado
    {
        public string Partido { get; set; } = "";
        public int Escanos { get; set; }
    }

    public class EngineDetalleCalculo
    {
        public string Metodo { get; set; } = "";
        public List<EngineFilaTabla> Filas { get; set; } = new();
        public List<EngineCociente> CocientesGanadores { get; set; } = new();
    }

    public class EngineFilaTabla
    {
        public string Partido { get; set; } = "";
        public int Votos { get; set; }
        public List<EngineCociente> Cocientes { get; set; } = new();
        public int EscanosGanados { get; set; }
    }

    public class EngineCociente
    {
        public string Partido { get; set; } = "";
        public double Divisor { get; set; }
        public double Valor { get; set; }
        public bool EsGanador { get; set; }
        public int OrdenAsignacion { get; set; }
    }

    // ==========================================
    // CORE ENGINE
    // ==========================================
    public class ElectionEngine
    {
        public static EngineResult Run(EngineInput input)
        {
            Validate(input);

            if (input.Tipo == EngineTipoEleccion.Plancha)
            {
                return CalculatePlancha(input);
            }
            else if (input.Tipo == EngineTipoEleccion.Mixta)
            {
                return CalculateMixta(input);
            }
            else if (input.Tipo == EngineTipoEleccion.Nominal) 
            {
                // Nominal puro es simplemente top N votos
                 return CalculateNominalPuro(input);
            }

            throw new ArgumentException($"Tipo de elección no soportado: {input.Tipo}");
        }

        private static void Validate(EngineInput input)
        {
            if (input.Tipo == EngineTipoEleccion.Mixta)
            {
                if (input.EscanosNominales + input.EscanosLista != input.EscanosTotales)
                    throw new ArgumentException("En elección Mixta, la suma de escaños nominales y de lista debe ser igual al total.");
            }
        }

        private static EngineResult CalculateNominalPuro(EngineInput input)
        {
            var winners = input.CandidatosNominales
                .OrderByDescending(c => c.Votos)
                .Take(input.EscanosTotales)
                .Select((c, idx) => new EngineCandidatoElecto 
                {
                    Nombre = c.Nombre,
                    Partido = c.Partido,
                    Tipo = "Nominal",
                    Votos = c.Votos,
                    Orden = idx + 1
                })
                .ToList();

            return new EngineResult
            {
                GanadoresNominales = winners,
                TotalElectos = winners
            };
        }

        private static EngineResult CalculatePlancha(EngineInput input)
        {
            // 1. Calcular Proporcional
            var detalle = CalculateProportionalDetailed(input.Listas, input.Metodo, input.EscanosTotales);
            
            // 2. Asignar candidatos de las listas
            var electosLista = AssignCandidatesFromList(detalle.Distribucion, input.Listas);

            return new EngineResult
            {
                DistribucionProporcional = detalle.Distribucion,
                GanadoresLista = electosLista,
                TotalElectos = electosLista,
                DetalleProporcional = detalle.Detalle
            };
        }

        private static EngineResult CalculateMixta(EngineInput input)
        {
            // 1. Parte Nominal (Mayoría simple)
            var ganadoresNominales = input.CandidatosNominales
                .OrderByDescending(c => c.Votos)
                .Take(input.EscanosNominales)
                .Select((c, idx) => new EngineCandidatoElecto
                {
                    Nombre = c.Nombre,
                    Partido = c.Partido,
                    Tipo = "Nominal",
                    Votos = c.Votos,
                    Orden = idx + 1
                })
                .ToList();

            // 2. Parte Proporcional (Lista)
            var detalle = CalculateProportionalDetailed(input.Listas, input.Metodo, input.EscanosLista);
            var electosLista = AssignCandidatesFromList(detalle.Distribucion, input.Listas, input.EscanosNominales + 1);

            return new EngineResult
            {
                GanadoresNominales = ganadoresNominales,
                DistribucionProporcional = detalle.Distribucion,
                GanadoresLista = electosLista,
                TotalElectos = ganadoresNominales.Concat(electosLista).ToList(),
                DetalleProporcional = detalle.Detalle
            };
        }

        // ==========================================
        // LÓGICA PROPORCIONAL
        // ==========================================
        
        private class ProportionalResult
        {
            public List<EngineEscanoAsignado> Distribucion { get; set; } = new();
            public EngineDetalleCalculo Detalle { get; set; } = new();
        }

        private static ProportionalResult CalculateProportionalDetailed(List<EngineLista> listas, EngineMetodo metodo, int escanos)
        {
            // Generar divisores
            var divisores = GenerateDivisors(escanos, metodo == EngineMetodo.Webster); // Webster usa impares

            // Generar todos los cocientes
            var todosCocientes = new List<EngineCociente>();
            var filasTabla = new List<EngineFilaTabla>();

            foreach (var lista in listas)
            {
                var fila = new EngineFilaTabla { Partido = lista.Nombre, Votos = lista.Votos };
                
                foreach (var div in divisores)
                {
                    var val = lista.Votos / div;
                    var cociente = new EngineCociente
                    {
                        Partido = lista.Nombre,
                        Divisor = div,
                        Valor = val
                    };
                    fila.Cocientes.Add(cociente);
                    todosCocientes.Add(cociente);
                }
                filasTabla.Add(fila);
            }

            // Ordenar y seleccionar ganadores
            var ganadores = todosCocientes
                .OrderByDescending(c => c.Valor)
                // En caso de empate técnico real, se suele usar la lista con más votos totales.
                // Aquí usamos lista.Votos implícitamente si quisieramos desempatar, pero por ahora simple sort.
                .Take(escanos)
                .ToList();

            // Marcar en la estructura
            foreach (var g in ganadores)
            {
                g.EsGanador = true;
            }
            
            // Asignar orden 
            for(int i=0; i<ganadores.Count; i++) ganadores[i].OrdenAsignacion = i+1;

            // Calcular distribución final sumarizada
            var distribucion = ganadores
                .GroupBy(g => g.Partido)
                .Select(g => new EngineEscanoAsignado { Partido = g.Key, Escanos = g.Count() })
                .ToList();

            // Actualizar filas con total ganado
            foreach (var fila in filasTabla)
            {
                var asignado = distribucion.FirstOrDefault(d => d.Partido == fila.Partido);
                fila.EscanosGanados = asignado?.Escanos ?? 0;
            }

            return new ProportionalResult
            {
                Distribucion = distribucion,
                Detalle = new EngineDetalleCalculo
                {
                    Metodo = metodo.ToString(),
                    Filas = filasTabla.OrderByDescending(f => f.Votos).ToList(),
                    CocientesGanadores = ganadores
                }
            };
        }

        private static List<double> GenerateDivisors(int count, bool oddOnly)
        {
            var divs = new List<double>();
            int n = 1;
            while (divs.Count < count)
            {
                if (!oddOnly || n % 2 != 0)
                {
                    divs.Add(n);
                }
                n++;
            }
            // Asegurar que tenemos suficientes divisores para la tabla visual (al menos 5 si es posible)
            while (divs.Count < 5)
            {
                 if (!oddOnly || n % 2 != 0) divs.Add(n);
                 n++;
            }
            return divs;
        }

        private static List<EngineCandidatoElecto> AssignCandidatesFromList(List<EngineEscanoAsignado> distribucion, List<EngineLista> listas, int startOrder = 1)
        {
            var electos = new List<EngineCandidatoElecto>();
            int currentOrder = startOrder;

            // Ordenamos la distribución para procesar listas con más escaños primero (opcional, visual)
            foreach (var asignacion in distribucion.OrderByDescending(d => d.Escanos))
            {
                var lista = listas.FirstOrDefault(l => l.Nombre == asignacion.Partido);
                if (lista == null) continue;

                // Tomar los top N candidatos de la lista (asumiendo orden de lista predefinido)
                var candidatosElectos = lista.Candidatos.Take(asignacion.Escanos);
                
                foreach (var cand in candidatosElectos)
                {
                    electos.Add(new EngineCandidatoElecto
                    {
                        Nombre = cand.Nombre,
                        Partido = lista.Nombre,
                        Tipo = "Lista",
                        Votos = 0, // En lista cerrada no importa el voto inidvidual del cand
                        Orden = 0 // Se asignará globalmente o por partido? Lo dejaremos 0 aquí y el caller puede ordenar
                    });
                }
            }
            
            // Si queremos un orden global seria complejo mezclar D'Hondt. 
            // Simplemente devolvemos la lista.
            return electos;
        }
    }
}
