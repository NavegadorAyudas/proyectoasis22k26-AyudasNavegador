using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

//-----------------------------------------------------
// - Hecho por: Evelyn Sofía Andrade Luna
// - Carne: 9959-23-1224
// - Fecha: 26/10/2026
namespace CapaModelo_Navegador
{
    // Acceso a los datos que necesita la ayuda dinámica del Navegador: el nombre de la aplicación y de
    // su módulo (base de datos de Seguridad), los archivos .chm que trae el sistema, su copia en el disco
    // del usuario y las páginas que trae cada uno por dentro. Aquí no se decide cuál ayuda mostrar, eso
    // lo hace el Controlador.
    public class ClsAyudas : CapaModelo_Seguridad.ClsConexion
    {
        // Los .chm viajan dentro de este ensamblado como recursos incrustados (ver CapaModelo_Navegador.csproj)
        // con el nombre "Ayudas/[archivo].chm". Así el sistema no depende de que exista una carpeta ayuda
        // junto al ejecutable.
        private const string _PrefijoRecurso = "Ayudas/";

        // El visor de Windows solo abre archivos .chm que estén en el disco, por eso cada ayuda se copia a
        // %LOCALAPPDATA%\Embutidos de Calidad\Ayudas la primera vez que se pide. Esa carpeta es del usuario de
        // Windows: no pide permisos de administrador y no cambia si el sistema se instala en otro lugar.
        private const string _CarpetaProducto = "Embutidos de Calidad";
        private const string _CarpetaAyudas = "Ayudas";

        // Copias que ya se compararon con su recurso en esta sesión. Calcular la huella de un .chm de decenas
        // de MB tarda, así que se hace la primera vez que se pide cada ayuda y no en cada clic en Ayuda.
        private static readonly List<string> _CopiasVerificadas = new List<string>();

        // Devuelve el nombre de la aplicación y el del módulo al que pertenece (una fila, o ninguna si
        // el IdAplicacion no existe en Seguridad).
        public DataTable NavegadorFuncObtenerDatosAplicacion(int IdAplicacion)
        {
            string ConsultaSQL =
                "SELECT a.nombreAplicacion, m.nombreModulo " +
                "FROM tblaplicacion a " +
                "INNER JOIN tblmodulo m ON m.idModulo = a.idModulo " +
                "WHERE a.idAplicacion = ?";

            OdbcConnection Conexion = SeguridadMetObtenerConexion();
            DataTable TablaDatos = new DataTable();

            try
            {
                using (OdbcDataAdapter AdaptadorDatos = new OdbcDataAdapter(ConsultaSQL, Conexion))
                {
                    AdaptadorDatos.SelectCommand.Parameters.AddWithValue("@IdAplicacion", IdAplicacion);
                    AdaptadorDatos.Fill(TablaDatos);
                }
            }
            finally
            {
                SeguridadMetDesconexion(Conexion);
            }

            return TablaDatos;
        }

        // Devuelve el nombre de los .chm que trae el sistema, por ejemplo "SeguridadAyudas.chm".
        public List<string> NavegadorFuncListarArchivosAyuda()
        {
            List<string> Archivos = new List<string>();

            foreach (string Recurso in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (Recurso.StartsWith(_PrefijoRecurso, StringComparison.Ordinal) &&
                    Recurso.EndsWith(".chm", StringComparison.OrdinalIgnoreCase))
                {
                    Archivos.Add(Recurso.Substring(_PrefijoRecurso.Length));
                }
            }

            Archivos.Sort();

            return Archivos;
        }

        // Devuelve la ruta en el disco del .chm que se pide (el nombre tal como lo entrega
        // NavegadorFuncListarArchivosAyuda). Solo se copia ese archivo, y solo cuando hace falta: si en la
        // carpeta del usuario ya hay una copia igual al recurso se usa tal cual; si falta o es de una
        // versión anterior del sistema se vuelve a copiar.
        public string NavegadorFuncObtenerRutaArchivo(string Archivo)
        {
            string Carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                _CarpetaProducto,
                _CarpetaAyudas);
            string Ruta = Path.Combine(Carpeta, Archivo);

            using (Stream Recurso = Assembly.GetExecutingAssembly().GetManifestResourceStream(_PrefijoRecurso + Archivo))
            {
                if (Recurso == null)
                {
                    throw new FileNotFoundException("El archivo de ayuda '" + Archivo + "' no está incluido en el sistema.");
                }

                if (!NavegadorFuncCopiaVigente(Ruta, Recurso))
                {
                    Directory.CreateDirectory(Carpeta);
                    NavegadorFuncCopiarRecurso(Recurso, Ruta);
                }

                if (!_CopiasVerificadas.Contains(Ruta))
                {
                    _CopiasVerificadas.Add(Ruta);
                }
            }

            return Ruta;
        }

