using System.Data;
using System.IO;
using System.Reflection;
using ClosedXML.Excel;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var export = typeof(Almoxarifado.MainWindow).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(m => m.Name == "ExportarXlsx" && !m.IsGenericMethod);
        foreach (var entrada in new[] { false, true })
        {
            using var data = new DataTable();
            data.Columns.Add(entrada ? "cod_entrada_almox" : "cod_saida_almox", typeof(long));
            data.Columns.Add("nome_apelido", typeof(string));
            data.Columns.Add("qtde", typeof(decimal));
            data.Columns.Add("data", typeof(DateTime));
            data.Columns.Add("obs", typeof(string));
            data.Rows.Add(63572L, "FUNCIONARIO TESTE", 2.5m, new DateTime(2026, 10, 24), DBNull.Value);
            data.Rows.Add(63573L, "=1+1", 0m, DBNull.Value, "Observacao");
            var path = Path.Combine(AppContext.BaseDirectory, entrada ? "entrada.xlsx" : "saida.xlsx");
            export.Invoke(null, new object[] { data, path });
            using (var book = new XLWorkbook(path))
            {
                var sheet = book.Worksheet(1);
                Check(sheet.Cell(1, 1).GetString() == data.Columns[0].ColumnName);
                Check(sheet.Cell(2, 1).GetDouble() == 63572);
                Check(sheet.Cell(2, 2).GetString() == "FUNCIONARIO TESTE");
                Check(sheet.Cell(2, 3).GetDouble() == 2.5);
                Check(sheet.Cell(2, 4).GetDateTime() == new DateTime(2026, 10, 24));
                Check(sheet.Cell(2, 5).IsEmpty());
                Check(sheet.Cell(3, 3).GetDouble() == 0);
                Check(sheet.Cell(3, 4).IsEmpty());
                Check(!sheet.Cell(3, 2).HasFormula);
                Check(!sheet.Tables.Any());
            }
            data.Clear();
            export.Invoke(null, new object[] { data, path });
            using var empty = new XLWorkbook(path);
            Check(empty.Worksheet(1).LastRowUsed().RowNumber() == 1);
        }
        Console.WriteLine("PASS: entrada/saida, cabecalhos, numeros, datas, nulos, zero e resultado vazio. Sem acesso ao banco.");
    }

    private static void Check(bool result)
    {
        if (!result) throw new Exception("Falha na exportacao de movimentacoes.");
    }
}
