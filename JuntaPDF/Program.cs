using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Forms;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Threading;

namespace JuntaPDF
{
    class Program
    {
        static string rutaRaiz;
        static Document nuevoPDF;
        static CommonOpenFileDialog carpeta = new CommonOpenFileDialog();
        static Document PDFdoc = new Document();
        static FileStream fileStream;
        static List<PdfReader> readerList = new List<PdfReader>();
        static PdfReader pdfReader;
        static PdfWriter pdfWriter;
        static string archivoPesoinfo;
        [STAThread]
        static void Main(string[] args)
        {
            Console.Title = "Salida Proceso AAA";
            carpeta.IsFolderPicker = true;
            carpeta.Title = "Seleccionar carpeta para comprimir archivos";
            CommonFileDialogResult accionUsuario = carpeta.ShowDialog();
            if (CommonFileDialogResult.Ok.Equals(accionUsuario))
            {
                rutaRaiz = carpeta.FileName;
                archivoPesoinfo = rutaRaiz + '\\' + "Info_Peso_Archivos.txt";

                if (!File.Exists(archivoPesoinfo))
                {
                    FileStream archivo = File.Create(archivoPesoinfo);
                    archivo.Close();
                }

                escribeArchivo("INFORMACIÓN DE CONTENIDO CARPETA 'PDF'\n");
                Console.WriteLine("Juntado PDFs peso normal");
                if (Directory.Exists(rutaRaiz + @"\PDF"))
                {
                    escribeArchivo("Archivo\t\tIMG\t\tPeso en Mb\n");
                    string[] PDFS = Directory.GetFiles(rutaRaiz + @"\PDF")
                                    .OrderBy(x => x.Split('\\').Last().Split('-').First())
                                    .ThenBy(x => x.Length).ToArray();

                    JuntaPDFS(PDFS, "PDF");
                    Console.WriteLine("{0} archivos totales", PDFS.Length);
                }


                escribeArchivo("\nINFORMACIÓN DE CONTENIDO CARPETA 'OPT'\n");
                Console.WriteLine("Juntado PDFs peso optimizado");
                if (Directory.Exists(rutaRaiz + @"\OPT"))
                {
                    escribeArchivo("Archivo\t\tIMG\t\tPeso en Mb\n");
                    string[] PDFOPT = Directory.GetFiles(rutaRaiz + @"\OPT")
                                      .OrderBy(x => x.Split('\\').Last().Split('-').First())
                                      .ThenBy(x => x.Length).ToArray();
                    JuntaPDFS(PDFOPT, "OPT");
                    Console.WriteLine("{0} archivos totales", PDFOPT.Length);
                }

                escribeArchivo("\nINFORMACIÓN DE CONTENIDO CARPETA 'TIFF'\n");
                Console.WriteLine("Distribuyendo TIFF");
                if (Directory.Exists(rutaRaiz + @"\TIFF"))
                {
                    escribeArchivo("Carpeta\tPeso en Mb\n");
                    string[] TIFF = Directory.GetFiles(rutaRaiz + @"\TIFF")
                                    .OrderBy(x => x.Split('\\').Last().Split('-').First())
                                    .ThenBy(x => x.Length).ToArray();
                    DistribuirTIFF(TIFF);
                    Console.WriteLine("{0} archivos totales", TIFF.Length);
                }

            }
            else
                Console.WriteLine("No hice nada");

            Console.WriteLine("\n\nSe ha terminado el proceso, la aplicación se cerrará.");
            Thread.Sleep(1500);
            Environment.Exit(0);
        }

