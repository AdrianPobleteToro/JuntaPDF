using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JuntaPDF_TDLC
{
    class Program
    {

        static string rutaRaiz;
        static CommonOpenFileDialog carpeta = new CommonOpenFileDialog();
        static string archivoPesoinfo;

        [STAThread]
        static void Main(string[] args)
        {
            Console.Title = "Salida Proceso AAA";
            carpeta.IsFolderPicker = true;
            carpeta.Title = "Seleccionar carpeta para comprimir archivos";
            carpeta.Multiselect = true;
            CommonFileDialogResult accionUsuario = carpeta.ShowDialog();
            if (CommonFileDialogResult.Ok.Equals(accionUsuario))
            {
                var carpetas = carpeta.FileNames;

                foreach (string carpetaSelecta in carpetas) {
                    rutaRaiz = carpetaSelecta;
                    archivoPesoinfo = rutaRaiz + '\\' + "Info_Peso_Archivos.txt";

                    Console.WriteLine("Juntado PDFs en {0}",rutaRaiz.Split('\\').Last());
                    if (Directory.Exists(rutaRaiz))
                    {
                        string[] PDFS = Directory.GetFiles(rutaRaiz).Select(f => f.Split('\\').Last().Split('-').First().Replace(".pdf", "")).OrderBy(x => x.Length).Select(x => rutaRaiz + '\\' + x + ".pdf").ToArray();

                        JuntaPDFS(PDFS);
                        Console.WriteLine("{0} archivos totales", PDFS.Length);
                    } 
                }
            }
            else
                Console.WriteLine("No hice nada");

            Console.WriteLine("\n\nSe ha terminado el proceso, la aplicación se cerrará.");
            Thread.Sleep(1500);
            Environment.Exit(0);
        }

        public static void JuntaPDFS(string[] PDFS)
        {
            int pdfSgeteIndex = 1;
            string pdf = string.Empty, pdfSgte = string.Empty, pdfPath = string.Empty,pdfDestino = string.Empty;
            Document documento = new Document();
            Rectangle tamanoPagina;
            PdfReader pdfReader;
            PdfWriter pdfWriter;
            PdfImportedPage page;
            string carpetaUnidos = rutaRaiz.Replace(rutaRaiz.Split('\\').Last(), "UNIDOS");
            string carpetaSalida = carpetaUnidos + '\\' + rutaRaiz.Split('\\').Last() + '\\';

            if (!Directory.Exists(carpetaSalida))
                Directory.CreateDirectory(carpetaSalida);
            for (int i = 0; i< PDFS.Length;i++,pdfSgeteIndex++)
            {
                pdf = PDFS[i].Split('\\').Last();
                pdfPath = PDFS[i];
                pdfDestino = carpetaSalida + pdf;

                Terminar:
                if (pdfSgeteIndex >= PDFS.Length)
                    break;

                pdfSgte = PDFS[pdfSgeteIndex].Split('\\').Last();

                if (!pdf.Equals(pdfSgte))
                {
                    File.Copy(pdfPath, pdfDestino);
                }
                else
                {
                    int anexo = 2;
                    pdfReader = new PdfReader(pdfPath);
                    documento = new Document(pdfReader.GetPageSize(1), 0, 0, 0, 0);
                    pdfWriter = PdfWriter.GetInstance(documento, new FileStream(pdfDestino, FileMode.Create));
                    documento.Open();
                    recorrePDF(pdfReader.NumberOfPages);
                    while (pdf.Equals(pdfSgte))
                    {
                        string pdfAnexo = pdfPath.Replace(".pdf", "-" + anexo + ".pdf");
                        pdfReader = new PdfReader(pdfAnexo);
                        recorrePDF(pdfReader.NumberOfPages);

                        i++;
                        pdfSgeteIndex++;
                        anexo++;
                        pdfPath = PDFS[i];
                        pdf = pdfPath.Split('\\').Last();
                        if (pdfSgeteIndex < PDFS.Length)
                            pdfSgte = PDFS[pdfSgeteIndex].Split('\\').Last();
                        else
                        {
                            documento.Close();
                            goto Terminar;
                        }
                    }
                    documento.Close();
                }

            }

            void recorrePDF(int paginas)
            {
                for (int pagina = 1; pagina <= paginas; pagina++)
                {
                    tamanoPagina = new Rectangle(pdfReader.GetPageSize(pagina));
                    page = pdfWriter.GetImportedPage(pdfReader, pagina);
                    documento.Add(Image.GetInstance(page));
                    documento.SetPageSize(tamanoPagina);
                }
            }
        }

        static void DistribuirTIFF(string[] TIFF)
        {
            string tSubCarpeta = string.Empty;
            string tCarpeta = string.Empty;
            float pesoCarpeta = 0;
            foreach (string tiff in TIFF)
            {
                string t = tiff.Split('\\').Last();
                if (!string.IsNullOrEmpty(tSubCarpeta) && !tSubCarpeta.Contains(t.Split('-').First()))
                {
                    string[] tifs = Directory.GetFiles(tSubCarpeta);
                    long pesoArchivo = 0;
                    foreach (string tf in tifs)
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
