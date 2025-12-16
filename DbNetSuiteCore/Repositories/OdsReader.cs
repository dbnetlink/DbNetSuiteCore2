using DbNetSuiteCore.Extensions;
using System.Data;
using System.IO.Compression;
using System.Xml.Linq;

namespace DbNetSuiteCore.Repositories
{
    public class OdsReader : IDisposable
    {
        public OdsReader()
        {
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources if any
        }

        public DataTable GetDataTableFromUrl(string url, string sheetName)
        {
            using (HttpClient client = new HttpClient())
            using (Stream stream = client.GetStreamAsync(url).Result)
            {
                return GetDatTableFromStream(stream, sheetName);
            }
        }

        public DataTable GetDataTableFromPath(string path, string sheetName)
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return GetDatTableFromStream(fileStream, sheetName);
            }
        }

        private DataTable GetDatTableFromStream(Stream stream, string sheetName)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read, false))
                {
                    DataSet dataSet = LoadOdsToDataSet(archive.GetEntry("content.xml")); 
                    DataTable dataTable = dataSet.GetTable(sheetName);
                    return dataTable;
                }
            }
        }

        public DataSet LoadOdsToDataSet(ZipArchiveEntry contentEntry)
        {
            DataSet dataSet = new DataSet();

            if (contentEntry == null)
            {
                return dataSet;
            }

            // 1. Define the ODS Namespaces
            // These are standard URNs for OpenDocument formats.
            XNamespace tableNs = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";
            XNamespace textNs = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";
            XNamespace officeNs = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";

            // 4. Load the XML content
            using (var stream = contentEntry.Open())
            {
                XDocument xdoc = XDocument.Load(stream);

                var tables = xdoc.Descendants(tableNs + "table");

                foreach (var table in tables)
                {
                    string sheetName = table.Attribute(tableNs + "name")?.Value ?? "Sheet";
                    DataTable dt = new DataTable(sheetName);


                    var rows = table.Descendants(tableNs + "table-row").ToList();

                    int maxCols = 0;
                    foreach (var row in rows)
                    {
                        int cells = row.Descendants(tableNs + "table-cell").Count();
                        if (cells > maxCols) maxCols = cells;
                    }

                    for (int i = 0; i < maxCols; i++)
                    {
                        dt.Columns.Add($"Column_{i + 1}");
                    }

                    foreach (var row in rows)
                    {
                        DataRow dataRow = dt.NewRow();
                        var cells = row.Descendants(tableNs + "table-cell").ToList();

                        int columnIndex = 0;
                        foreach (XElement cell in cells)
                        {
                            XAttribute repeatAttr = cell.Attribute(tableNs + "number-columns-repeated");
                            int repeatCount = repeatAttr != null ? int.Parse(repeatAttr.Value) : 1;


                            string cellValue = cell.Descendants(textNs + "p").FirstOrDefault()?.Value ?? "";
                            XNode lastNode = cell.LastNode;

                            if (lastNode != null)
                            {
                                if (lastNode is XElement element)
                                {
                                    cellValue = element.Value;
                                }
                                else if (lastNode is XText textNode)
                                {
                                    cellValue = textNode.Value;
                                }
                            }

                            // Extract the text content (usually inside <text:p>)
                            

                            // Fill the DataRow (accounting for repeats)
                            for (int r = 0; r < repeatCount; r++)
                            {
                                if (columnIndex < dt.Columns.Count)
                                {
                                    dataRow[columnIndex] = cellValue;
                                    columnIndex++;
                                }
                            }
                        }
                        dt.Rows.Add(dataRow);
                    }

                    dataSet.Tables.Add(dt);
                }
            }

            return dataSet;
        }
    
    }
}