        // La copia sirve si es igual al recurso. Primero se compara el tamaño, que es lo más barato; si
        // coincide y todavía no se verificó en esta sesión, se compara la huella SHA-256 de los dos, que es
        // la que detecta una versión distinta del mismo tamaño.
        private bool NavegadorFuncCopiaVigente(string Ruta, Stream Recurso)
        {
            if (!File.Exists(Ruta) || new FileInfo(Ruta).Length != Recurso.Length)
            {
                return false;
            }

            if (_CopiasVerificadas.Contains(Ruta))
            {
                return true;
            }

            using (FileStream Copia = new FileStream(Ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (SHA256 Algoritmo = SHA256.Create())
            {
                string HuellaCopia = BitConverter.ToString(Algoritmo.ComputeHash(Copia));

                Recurso.Position = 0;
                string HuellaRecurso = BitConverter.ToString(Algoritmo.ComputeHash(Recurso));
                Recurso.Position = 0;

                return HuellaCopia == HuellaRecurso;
            }
        }

        // Se copia a un archivo temporal y solo al terminar se le pone el nombre definitivo, así nunca
        // queda un .chm a medias con el nombre bueno si la copia se interrumpe.
        private void NavegadorFuncCopiarRecurso(Stream Recurso, string Ruta)
        {
            string Temporal = Ruta + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                using (FileStream Destino = new FileStream(Temporal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    Recurso.CopyTo(Destino);
                }

                if (new FileInfo(Temporal).Length != Recurso.Length)
                {
                    throw new IOException("La copia de la ayuda '" + Path.GetFileName(Ruta) + "' quedó incompleta.");
                }

                if (File.Exists(Ruta))
                {
                    File.Delete(Ruta);
                }

                File.Move(Temporal, Ruta);
            }
            finally
            {
                if (File.Exists(Temporal))
                {
                    File.Delete(Temporal);
                }
            }
        }

        // Devuelve los nombres de las páginas (.htm y .html) que trae un .chm, tal como están guardadas
        // adentro (por ejemplo "Empleados_Seguridad.html" o "Ayuda_General/Inicio.html").
        // Un .chm guarda al inicio un directorio con el nombre de todos sus archivos. Solo se lee ese
        // directorio (pesa unos pocos KB), no el archivo completo que puede ser de decenas de MB.
        public List<string> NavegadorFuncListarPaginas(string RutaArchivo)
        {
            List<string> Paginas = new List<string>();

            using (FileStream Archivo = new FileStream(RutaArchivo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BinaryReader Lector = new BinaryReader(Archivo))
            {
                // El encabezado empieza con la firma "ITSF". En la posición 0x48 dice dónde empieza el
                // directorio y cuántos bytes mide.
                if (Encoding.ASCII.GetString(Lector.ReadBytes(4)) != "ITSF")
                {
                    throw new InvalidDataException("El archivo '" + RutaArchivo + "' no es un archivo de ayuda .chm válido.");
                }

                Archivo.Position = 0x48;
                long InicioDirectorio = Lector.ReadInt64();
                long LargoDirectorio = Lector.ReadInt64();

                Archivo.Position = InicioDirectorio;
                byte[] Directorio = Lector.ReadBytes((int)LargoDirectorio);

                // El directorio empieza con "ITSP": largo del encabezado (0x08), tamaño de cada bloque
                // (0x10) y cantidad de bloques (0x2C).
                if (Encoding.ASCII.GetString(Directorio, 0, 4) != "ITSP")
                {
                    throw new InvalidDataException("No se pudo leer el directorio del archivo de ayuda '" + RutaArchivo + "'.");
                }

                int LargoEncabezado = BitConverter.ToInt32(Directorio, 0x08);
                int TamanoBloque = BitConverter.ToInt32(Directorio, 0x10);
                int CantidadBloques = BitConverter.ToInt32(Directorio, 0x2C);

                for (int Bloque = 0; Bloque < CantidadBloques; Bloque++)
                {
                    int InicioBloque = LargoEncabezado + (Bloque * TamanoBloque);

                    if (InicioBloque + TamanoBloque > Directorio.Length)
                    {
                        break;
                    }

                    // Solo los bloques "PMGL" traen nombres de archivos; los demás son índices.
                    if (Encoding.ASCII.GetString(Directorio, InicioBloque, 4) != "PMGL")
                    {
                        continue;
                    }

                    int EspacioLibre = BitConverter.ToInt32(Directorio, InicioBloque + 4);
                    int Posicion = InicioBloque + 20;
                    int FinBloque = InicioBloque + TamanoBloque - EspacioLibre;

                    while (Posicion < FinBloque)
                    {
                        // Cada entrada: largo del nombre, el nombre, y tres números más (sección,
                        // posición y tamaño) que aquí no se necesitan pero hay que saltarlos.
                        int LargoNombre = (int)NavegadorFuncLeerEntero(Directorio, ref Posicion);
                        string Nombre = Encoding.UTF8.GetString(Directorio, Posicion, LargoNombre);
                        Posicion += LargoNombre;

                        NavegadorFuncLeerEntero(Directorio, ref Posicion);
                        NavegadorFuncLeerEntero(Directorio, ref Posicion);
                        NavegadorFuncLeerEntero(Directorio, ref Posicion);

                        string Minuscula = Nombre.ToLowerInvariant();

                        if (Nombre.StartsWith("/") && (Minuscula.EndsWith(".htm") || Minuscula.EndsWith(".html")))
                        {
                            Paginas.Add(Nombre.Substring(1));
                        }
                    }
                }
            }

            return Paginas;
        }

        // Los números del directorio vienen en bloques de 7 bits: si el bit más alto del byte está
        // encendido, el número continúa en el byte siguiente.
        private long NavegadorFuncLeerEntero(byte[] Datos, ref int Posicion)
        {
            long Valor = 0;
            byte Actual;

            do
            {
                Actual = Datos[Posicion];
                Posicion++;
                Valor = (Valor << 7) + (Actual & 0x7F);
            }
            while ((Actual & 0x80) != 0);

            return Valor;
        }
    }
}
//--------------------------------------------------
// - Final: Evelyn Sofía Andrade Luna
// - Carne: 9959-23-1224
