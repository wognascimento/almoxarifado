using Almoxarifado.DataBase;
using Dapper;
using Npgsql;

namespace Almoxarifado.Views.Movimentacoes;

internal sealed class BaixaBolsaItem
{
    public int Id { get; set; }
    public int? Produto { get; set; }
    public double? Quantidade { get; set; }
    public string Versao { get; set; } = "";
    public string? Descricao { get; set; }
    public string? Unidade { get; set; }
    public string? Barcode { get; set; }
    public int Barcodes { get; set; }
    public bool Retorno { get; set; }
    public double Retornado { get; set; }
}

internal sealed record BaixaBolsaProduto(int Produto, string Descricao, double Quantidade);

internal sealed class BaixaBolsaService
{
    internal static readonly string[] Destinos = ["ADMINISTRAÇÃO", "ACERTO ESTOQUE", "ACERTO MOMADES",
        "CLIENTE", "MANUTENÇÃO GALPÃO", "MOMADES", "PRODUÇÃO", "TRANSFERENCIA ALMOX", "TREINAMENTO"];
    private readonly string connectionString;
    private readonly string usuario;
    internal BaixaBolsaService()
    {
        connectionString = DataBaseSettings.Instance.ConnectionString!;
        usuario = DataBaseSettings.Instance.Username ?? "";
    }
    private const string ItensSql = """
        SELECT b.cod_controlelinha AS Id,b.codcompladicional AS Produto,b.quantidade AS Quantidade,
            b.xmin::text AS Versao, p.descricao_completa AS Descricao,p.unidade AS Unidade,
            (SELECT MIN(barcode) FROM producao.tbl_barcodes WHERE codigo=b.codcompladicional) AS Barcode,
            (SELECT COUNT(DISTINCT barcode)::integer FROM producao.tbl_barcodes WHERE codigo=b.codcompladicional) AS Barcodes,
            COALESCE(b.retorno,false) AS Retorno,COALESCE(b.quantidade_retorno,0) AS Retornado
        FROM almoxarifado_apoio.tblsaidabolsa b
        LEFT JOIN producao.qry3descricoes p ON p.codcompladicional=b.codcompladicional
        WHERE b.codfun=@funcionario AND b.baixa_movimentacao IS NULL
        ORDER BY b.cod_controlelinha
        """;
    internal async Task<List<BaixaBolsaItem>> Carregar(int funcionario)
    {
        await using var db = new NpgsqlConnection(connectionString);
        try { return (await db.QueryAsync<BaixaBolsaItem>(ItensSql, new { funcionario })).ToList(); }
        catch (PostgresException ex) when (ex.SqlState == "42703")
        {
            throw new InvalidOperationException("Solicite ao suporte a execucao do script 20261123_baixa_bolsas.sql no banco deste exercicio.", ex);
        }
    }
    internal static List<BaixaBolsaProduto> Consolidar(IReadOnlyList<BaixaBolsaItem> itens)
    {
        if (itens.Count == 0) throw new InvalidOperationException("Nao existem itens pendentes de baixa para este funcionario.");
        if (itens.Select(i => i.Id).Distinct().Count() != itens.Count)
            throw new InvalidOperationException("A consulta de produtos retornou duplicidades. Solicite verificacao ao suporte.");
        foreach (var i in itens)
        {
            if (i.Produto is null || i.Descricao is null || !i.Quantidade.HasValue ||
                !double.IsFinite(i.Quantidade.Value) || i.Quantidade < 0)
                throw new InvalidOperationException($"Confira produto e quantidade no item {i.Id} antes da baixa.");
            if (i.Retorno || i.Retornado != 0)
                throw new InvalidOperationException($"O item {i.Id} possui devolucao anterior. Confira a movimentacao antes da baixa.");
            if (i.Quantidade > 0 && (i.Barcodes != 1 || string.IsNullOrWhiteSpace(i.Barcode)))
                throw new InvalidOperationException($"Confira o codigo de barras do produto {i.Produto}: ausente ou ambiguo.");
        }
        var grupos = itens.GroupBy(i => i.Produto!.Value)
            .Select(g => new BaixaBolsaProduto(g.Key, g.First().Descricao!, g.Sum(i => i.Quantidade!.Value)))
            .Where(i => i.Quantidade > 0).ToList();
        if (grupos.Count == 0 || grupos.Any(i => !double.IsFinite(i.Quantidade) || i.Quantidade > float.MaxValue))
            throw new InvalidOperationException("Nao ha quantidades validas para a baixa.");
        return grupos;
    }
    internal async Task<int> Salvar(int funcionario, IReadOnlyList<BaixaBolsaItem> previa,
        string destino, string atendente, int os, string endereco)
    {
        if (!Destinos.Contains(destino) || string.IsNullOrWhiteSpace(atendente) || os < 0 || endereco.Length > 12)
            throw new InvalidOperationException("Confira destino, atendente, O.S. e endereco (ate 12 caracteres).");
        if (string.IsNullOrWhiteSpace(usuario) || usuario.Length > (destino == "ACERTO ESTOQUE" ? 25 : 50))
            throw new InvalidOperationException("Usuario configurado invalido para registrar a movimentacao.");
        await using var db = new NpgsqlConnection(connectionString);
        await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync();
        // Bloqueia gravacoes concorrentes durante a conferencia do snapshot e a baixa.
        await db.ExecuteAsync("LOCK TABLE almoxarifado_apoio.tblsaidabolsa IN SHARE ROW EXCLUSIVE MODE;", transaction: tx);
        var atuais = (await db.QueryAsync<BaixaBolsaItem>(ItensSql, new { funcionario }, tx)).ToList();
        if (!atuais.Select(i => (i.Id, i.Versao)).SequenceEqual(previa.Select(i => (i.Id, i.Versao))))
            throw new InvalidOperationException("As bolsas foram alteradas ou baixadas em outra estacao. Feche e abra a baixa novamente.");
        var grupos = Consolidar(atuais);
        if (!await db.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM almoxarifado_jac.tbl_atendentes_almox WHERE nome_funcionario=@atendente)", new { atendente }, tx))
            throw new InvalidOperationException("Atendente nao encontrado.");
        var pessoas = (await db.QueryAsync<(string nome, string setor)>("SELECT nome_apelido,setor FROM almoxarifado_jac.view_funcionarios_terceiros WHERE codfun=@funcionario", new { funcionario }, tx)).Distinct().ToList();
        if (pessoas.Count != 1) throw new InvalidOperationException("Funcionario nao encontrado ou ambiguo.");
        var movimento = await db.ExecuteScalarAsync<int>("SELECT nextval('almoxarifado_jac.seq_movimentacao_saida')", transaction: tx);
        var data = DateTime.Today;
        var agora = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        foreach (var grupo in grupos)
        {
            var produto = atuais.First(i => i.Produto == grupo.Produto);
            var p = new { movimento, produto.Barcode, qtde = grupo.Quantidade, data, atendente, destino,
                usuario, agora, nome = pessoas[0].nome, setor = pessoas[0].setor, produto.Unidade,
                os, endereco, codigo = grupo.Produto, funcionario };
            var saida = await db.ExecuteScalarAsync<int>("""
                INSERT INTO almoxarifado_jac.t_saida_almox
                (cod_movimentacao,barcode,qtde,data,resp,requisicao,destino,incluido_por,data_inclusao,
                 funcionario,setor,unidade,num_os,endereco,codcompladicional,codfun,obs)
                VALUES (@movimento,@Barcode,@qtde,@data,@atendente,0,@destino,@usuario,@agora,
                 @nome,@setor,@Unidade,@os,@endereco,@codigo,@funcionario,'BAIXA CONSOLIDADA DE BOLSAS')
                RETURNING cod_saida_almox
                """, p, tx);
            if (destino == "ACERTO ESTOQUE")
                await db.ExecuteAsync("""
                    INSERT INTO pcp.tbl_controle_almox_estoque
                    (cod_movimentacao,cod_almox,barcode,codcompladicional,quantidade,data,operacao,processo,
                     num_os,incluido_por,incluido_data,bloqueado)
                    VALUES (@movimento,@saida,@Barcode,@codigo,@quantidade,@data,@destino,'SAIDA',
                     @os,@usuario,@data,'-1')
                    """, new { movimento, saida, produto.Barcode, codigo = grupo.Produto,
                        quantidade = grupo.Quantidade, data, destino, os, usuario }, tx);
            await db.ExecuteAsync("""
                UPDATE almoxarifado_apoio.tblsaidabolsa SET baixa_saida_id=@saida
                WHERE cod_controlelinha=ANY(@ids) AND baixa_movimentacao IS NULL
                """, new { saida, ids = atuais.Where(i => i.Produto == grupo.Produto).Select(i => i.Id).ToArray() }, tx);
        }
        await db.ExecuteAsync("""
            UPDATE almoxarifado_apoio.tblsaidabolsa
            SET baixa_movimentacao=@movimento,baixa_em=CURRENT_TIMESTAMP,baixa_por=@usuario
            WHERE cod_controlelinha=ANY(@ids) AND baixa_movimentacao IS NULL
            """, new { movimento, usuario, ids = atuais.Select(i => i.Id).ToArray() }, tx);
        await tx.CommitAsync();
        return movimento;
    }
}