        public static void JuntaPDFS(string[] PDFS, string NombreCarpeta)
        {
            long pesoPDF;
            int pdfSgteIndice = 1, paginasPDF;
            string pdf = string.Empty, pdfRuta = string.Empty, pdfSgte = string.Empty, pdfDestino = string.Empty, CarpetaDestino = string.Empty;
            Rectangle tamanoPagina;
            PdfImportedPage page;
            for (int i = 0; i <= PDFS.Length - 1; i++,pdfSgteIndice++){
                pdf = Path.GetFileName(PDFS[i]);
                pdfRuta = PDFS[i]; 
                CarpetaDestino = string.Format(@"{0}\{1}\{2}", rutaRaiz, pdf.Split('-').First(),NombreCarpeta);
                pdfDestino = string.Format(@"{0}\{1}.pdf", CarpetaDestino, pdf.Split('-').First());

                paginasPDF = 1;
                Console.WriteLine("PDF {0} detectado",pdf);

                //Terminar:
                if (pdfSgteIndice >= PDFS.Length)
                    pdfSgteIndice--;

                if (!Directory.Exists(CarpetaDestino))
                    Directory.CreateDirectory(CarpetaDestino);

                pdfSgte = Path.GetFileName(PDFS[pdfSgteIndice]);

                if (!pdf.Split('-').First().Equals(pdfSgte.Split('-').First()) || pdfRuta.Equals(PDFS.Last()))
                {
                    Console.WriteLine("Copiado sin anexos");
                    File.Copy(pdfRuta, pdfDestino);
                }
                else
                {
                    Console.WriteLine("Contiene anexos:");
                    pdfReader = new PdfReader(pdfRuta);
                    nuevoPDF = new Document(pdfReader.GetPageSize(1),0,0,0,0);
                    pdfWriter = PdfWriter.GetInstance(nuevoPDF, new FileStream(pdfDestino, FileMode.Create));
                    nuevoPDF.Open();
                    recorrePDF(pdfReader.NumberOfPages);
                    while (pdf.Split('-').First().Equals(pdfSgte.Split('-').First()))
                    {
                        Console.WriteLine("{0} anexado a {1}.pdf", pdfSgte, pdf.Split('-').First());
                        pdfRuta = Path.GetDirectoryName(pdfRuta) + '\\' + pdfSgte;
                        pdfReader = new PdfReader(pdfRuta);
                        recorrePDF(pdfReader.NumberOfPages);

                        i++;
                        pdfSgteIndice++;
                        pdfRuta = PDFS[i];
                        pdf = Path.GetFileName(pdfRuta);
                        paginasPDF++;
                        if (pdfSgteIndice < PDFS.Length)
                            pdfSgte = Path.GetFileName(PDFS[pdfSgteIndice]);
                        else
                            break;
                    }
                    nuevoPDF.Close();
                }
                pesoPDF = new FileInfo(pdfDestino).Length;
                escribeArchivo(string.Format("{0}\t{1}\t{2}", Path.GetDirectoryName(CarpetaDestino).Split('\\').Last(), paginasPDF, pesoPDF / 1000));
            }
            void recorrePDF(int paginas)
            {
                for (int pagina = 1; pagina <= paginas; pagina++)
                {
                    tamanoPagina = new Rectangle(pdfReader.GetPageSize(pagina));
                    page = pdfWriter.GetImportedPage(pdfReader, pagina);
                    nuevoPDF.Add(Image.GetInstance(page));
                    nuevoPDF.SetPageSize(tamanoPagina);
                }
            }
        }

        static void DistribuirTIFF(string[] TIFF)
        {
            string tSubCarpeta = string.Empty;
            string tCarpeta = string.Empty;
            float pesoCarpeta = 0;
            foreach(string tiff in TIFF)
            {
                string t = tiff.Split('\\').Last();
                if (!string.IsNullOrEmpty(tSubCarpeta) && !tSubCarpeta.Contains(t.Split('-').First()))
                {
                    string[] tifs = Directory.GetFiles(tSubCarpeta);
                    long pesoArchivo = 0;
                    foreach(string tf in tifs)
                    {
                        pesoArchivo += new FileInfo(tf).Length;
                    }
                    pesoCarpeta = pesoArchivo / 1000;

                    escribeArchivo(string.Format("{0}\t{1}", tCarpeta.Split('\\').Last(), pesoCarpeta));

                    Console.WriteLine("{0}\t{1}", tCarpeta.Split('\\').Last(), pesoCarpeta);

                }
                tCarpeta = rutaRaiz + '\\' + t.Split('-').First();
                tSubCarpeta = rutaRaiz + '\\' + t.Split('-').First() + '\\' + "TIFF";

                if (!Directory.Exists(tCarpeta))
                    Directory.CreateDirectory(tCarpeta);

                if (!Directory.Exists(tSubCarpeta))
                    Directory.CreateDirectory(tSubCarpeta);

                File.Copy(tiff, tSubCarpeta + '\\' + t);

                if (tiff == TIFF.Last())
                {
                    string[] tifs = Directory.GetFiles(tSubCarpeta);
                    long pesoArchivo = 0;
                    foreach (string tf in tifs)
                    {
                        pesoArchivo += new FileInfo(tf).Length;
                    }
                    pesoCarpeta = pesoArchivo / 1000;

                    escribeArchivo(string.Format("{0}\t{1} ", tCarpeta.Split('\\').Last(), pesoCarpeta));

                    Console.WriteLine("{0}\t{1}", tCarpeta.Split('\\').Last(), pesoCarpeta);

                }

            }
        }
        static void escribeArchivo(string input)
        {
            using (StreamWriter WriteReportFile = File.AppendText(archivoPesoinfo))
            {
                WriteReportFile.WriteLine(input);
                WriteReportFile.Close();
            }
        }
    }
}
